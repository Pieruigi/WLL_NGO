using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace WLL_NGO.Gameplay
{
    


    public class PlayerController : NetworkBehaviour
    {
        public struct InputPaylod : INetworkSerializable
        {
            public int tick;
            public Vector2 inputVector;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref tick);
                serializer.SerializeValue(ref inputVector);
            }
        }

        public struct StatePayload : INetworkSerializable
        {
            public int tick;
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 velocity;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref tick);
                serializer.SerializeValue(ref position);
                serializer.SerializeValue(ref rotation);
                serializer.SerializeValue(ref velocity);
            }

            public override string ToString()
            {
                return $"[StatePayload tick:{tick}, position:{position}, rotation:{rotation}, velocity:{velocity}]";
            }
        }


        [SerializeField]
        float maxSpeed = 5f;

        [SerializeField]
        float rotationSpeed = 480;


        [SerializeField]
        float acceleration = 10f;

        [SerializeField]
        float deceleration = 30f;

        //CharacterController cc;
        Rigidbody rb;

        bool moving = false;
        Vector3 currentVelocity = Vector3.zero;

        #region input data
        Vector3 inputMove;
        bool shootInput, passInput;
        #endregion

        #region netcode prediction and reconciliation
        // General
        NetworkTimer timer;
        float serverTickRate = 60f;
        int bufferSize = 1024;

        // Client
        CircularBuffer<InputPaylod> clientInputBuffer;
        CircularBuffer<StatePayload> clientStateBuffer;
        StatePayload lastServerState;
        StatePayload lastProcessedState;
        float reconciliationThreshold = .5f;

        // Server
        CircularBuffer<StatePayload> serverStateBuffer;
        Queue<InputPaylod> serverInputQueue;

        #endregion

        NetworkVariable<PlayerInfo> playerInfo = new NetworkVariable<PlayerInfo>();
        public PlayerInfo PlayerInfo
        {
            get { return playerInfo.Value; }
        }

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();

            // Init netcode for p&r
            clientInputBuffer = new CircularBuffer<InputPaylod> (bufferSize);
            clientStateBuffer = new CircularBuffer<StatePayload> (bufferSize);
            serverStateBuffer = new CircularBuffer<StatePayload>(bufferSize);
            serverInputQueue = new Queue<InputPaylod> ();
            timer = new NetworkTimer(serverTickRate);
        }

        private void Update()
        {
            if (IsOwner)
                CheckInput();

            // Update the network timer 
            timer.Update(Time.deltaTime);

        }

        private void FixedUpdate()
        {
            // If time to tick then tick
            if (timer.TimeToTick())
            {
                HandleClientTick();
                HandleServerTick();
            }
        }

        void HandleClientTick()
        {
            if (!IsClient) return;

            // Get the current tick and buffer index
            int currentTick = timer.CurrentTick;
            var bufferIndex = currentTick % bufferSize;

            // Create the input payload
            InputPaylod input = new InputPaylod()
            {
                tick = currentTick,
                inputVector = inputMove
            };
            // Add the input to the buffer
            clientInputBuffer.Add(input, bufferIndex);

            // Send the input to the server
            SendToServerRpc(input);

            // Process locally
            StatePayload statePayload = ProcessMove(input.inputVector, input.tick);
            clientStateBuffer.Add(statePayload, bufferIndex);
            UnityEngine.Debug.Log(statePayload);

            HandleReconciliation();
        }

        bool ReconciliationAllowed()
        {
            bool lastServerStateIsDefined = !lastServerState.Equals(default);
            bool lastProcessedStateUndefinedOrDifferent = lastProcessedState.Equals(default) || !lastProcessedState.Equals(lastServerState);

            return lastServerStateIsDefined && lastProcessedStateUndefinedOrDifferent;

        }

        void HandleReconciliation()
        {
            if (IsHost || !ReconciliationAllowed())
                return;

            float positionError;
            int bufferIndex;
            StatePayload rewindState = default;

            // Get the last state buffer index
            bufferIndex = lastServerState.tick % bufferSize;
            
            if (bufferIndex /*- 1*/ < 0) return; // Not enough data to reconcile

            // If we are the host there is no latency, so the last server state has not been process
            rewindState = /*IsHost ? serverStateBuffer.Get(bufferIndex - 1) :*/ lastServerState;

            positionError = Vector3.Distance(rewindState.position, clientStateBuffer.Get(bufferIndex).position);
            if(positionError > reconciliationThreshold)
            {
                ReconcileState(rewindState);
            }

            lastProcessedState = rewindState;
        }

        void ReconcileState(StatePayload state)
        {
            Debug.Log($"Reconciliation tick:{state.tick}");

            rb.position = state.position;
            transform.rotation = state.rotation;
            rb.velocity  = state.velocity;

            // Add the state to the client buffer
            clientStateBuffer.Add(state, state.tick);

            // Replay all the input from the rewind state to the current state
            int tickToReplay = state.tick;
            while(tickToReplay < timer.CurrentTick)
            {
                int bufferIndex = tickToReplay % bufferSize;
                StatePayload clientState = ProcessMove(clientInputBuffer.Get(bufferIndex).inputVector, tickToReplay);
                clientStateBuffer.Add(clientState, bufferIndex);
                tickToReplay++;
            }


        }

        void HandleServerTick()
        {
            if (!IsServer) return;

            var bufferIndex = -1;
            while (serverInputQueue.Count > 0)
            {
                // Get the first input payload
                var input = serverInputQueue.Dequeue();
                // Get the buffer index
                bufferIndex = input.tick % bufferSize;
                StatePayload state = SimulateMove(input.inputVector, input.tick);
                UnityEngine.Debug.Log(state);
                serverStateBuffer.Add(state, bufferIndex);
            }

            if (bufferIndex == -1) return;
            SendToClientRpc(serverStateBuffer.Get(bufferIndex));
        }

        [ServerRpc]
        void SendToServerRpc(InputPaylod input)
        {
            // Return if we are the host
            if (IsHost) return;
            // Put at the end of the queue
            serverInputQueue.Enqueue(input);
        }

        [ClientRpc]
        void SendToClientRpc(StatePayload state)
        {
            if (!IsOwner) return;
            lastServerState = state;
        }



        StatePayload SimulateMove(Vector2 inputMove, int tick)
        {
            //Physics.simulationMode = SimulationMode.Script;
            Move(inputMove);
            Physics.Simulate(Time.fixedDeltaTime);
            //Physics.simulationMode = SimulationMode.FixedUpdate;

            return new StatePayload()
            {
                tick = tick,
                position = rb.position,
                rotation = transform.rotation,
                velocity = rb.velocity
            };
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            //Debug.Log($"New player spawned, owner:{playerInfo.Value}");

            //PlayerController pc = GetComponent<PlayerController>();
            PlayerControllerManager.Instance.AddPlayerController(this);
            if (IsServer)
            {
                // Server must set the player ownership
                NetworkObject no = GetComponent<NetworkObject>();
                no.ChangeOwnership(PlayerInfo.ClientId);
            }
        }

        StatePayload ProcessMove(Vector2 inputMove, int tick)
        {
            //DoUpdate(input.inputVector, false, false);
            Move(inputMove);
            return new StatePayload()
            {
                tick = tick,
                position = rb.position,
                rotation = transform.rotation,
                velocity = rb.velocity
            };
        }

       
        private void Move(Vector2 moveInput)
        {
            // Normalize input 
            moveInput.Normalize();
            float speed = rb.velocity.magnitude;
            if (moveInput.magnitude > 0)
            {
                transform.forward = new Vector3(moveInput.x, 0f, moveInput.y);
                speed += acceleration * Time.fixedDeltaTime;
                if (speed > maxSpeed) speed = maxSpeed;
            }
            else
            {
                speed -= deceleration * Time.fixedDeltaTime;
                if(speed < 0) speed = 0;
            }
            
            rb.velocity = transform.forward * speed;
        }


        //void DoUpdate(Vector2 moveInput, bool shootDown, bool passDown)
        //{
        //    // Normalize input 
        //    moveInput.Normalize();

        //    if (moveInput == Vector2.zero)
        //        moving = false;
        //    else
        //        moving = true;

        //    Vector3 targetDirection = new Vector3(moveInput.x, 0, moveInput.y).normalized;
        //    float angle = 0;
        //    if (moving)
        //    {
        //        // We check target direction only if player is moving
        //        angle = Vector3.SignedAngle(transform.forward, targetDirection, Vector3.up);
        //        angle = Mathf.MoveTowardsAngle(0f, angle, rotationSpeed * Time.deltaTime);
        //        // Apply direction
        //        transform.forward = Quaternion.AngleAxis(angle, Vector3.up) * transform.forward;
        //    }

        //    // We always check for speed
        //    float targetSpeed = 0;
        //    float acc = deceleration;
        //    if (moveInput != Vector2.zero)
        //    {
        //        targetSpeed = speed;
        //        acc = acceleration;
        //    }

        //    float cSpeed = currentVelocity.magnitude;
        //    Vector3 cDir = currentVelocity.normalized;
        //    if(cDir == Vector3.zero)
        //        cDir  = transform.forward;
                

        //    cSpeed = Mathf.MoveTowards(cSpeed, targetSpeed, acc * Time.deltaTime);
        //    angle = Vector3.SignedAngle(cDir, targetDirection, Vector3.up);
        //    float mul = Mathf.Lerp(rotationSpeed*0.05f, rotationSpeed * .7f, (speed - cSpeed) / speed);
        //    angle = Mathf.MoveTowardsAngle(0f, angle, rotationSpeed * mul * Time.deltaTime);
            
        //    cDir = Quaternion.AngleAxis(angle, Vector3.up) * cDir;
        //    currentVelocity = cSpeed * cDir;
        //    cc.Move(transform.forward * cSpeed * Time.deltaTime);
            

        //}

        /// <summary>
        /// To replace with an input handler ( in order to support AI )
        /// </summary>
        void CheckInput()
        {
            inputMove = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
            
        }

        public void Init(PlayerInfo owner)
        {
            this.playerInfo.Value = owner;
        }



    }

}
