using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using WLL_NGO.Interfaces;
using static UnityEngine.UI.GridLayoutGroup;

namespace WLL_NGO.Netcode
{
    

    /// <summary>
    /// Player controller with server authority and client side prediction and reconciliation explained.
    /// The game time is splitted in frames ( called ticks ) with length equal to 1/serverFrameRate ( ex: 1/60ms=1.67ms ) ( Check the NetworkTimer class ).
    /// On each tick the client reads the input and moves the player accordingly storing the new state to the client state buffer; at the same time stores the input and 
    /// the current tick in another buffer and sent it to the server ( check HandleClientTick() );. 
    /// The server receives from the client the input which is used to move the player by simulating the physics tick by tick; each new state is then stored in a state 
    /// buffer with the corresponding tick; the last processed state on server is then sent back to the client ( check HandleServerTick() ).
    /// The client receives the last state from the server and compares it with the state having the same tick from its buffer: if these two states are not equal a 
    /// reconciliation is needed.
    /// If a reconciliation is needed the client sets the player back to the last received state resimulating the physics from that tick to the current tick ( the reason we
    /// need to resimulate is that the state received by the server is behind the client current tick due to the lag ).
    /// If a player is not a local player ( ex. the opponent team ) or is not selected ( you can select only one player at the time in your team ) we call the 
    /// ApplyNotOwnedOrUnselectedClientState(lastServerState) with the last server state we just received ( basically is kind of a reconciliation without 
    /// any prediction ); this happens because we don't have any input in order to make any prediction client side: if the player is owned by another client the input 
    /// is from that client and if we owned the player but its not the selected one at this moment then the input is directly set on the server by the AI.
    /// 
    /// Input controller.
    /// The player controller always receives input both from human and AI; this means that the AI never tells the player to move or shoot, instead sets the input 
    /// like a human would do ( for example to shoot the AI sets the corresponding button to true and after a while sets it back to false thus triggering 
    /// the shooting routine ).
    /// 
    /// </summary>

    public class PlayerController : NetworkBehaviour
    {
        public static UnityAction<PlayerController> OnSpawned;

        public struct InputPaylod : INetworkSerializable
        {
            public int tick;
            public Vector2 inputVector;
            public bool button1;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref tick);
                serializer.SerializeValue(ref inputVector);
                serializer.SerializeValue(ref button1);
            }
        }

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
                return $"[StatePayload tick:{tick}, position:{position}, rotation:{rotation}, velocity:{velocity}, angularVelocity:{angularVelocity}]";
            }
        }

        NetworkVariable<byte> playerState = new NetworkVariable<byte>((byte)PlayerState.Normal);



        [SerializeField]
        float maxSpeed = 5f;

        [SerializeField]
        float rotationSpeed = 480;


        [SerializeField]
        float acceleration = 10f;

        [SerializeField]
        float deceleration = 30f;

        /// <summary>
        /// When the ball enters this trigger the player can become the owner under certain conditions.
        /// </summary>
        [SerializeField]
        Collider ballHandleTrigger;

        bool canHandleTheBall = true;
        Rigidbody rb;

        bool moving = false;
        Vector3 currentVelocity = Vector3.zero;
        bool Selected 
        {
            get { return true; }
        }

        #region input data
        [SerializeField]
        IInputHandler inputHandler;
        InputData input;
        bool button1LastValue, button2LastValue, button3LastValue;
        #endregion

        #region netcode prediction and reconciliation
        // General
        NetworkTimer timer;
        float serverTickRate = Constants.ServerTickRate;
        int bufferSize = 1024;

        // Client
        CircularBuffer<InputPaylod> clientInputBuffer;
        CircularBuffer<StatePayload> clientStateBuffer;
        StatePayload lastServerState = default;
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
            if (IsOwner && Selected)
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

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            //Debug.Log($"New player spawned, owner:{playerInfo.Value}");

            PlayerControllerManager.Instance.AddPlayerController(this);
            if (IsServer)
            {
                // Server must set the player ownership
                NetworkObject no = GetComponent<NetworkObject>();
                no.ChangeOwnership(PlayerInfo.ClientId);
            }

            // Player state changed event handler
            playerState.OnValueChanged += HandleOnPlayerStateChanged;

            OnSpawned?.Invoke(this);
        }

        #region buttons
        /// <summary>
        /// Called on both client and server
        /// </summary>
        /// <param name="value"></param>
        /// <param name="tick"></param>
        /// <param name="client"></param>
        void CheckButton1(bool value, int tick, bool client)
        {
            //if ((client && IsServer) || (!client && !IsServer))
            //    return;

            if (!IsServer)
                return;

            if(value != button1LastValue)
            {
                if (value)
                {
                    // Start charging
                    Debug.Log("Start charging...");
                }
                else
                {
                    //if (IsServer)
                    {
                        Debug.Log("Shoot");
                        // Shoot
                        Vector3 dir = new Vector3(0.5f, .5f, 0f);
                        if (BallController.Instance.transform.position.x > 0)
                            dir = new Vector3(-0.5f, .5f, 0f);
                        int aheadTick = 32;
                        BallController.Instance.ShootAtTick(this, dir.normalized * 10, tick + aheadTick);
                    }
                    
                 
                }
            }
            else
            {
                if (value)
                {
                    // Charge shot
                    Debug.Log("Keep charging...");
                }
            }
            button1LastValue = value;
        }

        
        #endregion



        #region predicted movement
        void HandleClientTick()
        {
            if (!IsClient) return;

            if (IsOwner && Selected) // If the client is the owner and the player is selected you can read input and send data to the server
            {
                // Get the current tick and buffer index
                int currentTick = timer.CurrentTick;
                var bufferIndex = currentTick % bufferSize;

                // Create the input payload
                InputPaylod payload = new InputPaylod()
                {
                    tick = currentTick,
                    inputVector = input.joystick,
                    button1 = input.button1
                };
                // Add the input to the buffer
                clientInputBuffer.Add(payload, bufferIndex);

                // Send the input to the server
                SendToServerRpc(payload);

                if (!IsHost)
                {
                    //
                    // Process movement locally
                    //
                    StatePayload statePayload = ClientProcessMovement(payload.inputVector, payload.tick);
                    clientStateBuffer.Add(statePayload, bufferIndex);
                    //UnityEngine.Debug.Log(statePayload);
                    HandleReconciliation();

                    //
                    // Check buttons ( we can check directly on server )
                    //
                    //CheckButton1(payload.button1, currentTick, true); 
                }
                
            }
            else // Not the owner or the selected one ( controlled by another client or the AI or busy in someway, for example stunned )
            {
                // Simply get the last state processed by the server and apply it
                ApplyNotOwnedOrUnselectedClientState(lastServerState);
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

                //
                // Simulate movement
                //
                StatePayload state = ServerSimulateMovement(input.inputVector, input.tick);
                //UnityEngine.Debug.Log(state);
                serverStateBuffer.Add(state, bufferIndex);

                //
                // Check buttons
                //
                CheckButton1(input.button1, input.tick, false);
            }

            if (bufferIndex == -1) return; // No data
            // We send the all clients the last state processed by the server
            SendToClientRpc(serverStateBuffer.Get(bufferIndex));
        }

        /// <summary>
        /// Called by the client to apply server state to every not owned or unselected player
        /// </summary>
        /// <param name="state"></param>
        void ApplyNotOwnedOrUnselectedClientState(StatePayload state)
        {
            if (state.Equals(default))
                return;
            WriteTransform(state.position, state.rotation, state.velocity, state.angularVelocity);
            lastProcessedState = state;

        }

        /// <summary>
        /// If last state on server is not null and last processed state on client is null or different than last server state then we check for reconciliation
        /// </summary>
        /// <returns></returns>
        bool ReconciliationAllowed()
        {
            bool lastServerStateIsDefined = !lastServerState.Equals(default);
            bool lastProcessedStateUndefinedOrDifferent = lastProcessedState.Equals(default) || !lastProcessedState.Equals(lastServerState);

            return lastServerStateIsDefined && lastProcessedStateUndefinedOrDifferent;

        }

        void HandleReconciliation()
        {
            // If we are playing as the host we don't need reconciliation at all
            if (!ReconciliationAllowed())
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

        /// <summary>
        /// Reconciliate the selected player if needed
        /// </summary>
        /// <param name="state"></param>
        void ReconcileState(StatePayload state)
        {
            Debug.Log($"Reconciliation tick:{state.tick}");

            // Get the last server state data
            WriteTransform(state.position, state.rotation, state.velocity, state.angularVelocity);
           
            // Add to the client buffer
            clientStateBuffer.Add(state, state.tick);

            // Replay all the input from the rewind state to the current state
            int tickToReplay = state.tick;
            while(tickToReplay < timer.CurrentTick)
            {
                int bufferIndex = tickToReplay % bufferSize;
                StatePayload clientState = ClientProcessMovement(clientInputBuffer.Get(bufferIndex).inputVector, tickToReplay);
                clientStateBuffer.Add(clientState, bufferIndex);
                tickToReplay++;
            }
        }
       
        /// <summary>
        /// Send this controller data to the server
        /// </summary>
        /// <param name="input"></param>
        [ServerRpc]
        void SendToServerRpc(InputPaylod input)
        {
            // We don't need to send data to the server if we are the host
            //if (IsHost) return;
            // Put at the end of the queue
            serverInputQueue.Enqueue(input);
            Debug.Log($"Server input:{input.button1}");
        }

        /// <summary>
        /// Update player on each client
        /// </summary>
        /// <param name="state"></param>
        [ClientRpc]
        void SendToClientRpc(StatePayload state)
        {
            lastServerState = state;
        }


        /// <summary>
        /// Called on the server to simulate movement of any player controlled by a client or the AI.
        /// </summary>
        /// <param name="inputMove"></param>
        /// <param name="tick"></param>
        /// <returns></returns>
        StatePayload ServerSimulateMovement(Vector2 inputMove, int tick)
        {
            //Physics.simulationMode = SimulationMode.Script;
            Move(inputMove);
            Physics.Simulate(Time.fixedDeltaTime);
            //Physics.simulationMode = SimulationMode.FixedUpdate;

            StatePayload state = ReadTransform();
            state.tick = tick;
            return state;
        }

       
        /// <summary>
        /// Called on the client to apply movement to the local selected player
        /// </summary>
        /// <param name="inputMove"></param>
        /// <param name="tick"></param>
        /// <returns></returns>
        StatePayload ClientProcessMovement(Vector2 inputMove, int tick)
        {
            // Move player and return the state payload
            Move(inputMove);
            StatePayload state = ReadTransform();
            state.tick = tick;
            return state;
           
        }

       
        private void Move(Vector2 moveInput)
        {
            switch (playerState.Value)
            {
                case (byte)PlayerState.Normal:
                    UpdateNormalMovement(moveInput);
                    break;
                case (byte)PlayerState.Stunned:
                    UpdateStunnedMovement();
                    break;
            }

            
        }

        void UpdateNormalMovement(Vector2 moveInput)
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
                if (speed < 0) speed = 0;
            }

            rb.velocity = transform.forward * speed;
        }

        void UpdateStunnedMovement()
        {

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

        #region networked variables
        /// <summary>
        /// Called on client
        /// </summary>
        /// <param name="oldState"></param>
        /// <param name="newState"></param>
        void HandleOnPlayerStateChanged(byte oldState, byte newState)
        {
            
        }

        

        #endregion

        #region misc
        /// <summary>
        /// To replace with an input handler ( in order to support AI )
        /// </summary>
        void CheckInput()
        {
            if (inputHandler == null || !IsOwner || !Selected) 
                return;
            
            input = inputHandler.GetInput();
            Debug.Log($"Client input:{input}");
        }

        private void OnTriggerEnter(Collider other)
        {
            if (playerState.Value != (byte)PlayerState.Normal && playerState.Value != (byte)PlayerState.ReceivingPassage)
                return;

            // It's the ball
            if (other.CompareTag(Tags.Ball))
            {
                if(canHandleTheBall) 
                    BallController.Instance.BallEnterTheHandleTrigger(this);
            }

            
        }

        private void OnTriggerExit(Collider other)
        {
            // It's the ball
            if (other.CompareTag(Tags.Ball))
            {
                BallController.Instance.BallExitTheHandleTrigger(this);
            }
        }

        /// <summary>
        /// Called both on client and server
        /// </summary>
        public void StartHandlingTheBall()
        {
            canHandleTheBall = true;
        }

        /// <summary>
        /// Called both on client and server
        /// </summary>
        public void StopHandlingTheBall()
        {
            canHandleTheBall = false;
        }

        #endregion

        #region initialization

        /// <summary>
        /// Called by the server when a new player controller is spawned
        /// </summary>
        /// <param name="owner"></param>
        public void Init(PlayerInfo owner)
        {
            this.playerInfo.Value = owner;
        }

        public void SetInputHandler(IInputHandler inputHandler)
        {
            Debug.Log($"PlayerController setting input handler:{inputHandler}");
            this.inputHandler = inputHandler;
        }
        #endregion



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





    }

}
