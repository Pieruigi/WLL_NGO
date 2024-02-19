using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
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
        public struct StatePayload : INetworkSerializable
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


        NetworkVariable<NetworkObjectReference> ownerReference = new NetworkVariable<NetworkObjectReference>(default);

        PlayerController owner = null;

        Rigidbody rb;

        public Vector3 Position
        {
            get { return rb.position; }
            set { rb.position = value; }
        }


        #region prediction and reconciliation
        // General
        NetworkTimer timer;
        float serverTickRate = Constants.ServerTickRate;
        int bufferSize = 1024;

        // Client
        CircularBuffer<StatePayload> clientStateBuffer;
        StatePayload lastServerState = default;
        StatePayload lastProcessedState;
        float reconciliationThreshold = .5f;

        // Server
        CircularBuffer<StatePayload> serverStateBuffer;
        #endregion

        #region misc fields
        

        #endregion

        protected override void Awake()
        {
            base.Awake();
            // Rigidbody
            rb = GetComponent<Rigidbody>();

       
            // Init netcode for p&r
            clientStateBuffer = new CircularBuffer<StatePayload>(bufferSize);
            serverStateBuffer = new CircularBuffer<StatePayload>(bufferSize);
            timer = new NetworkTimer(serverTickRate);

        }

        private void Update()
        {
            // Update timer
            timer.Update(Time.deltaTime);

#if UNITY_EDITOR
            if (Input.GetKeyDown(KeyCode.Q))
            {

                if (!owner)
                {
                    PlayerController pc = FindObjectsOfType<PlayerController>().Where(p => p.OwnerClientId == 1).First();
                    Debug.Log($"Ball - pc:{pc.PlayerInfo}");
                    ownerReference.Value = pc.NetworkObject;
                }
                else
                {

                    ownerReference.Value = default;
                }
            }
#endif
        }

        private void FixedUpdate()
        {
            if (timer.TimeToTick())
            {
                // Client side
                HandleClientTick();
                // Server side
                HandleServerTick();
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            // Owner change event handler
            ownerReference.OnValueChanged += HandleOnOwnerReferenceChanged;
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            // Owner change event handler
            ownerReference.OnValueChanged -= HandleOnOwnerReferenceChanged;
        }
        
        void HandleOnOwnerReferenceChanged(NetworkObjectReference oldRef, NetworkObjectReference newRef)
        {
            // Try get the controller and set the owner 
            NetworkObject player = null;
            owner = newRef.TryGet(out player) ? player.GetComponent<PlayerController>() : null;

            
            if(owner != null)
            {
                // NB: if we set the ball kinematic the player's handling trigger will call a ball exit event, so we don't use kinematic at all
                //rb.isKinematic = true;
                rb.useGravity = false;
                owner.StartHandlingTheBall();
            }
            else
            {
                //rb.isKinematic= false; 
                rb.useGravity = true;
            }

            // The old owner must eventually stop handling the ball
            if(oldRef.TryGet(out player))
                player.GetComponent<PlayerController>().StopHandlingTheBall();

        }


        #region prediction and reconciliation
        /// <summary>
        /// Executed on the client: simulates the physics, puts the state into a circular buffer and check for reconciliation on every tick.
        /// </summary>
        void HandleClientTick()
        {
            if (!IsClient || IsHost)
                return;

            Physics.Simulate(Time.fixedDeltaTime);
            StatePayload clientState = ReadTransform();
            clientState.tick = timer.CurrentTick;
            clientStateBuffer.Add(clientState, clientState.tick % bufferSize);
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

            Physics.Simulate(Time.fixedDeltaTime);
            // Send the current state to the client
            StatePayload state = ReadTransform();
            state.tick = timer.CurrentTick;
            SendToClientRpc(state);
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
            Debug.Log($"Reconciliation tick:{state.tick}");

            // Get the last server state data
            WriteTransform(state.position, state.rotation, state.velocity, state.angularVelocity);

            // Add to the client buffer
            clientStateBuffer.Add(state, state.tick);

            // Replay all the input from the rewind state to the current state
            int tickToReplay = state.tick;
            while (tickToReplay < timer.CurrentTick)
            {
                int bufferIndex = tickToReplay % bufferSize;
                Physics.Simulate(Time.fixedDeltaTime);
                StatePayload clientState = ReadTransform();
                clientState.tick = tickToReplay;
                clientStateBuffer.Add(clientState, bufferIndex);
                tickToReplay++;
            }
        }

        bool ReconciliationAllowed()
        {
            bool lastServerStateIsDefined = !lastServerState.Equals(default);
            bool lastProcessedStateUndefinedOrDifferent = lastProcessedState.Equals(default) || !lastProcessedState.Equals(lastServerState);

            return lastServerStateIsDefined && lastProcessedStateUndefinedOrDifferent;

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
        /// Tell all the clients that a given player is going to shoot in few ticks ( in the future ); this way we can shoot the ball at the same tick on both server
        /// and client.
        /// </summary>
        /// <param name="playerRef"></param>
        /// <param name="force"></param>
        /// <param name="tick"></param>
        [ClientRpc]
        private void ShootAtTickClientRpc(NetworkObjectReference playerRef, Vector3 force, int tick)
        {
            NetworkObject player = null;
            if(playerRef.TryGet(out player))
            {
                ShootAtTick(player.GetComponent<PlayerController>(), force, tick);
            }
            
        }

       

        public async void ShootAtTick(PlayerController player, Vector3 velocity, int tick)
        {
            Debug.Log($"Shoot at tick {tick}");

            // You can not shoot the ball it's controlled by another player
            if (owner != null && owner != player) return;
            
            if(IsServer && !IsHost)
                ShootAtTickClientRpc(new NetworkObjectReference(player.NetworkObject), velocity, tick);
            
            if(timer.CurrentTick > tick)
            {
                rb.velocity = velocity;
            }
            else
            {
                while(timer.CurrentTick < tick)
                {
                    await Task.Delay(1000/Constants.ServerTickRate);
                }
                // Check if you can still shoot the ball first
                rb.velocity = velocity;
            }
                
        }

        /// <summary>
        /// Evaluation function to decretate a winner during tackles
        /// </summary>
        /// <param name="playerA"></param>
        /// <param name="playerB"></param>
        /// <returns></returns>
        PlayerController EvaluateTackleWinner(PlayerController playerA, PlayerController playerB)
        {
            Debug.Log($"Tackle evaluation - {playerA} VS {playerB}");
            return playerA;
        }

        /// <summary>
        /// A player in on the ball, we must check if the player can handle it
        /// </summary>
        /// <param name="player"></param>
        public void BallEnterTheHandleTrigger(PlayerController player)
        {
            if (IsServer)
            {
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
        public void BallExitTheHandleTrigger(PlayerController player)
        {
            if(IsServer)
            {
                if(owner == player)
                {
                    ownerReference.Value = default;
                }
            }
        }

        #endregion
    }

}
