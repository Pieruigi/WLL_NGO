using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading.Tasks;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;
using WLL_NGO.AI;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.UI.Image;
using static WLL_NGO.Netcode.PlayerController;

namespace WLL_NGO.Netcode
{
    /// <summary>
    /// - Ball Synchronization.
    /// The ball uses the same logic as the client controller but with a slightly different implementation.
    /// Basically the client reads the input and sends it to the server with the corresponding tick ( in the HandleClientTick() method ); at this point the server 
    /// processes the input and, for example, tells the client to shoot ( if the input was about shooting ), but since we have to play some animation before we can 
    /// actually shoot, the server can tell all the clients that the player is shooting at a specific tick in the future; doing so all the clients and the server 
    /// would be able to shoot at the same time ( if you have 1 second of lag this is not going to work for you of course, but in that case nothing is going to work ).
    /// 
    /// - Ball handling.
    /// When the ball enters the player handling trigger the player tell the ball they to take control; the ball runs some evaluation function and eventually gives 
    /// control to the player. When the player has given control over the ball the ball kinematic is set true and the ball is attached to the player's hook transform
    /// which is handled by animations ( in this case we can for example move the ball back and forth while the player is moving ).
    /// </summary>
    public class BallController : SingletonNetwork<BallController>
    {
        public static UnityAction OnBallSpawned;
        public static UnityAction OnShoot;
        public static UnityAction</*old*/PlayerController, /*new*/PlayerController> OnOwnerChanged;

        struct StatePayload : INetworkSerializable
        {
            public int tick;
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 velocity;
            public Vector3 angularVelocity;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref tick);
                serializer.SerializeValue(ref position);
                serializer.SerializeValue(ref rotation);
                serializer.SerializeValue(ref velocity);
                serializer.SerializeValue(ref angularVelocity);
            }

            public override string ToString()
            {
                return $"[Ball.StatePayload tick:{tick}, position:{position}, rotation:{rotation}, velocity:{velocity}, angularVelocity:{angularVelocity}]";
            }
        }

        struct ShootingData
        {
            public Vector3 InitialPosition;
            public Vector3 TargetPosition;
            public Vector3 Velocity; // Velocity with no effect applied
            public Vector3 InitialEffectVelocity, CurrentEffectVelocity; // Effect velocities
            public float EffectTime, CurrentEffectTime;
            public int InitialTick; // The ball tick
            public int FinalTick; // The ball tick
            public PlayerController Shooter;
            public PlayerController Receiver; // Only for passage

            public ShotTiming Timing;

            public bool IsPassage;

            public bool IsOnTheFly;

            public override string ToString()
            {
                return $"[ShootingData Position:{InitialPosition}, Target:{TargetPosition}, StraightVelocity:{Velocity}, InitialEffectVelocity:{InitialEffectVelocity}, CurrentEffectVelocity:{CurrentEffectVelocity}, EffectTime:{EffectTime}, CurrentEffectTime:{CurrentEffectTime}, Timing:{Timing}, IsPassage:{IsPassage}, IsOnTheFly:{IsOnTheFly}]";
            }
        }


        NetworkVariable<NetworkObjectReference> ownerReference = new NetworkVariable<NetworkObjectReference>(default);

        PlayerController owner = null;
        public PlayerController Owner
        {
            get { return owner;}
        }

        Rigidbody rb;

        public Vector3 Position
        {
            get { return rb.position; }
            set { rb.position = value; if (rb.position.y < coll.radius) rb.position = new Vector3(rb.position.x, coll.radius + 0.01f, rb.position.z); }
        }

        public Vector3 Velocity
        {
            get { return rb.velocity; }
            set { rb.velocity = value; }
        }

        public Vector3 AngularVelocity
        {
            get { return rb.angularVelocity; }
            set { rb.angularVelocity = value; }
        }

        public bool Kinematic
        {
            get { return rb.isKinematic; }
            //set { rb.isKinematic = value; }
        }

        public bool UseGravity
        {
            get { return rb.useGravity; }
            //set { rb.useGravity = value; }
        }

        SphereCollider coll;

        #region prediction and reconciliation
        // General
        int bufferSize = 1024;

        // Client
        CircularBuffer<StatePayload> clientStateBuffer;
        StatePayload lastServerState = default;
        StatePayload lastProcessedState;
        float reconciliationThreshold = .5f;
        float reconciliationSpeed = 4f;
        // Server
        CircularBuffer<StatePayload> serverStateBuffer;
        #endregion

        #region misc fields
        ShootingData shootingData = default;
        
        #endregion

        protected override void Awake()
        {
            base.Awake();
            // Rigidbody
            rb = GetComponent<Rigidbody>();
            coll = GetComponent<SphereCollider>();
       
            // Init netcode for p&r
            clientStateBuffer = new CircularBuffer<StatePayload>(bufferSize);
            serverStateBuffer = new CircularBuffer<StatePayload>(bufferSize);

            //timer = new NetworkTimer();
            //Physics.simulationMode = SimulationMode.FixedUpdate;
            //MatchController.OnStateChanged += HandleOnMatchStateChanged;
        }

        

       

        private void Update()
        {
            if(!IsSpawned) 
                return;

        
            // Update timer
            // The ball is already on scene when the server starts, so client and server ticks don't match.
            //timer.Update(Time.deltaTime);

#if UNITY_EDITOR
            if (Input.GetKeyDown(KeyCode.Q))
            {
                Time.timeScale = 1;
                //if (!owner)
                //{
                //    PlayerController pc = FindObjectsOfType<PlayerController>().Where(p => p.OwnerClientId == 1).First();
                //    Debug.Log($"Ball - pc:{pc.PlayerInfo}");
                //    ownerReference.Value = pc.NetworkObject;
                //}
                //else
                //{

                //    ownerReference.Value = default;
                //}
            }


#endif
        }


        void OnEnable()
        {
            MatchController.OnStateChanged += HandleOnMatchStateChanged;
        }

        void OnDisable()
        {
            MatchController.OnStateChanged -= HandleOnMatchStateChanged;
        }
        //private void FixedUpdate()
        //{
        //    if (!IsSpawned)
        //        return;

        //    if (timer==null) 
        //        return;

        //    if (timer.TimeToTick())
        //    {
        //        // Client side
        //        HandleClientTick();
        //        // Server side
        //        HandleServerTick();
        //    }


        //}

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();


            // Owner change event handler
            ownerReference.OnValueChanged += HandleOnOwnerReferenceChanged;

            if (IsServer)
                OnBallSpawned?.Invoke();

            NetworkTimer.Instance.OnTimeToTick += HandleOnTimeToTick;

        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            // Owner change event handler
            ownerReference.OnValueChanged -= HandleOnOwnerReferenceChanged;

            NetworkTimer.Instance.OnTimeToTick -= HandleOnTimeToTick;
        }

        void HandleOnTimeToTick()
        {
            if (!IsSpawned)
                return;

            
            // Client side
            HandleClientTick();
            // Server side
            HandleServerTick();
            
        }

        private void HandleOnMatchStateChanged(int oldValue, int newValue)
        {
            switch(newValue)
            {
                case (byte)MatchState.StartingMatch:
                    //timer = new NetworkTimer();
                    break;
            }

        
        }

        void HandleOnOwnerReferenceChanged(NetworkObjectReference oldRef, NetworkObjectReference newRef)
        {
            // Try get the controller and set the owner 
            NetworkObject player = null;
            owner = newRef.TryGet(out player) ? player.GetComponent<PlayerController>() : null;

            if(owner != null)
            {
                if(owner.Role != PlayerRole.GK || /*owner.GetState() != (byte)PlayerState.Diving ||*/ !owner.GetComponent<GoalkeeperAI>().IsBouncingTheBallBack)
                {
                    // NB: if we set the ball kinematic the player's handling trigger will call a ball exit event, so we don't use kinematic unless we set 
                    // useTrigger to false in the handling trigger.
                    rb.isKinematic = true;
                    // Reset velocity and agular velocity
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    // Tell the player to take control of the ball
                    owner.StartHandlingTheBall();
                }
                //else
                //{
                //    player.GetComponent<GoalkeeperAI>().BounceTheBallBack();
                //}
            }
            else
            {
                if(rb.isKinematic)
                    rb.isKinematic= false; 
                //rb.useGravity = true;
            }

            // The old owner must eventually stop handling the ball
            PlayerController oldOwner = null;
            if(oldRef.TryGet(out player))
            {
                oldOwner = player.GetComponent<PlayerController>();
                oldOwner.GetComponent<PlayerController>().StopHandlingTheBall();
            }

            // If one of opponent team players was waiting the ball (for example during a pass) we better try to select another player
            if (owner)
            {
                var opponentTeam = TeamController.GetOpponentTeam(owner);

                var receiver = opponentTeam.GetPlayers().Find(p => p.IsReceivingPassage());
                if (receiver)
                {
                    // Try to set the receiver to normal 
                    if (receiver.Position.y < 0.1f)
                        receiver.SetPlayerStateInfo((byte)PlayerState.Normal, 0, 0, 0);

                    opponentTeam.SelectClosestPlayerToBall(goalkeeperAllowed: false);    
                }
                
            }
                

            OnOwnerChanged?.Invoke(oldOwner, owner);
        }


        #region prediction and reconciliation
        /// <summary>
        /// Executed on the client: simulates the physics, puts the state into a circular buffer and check for reconciliation on every tick.
        /// </summary>
        void HandleClientTick()
        {
            if (!IsClient || IsHost)
                return;

            //Physics.simulationMode = SimulationMode.Script;
            //Physics.Simulate(Time.fixedDeltaTime);
            //Physics.simulationMode = SimulationMode.FixedUpdate;
            StatePayload clientState = ReadTransform();
            
            clientState.tick = NetworkTimer.Instance.CurrentTick;
            //Debug.Log($"Reconciliation - read client state, tick:{clientState.tick}, pos:{clientState.position}");
            clientStateBuffer.Add(clientState, clientState.tick % bufferSize);

            // Handle effect
            HandleEffect();

            // Reconciliate
            Reconciliate();
        }

        /// <summary>
        /// Executed on the server: simulates the physics and send the last state to all clients to allow reconciliation.
        /// </summary>
        void HandleServerTick()
        {
            if(!IsServer)
                return;

            //Physics.simulationMode = SimulationMode.Script;
            //Physics.Simulate(Time.fixedDeltaTime);
            //Physics.simulationMode = SimulationMode.FixedUpdate;
            // Send the current state to the client
            
            // Handle effect
            HandleEffect();



            StatePayload state = ReadTransform();
            state.tick = NetworkTimer.Instance.CurrentTick;
            serverStateBuffer.Add(state, state.tick);
            SendToClientRpc(state);
        }

        void HandleEffect()
        {
            if (shootingData.EffectTime > 0)
            {
                // Remove the old effect velocity
                rb.velocity -= shootingData.CurrentEffectVelocity;
                shootingData.CurrentEffectVelocity = shootingData.InitialEffectVelocity * (1f - 2f * shootingData.CurrentEffectTime / shootingData.EffectTime);
                shootingData.CurrentEffectTime += NetworkTimer.Instance.DeltaTick;
                // Add the updated effect velocity
                rb.velocity += shootingData.CurrentEffectVelocity;
                if (shootingData.CurrentEffectTime >= shootingData.EffectTime)
                    shootingData.EffectTime = 0;


                Debug.Log(shootingData);
            }
        }

        void Reconciliate()
        {
            if (!ReconciliationAllowed())
                return;

            StatePayload rewindState = lastServerState;
            int bufferIndex = rewindState.tick % bufferSize;

            float positionError = Vector3.Distance(rewindState.position, clientStateBuffer.Get(bufferIndex).position);
            
            if (positionError > reconciliationThreshold)
            {
                ReconcileState(rewindState);
            }


            lastProcessedState = rewindState;
        }

        void ReconcileState(StatePayload state)
        {
            Vector3 position = Vector3.Lerp(rb.position, state.position, NetworkTimer.Instance.DeltaTick * reconciliationSpeed);
            Quaternion rotation = Quaternion.Lerp(rb.rotation, state.rotation, NetworkTimer.Instance.DeltaTick * reconciliationSpeed);
            Vector3 velocity = Vector3.Lerp(rb.velocity, state.velocity, NetworkTimer.Instance.DeltaTick * reconciliationSpeed);
            Vector3 angularVelocity = Vector3.Lerp(rb.angularVelocity, state.angularVelocity, NetworkTimer.Instance.DeltaTick * reconciliationSpeed);

            // Get the last server state data
            //WriteTransform(state.position, state.rotation, state.velocity, state.angularVelocity);
            WriteTransform(position, rotation, velocity, angularVelocity);
            // Add to the client buffer
            clientStateBuffer.Add(state, state.tick);

            // Replay all the input from the rewind state to the current state
            //int tickToReplay = state.tick;

            //Physics.simulationMode = SimulationMode.Script;
            //while (tickToReplay < timer.CurrentTick)
            //{
            //    int bufferIndex = tickToReplay % bufferSize;

            //    Physics.Simulate(timer.DeltaTick);
            //    StatePayload clientState = ReadTransform();
                
            //    clientState.tick = tickToReplay;
            //    clientStateBuffer.Add(clientState, bufferIndex);
            //    tickToReplay++;
            //}
            //Physics.simulationMode = SimulationMode.FixedUpdate;
        }

        bool ReconciliationAllowed()
        {
            bool lastServerStateIsDefined = !lastServerState.Equals(default);
            bool lastProcessedStateUndefinedOrDifferent = lastProcessedState.Equals(default) || !lastProcessedState.Equals(lastServerState);
            bool ownerIsNull = owner == null;

            return lastServerStateIsDefined && lastProcessedStateUndefinedOrDifferent && ownerIsNull;

        }

        [ClientRpc]
        void SendToClientRpc(StatePayload state)
        {
            lastServerState = state;
        }

        void WriteTransform(Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angularVelocity)
        {
            rb.position = position;
            rb.rotation = rotation.normalized;
            rb.velocity = velocity;
            rb.angularVelocity = angularVelocity;
        }

        StatePayload ReadTransform()
        {
            return new StatePayload()
            {
                position = rb.position,
                rotation = rb.rotation,
                velocity = rb.velocity,
                angularVelocity = rb.angularVelocity

            };
        }


        #endregion

        #region gameplay

        /// <summary>
        /// If we want to reach a target T we must aim to T1 higher than T because of the gravity.
        /// We apply the formula V(y) = (1 / 2 * g* t) +/ -(d* sin(b)) / t(sign depending whether the ball is higher or lower than the target)
        /// - V(y) is the vertical velocity taking into account the gravity(towards T1)
        /// - g is the gravity acceleration
        /// - t is the time it will take to reach the original target T depending on the speed
        /// - b is the angle between the original direction(towards T) and the horizontal plane
        /// Basically our vertical velocity will be made by two components: one to reach the target and anothersla to contrast the gravity
        /// </summary>
        /// <param name="targetPosition"></param>
        /// <param name="speed"></param>
        /// <param name="effectSpeed"></param>
        ShootingData ComputeVelocity(Vector3 targetPosition, float speed, float effectSpeed)
        {
            if (speed > GlobalGameManager.Instance.MaxBallSpeed)
                speed = GlobalGameManager.Instance.MaxBallSpeed;
                

            ShootingData sData = default;
            //sData.InitialTick = NetworkTimer.Instance.CurrentTick;
            sData.InitialTick = NetworkTimer.Instance.CurrentTick;
            sData.TargetPosition = targetPosition;
            sData.InitialPosition = rb.position;
            
            // Get the original direction
            Vector3 direction = targetPosition - rb.position;
            // Project the direction on the floor
            Vector3 dirOnFloor = Vector3.ProjectOnPlane(direction, Vector3.up);
            // Get the angle between original direction and its projection
            float b = Vector3.Angle(direction, dirOnFloor);

            // Get the time it takes to reach the original target 
            float t = direction.magnitude / speed;
            sData.FinalTick = NetworkTimer.Instance.CurrentTick + Mathf.RoundToInt( t / NetworkTimer.Instance.DeltaTick );

            // Invert the sign of the first component of the velocity if the ball is higher than the target ( we must move down )
            float sign = rb.position.y > targetPosition.y ? -1 : 1;

            // Compute horizontal and vertical components: the first term is the velocity to reach the original target, the second is to contrast gravity
            Vector3 velY = (sign * (direction.magnitude * math.sin(math.radians(b)) / t) + (.5f * math.abs(Physics.gravity.y) * t)) * Vector3.up;
            Vector3 velH = dirOnFloor.normalized * (dirOnFloor.magnitude / t);
            Vector3 velE = Vector3.zero;

            
            
            // We apply some effect to the ball if needed
            if (effectSpeed != 0)
            {
                //eSpeed = eSpeed * (1f - 2f * t);
                Vector3 eDir = Vector3.Cross(direction, dirOnFloor).normalized;
                velE = eDir * effectSpeed;

                sData.EffectTime = t;
                sData.InitialEffectVelocity = velE;
                sData.CurrentEffectTime = 0;
                sData.CurrentEffectVelocity = velE;
            }
            
            sData.Velocity = velH + velY + velE;

            

            // Apply shooting data
            return sData;
        }

        /// <summary>
        /// Tell all the clients that a given player is going to shoot in few ticks ( in the future ); this way we can shoot the ball at the same tick on both server
        /// and client.
        /// </summary>
        /// <param name="playerRef"></param>
        /// <param name="force"></param>
        /// <param name="tick"></param>
        [ClientRpc]
        private void ShootAtTickClientRpc(NetworkObjectReference playerRef, NetworkObjectReference receiverRef, Vector3 targetPosition, float speed, float effectSpeed, int tick, bool isPass, bool isOnTheFly)
        {
            NetworkObject player = null;
            if(playerRef.TryGet(out player))
            {
                NetworkObject receiver = null;
                receiverRef.TryGet(out receiver);
                ShootAtTick(player.GetComponent<PlayerController>(), receiver ? receiver.GetComponent<PlayerController>() : null, targetPosition, speed, effectSpeed, tick, isPass, isOnTheFly);
            }
            
        }

       
        // TODO: Add som IsPassage boolean parameter
        public async void ShootAtTick(PlayerController player, PlayerController receiver, Vector3 targetPosition, float speed, float effectSpeed, int tick, bool isPass, bool isOnTheFly)
        {
           
            //Vector3 velocity = (targetPosition - rb.position).normalized * speed;

            // You can not shoot the ball it's controlled by another player
            if (owner != null && owner != player) return;
            
            // If we are not playng singleplayer we need to tell the other clients that the player is going to shoot
            if(IsServer && !IsHost)
                ShootAtTickClientRpc(new NetworkObjectReference(player.NetworkObject), receiver ? new NetworkObjectReference(receiver.NetworkObject) : default, targetPosition, speed, effectSpeed, tick, isPass, isOnTheFly);
            
            if(NetworkTimer.Instance.CurrentTick > tick)
            {
                //Vector3 velocity = (targetPosition - rb.position).normalized * speed;
                shootingData = ComputeVelocity(targetPosition, speed, effectSpeed);
                shootingData.Shooter = player;
                shootingData.Receiver = receiver;
                shootingData.IsPassage = isPass;
                shootingData.IsOnTheFly = isOnTheFly;
                 if (!isPass)
                    {
                        if (!isOnTheFly)
                            shootingData.Timing = InputTimingUtility.GetShotTimingByCharge(player.Charge);
                        else
                            shootingData.Timing = InputTimingUtility.GetOnTheFlyTiming(player.Charge);
                    }
                    else
                    {
                        if (isOnTheFly)
                            shootingData.Timing = InputTimingUtility.GetOnTheFlyTiming(player.Charge);
                    }
                // Adjust elapsed time
                if (shootingData.EffectTime > 0)
                    shootingData.CurrentEffectTime = (NetworkTimer.Instance.CurrentTick - tick) * NetworkTimer.Instance.DeltaTick;
    

                Shoot(player, shootingData);
            }
            else
            {
                while(NetworkTimer.Instance.CurrentTick < tick)
                {
                    await Task.Delay(1000/Constants.ServerTickRate);
                }
                // Check if you can still shoot the ball first
                if(owner == null || owner == player)
                {
                    //Vector3 velocity = (targetPosition - rb.position).normalized * speed;
                    shootingData = ComputeVelocity(targetPosition, speed, effectSpeed);
                    shootingData.Shooter = player;
                    shootingData.Receiver = receiver;
                    shootingData.IsPassage = isPass;
                    shootingData.IsOnTheFly = isOnTheFly;
                    if (!isPass)
                    {
                        if (!isOnTheFly)
                            shootingData.Timing = InputTimingUtility.GetShotTimingByCharge(player.Charge);
                        else
                            shootingData.Timing = InputTimingUtility.GetOnTheFlyTiming(player.Charge);
                    }
                    else
                    {
                        if (isOnTheFly)
                            shootingData.Timing = InputTimingUtility.GetOnTheFlyTiming(player.Charge);
                    }
                        
                    
                    // Adjust elapsed time ( it should be the same tick at this point, but... who knows )
                    if (shootingData.EffectTime > 0)
                        shootingData.CurrentEffectTime = (NetworkTimer.Instance.CurrentTick - tick) * NetworkTimer.Instance.DeltaTick;

                    Shoot(player, shootingData);
                }
                
            }

            Debug.Log($"TEST - ShootingData:{shootingData}");

            OnShoot?.Invoke();
        }

        void Shoot(PlayerController player, ShootingData shootingData)
        {
            player.StopHandlingTheBall();
            rb.isKinematic = false;
            rb.velocity = shootingData.Velocity;

            if (IsServer)
            {
                if(shootingData.Receiver != null)
                {
                    // Flag target as receiver
                    shootingData.Receiver.SetAsReceiver();

                    // Set target as the selected player
                    TeamController.GetPlayerTeam(shootingData.Receiver).SetPlayerSelected(shootingData.Receiver);
                }
                
                
            }

            
        }
        
        public Vector3 GetVelocityWithoutEffect()
        {
            Vector3 vel = Velocity;

            if (vel.magnitude > 0 && shootingData.EffectTime > 0)
                vel -= shootingData.CurrentEffectVelocity;

            return vel;
        }
        
       
        

        /// <summary>
        /// Evaluation function to decretate a winner during tackles
        /// </summary>
        /// <param name="playerA">The player who just entered the trigger</param>
        /// <param name="playerB">The current owner</param>
        /// <returns></returns>
        PlayerController EvaluateTackleWinner(PlayerController playerA, PlayerController playerB)
        {
            if (playerA.Role == PlayerRole.GK)
                return playerA;

            return playerA;
        }

        

        /// <summary>
        /// A player in on the ball, we must check if the player can handle it
        /// </summary>
        /// <param name="player"></param>
        public void BallEnterTheHandlingTrigger(PlayerController player)
        {
            if (IsServer)
            {
                // if (MatchController.Instance.MatchState != MatchState.Playing || MatchController.Instance.MatchState != MatchState.KickOff)
                //     return;

                PlayerController winner = player;
                // If any opponent player is owning the ball we need to call the evaluate function to choose the winner
                if (!owner)
                {
                    winner = EvaluateTackleWinner(player, owner);
                    PlayerController loser = winner == player ? owner : player;

                    // DoSomethingWithTheLoser();
                }

                // If the new player is the winner we change the ball ownership
                if (winner != owner)
                    ownerReference.Value = winner.NetworkObject;
                
            }
        }

        /// <summary>
        /// Player off the ball, eventually loses controll.
        /// </summary>
        /// <param name="player"></param>
        public void BallExitTheHandlingTrigger(PlayerController player)
        {
            if(IsServer)
            {
                if(owner == player)
                {
                    ownerReference.Value = default;
                }
            }
        }

        public Vector3 GetEstimatedPosition(int tickCount)
        {
            return rb.position + rb.velocity * tickCount * NetworkTimer.Instance.DeltaTick + Vector3.up * Physics.gravity.y * tickCount * NetworkTimer.Instance.DeltaTick;
        }

        public Vector3 GetShootingDataTargetPosition()
        {
            return shootingData.TargetPosition;
        }

        public int GetShootingDataRemainingTicks()
        {
            return shootingData.FinalTick - NetworkTimer.Instance.CurrentTick;
        }

        public float GetShootingDataRemainingTime()
        {
            return GetShootingDataRemainingTicks() * NetworkTimer.Instance.DeltaTick;
        }

        public PlayerController GetShootingDataShooter()
        {
            return shootingData.Shooter;
        }

        public PlayerController GetShootingDataReceiver()
        {
            return shootingData.Receiver;  
        }

        public Vector3 GetShootingDataInitialPosition()
        {
            return shootingData.InitialPosition;
        }
                

        public bool TryGetServerStatePosition(int tick, out Vector3 position)
        {
            position = Vector3.zero;   
            if (serverStateBuffer.Equals(default))
                return false;

            position = serverStateBuffer.Get(tick).position;
            return true;
        }

        public void ResetToKickOff()
        {
            rb.isKinematic = true;
            Position = BallSpawner.Instance.GetKickOffBallPosition();
            Velocity = Vector3.zero;
            Velocity = Vector3.zero;
            rb.isKinematic = false;
        }

        #endregion



    }

}
