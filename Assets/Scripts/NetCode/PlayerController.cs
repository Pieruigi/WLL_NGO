using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Services.Matchmaker.Models;
using UnityEngine;
using UnityEngine.Events;
using WLL_NGO.AI;
using WLL_NGO.Interfaces;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.Rendering.DebugUI;
using static UnityEngine.UI.GridLayoutGroup;

namespace WLL_NGO.Netcode
{


    /// <summary>
    /// Player controller with server authority and client side prediction and reconciliation.
    /// The game time is splitted in frames ( called ticks ); each frame lasts 1/serverFrameRate ( ex: 1/60ms=1.67ms ) ( Check the NetworkTimer class ).
    /// On each tick the client reads the input and moves the player accordingly, then stores the new state in a buffer, the input and the current tick in another buffer and 
    /// sends both to the server ( check HandleClientTick() );. 
    /// The server receives the input which is used to move the player by simulating the physics tick by tick; each new state is then stored in a state 
    /// buffer with the corresponding tick; the last processed state on server is then sent back to the client ( check HandleServerTick() ).
    /// The client receives the last state from the server and compares it to the state with the same tick previously stored in its buffer: if there is some difference then a
    /// reconciliation is made.
    /// If a reconciliation is needed the client sets the player back to the last received state resimulating the physics from that tick to the current tick ( the reason we
    /// need to resimulate is that the state received by the server is behind the client current tick due to the lag ).
    /// If a player is not a local player ( ex. the opponent team ) or is not selected ( you can select only one player at the time in your team ) we call the 
    /// ApplyNotOwnedOrUnselectedClientState(lastServerState) with the last server state we just received ( basically is kind of a reconciliation without 
    /// any prediction ); this happens because we don't have input to make any prediction client side: if the player is owned by another client the input 
    /// is from that client and if we own the player but its not the selected one then input is set on server by the AI.
    /// 
    /// Input controller.
    /// The player controller always receives input from both human and AI; this means that the AI never tells the player to move or shoot by calling the controller, instead sets the input 
    /// like a human would do ( for example to shoot the AI sets the corresponding button to true and after a while sets it back to false, thus triggering 
    /// the shooting routine ). Since the IA only works on server, the input will be enqued in the server input queue without being passed by the client.
    /// 
    /// Animations.
    /// Server always has authority on animations so you can not play an animation on client.
    /// 
    /// </summary>

    public class PlayerController : NetworkBehaviour
    {
        public static UnityAction<PlayerController> OnSpawned;

        public struct InputPayload : INetworkSerializable
        {
            public int tick;
            public Vector2 inputVector;
            public bool button1;
            public bool button2;
            public bool button3;
            public bool button4;


            public InputPayload(InputData inputData, int tick)
            {

                inputVector = inputData.joystick;
                button1 = inputData.button1;
                button2 = inputData.button2;
                button3 = inputData.button3;
                button4 = inputData.button4;
                this.tick = tick;
            }


            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref tick);
                serializer.SerializeValue(ref inputVector);
                serializer.SerializeValue(ref button1);
                serializer.SerializeValue(ref button2);
                serializer.SerializeValue(ref button3);
                serializer.SerializeValue(ref button4);
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

        [System.Serializable]
        struct PlayerStateInfo : INetworkSerializable
        {
            public byte state; // The main state ( ex. 'stunned' )
            public byte subState; // The substate ( ex. stunned by 'drop kick' )
            public byte detail; // Some detail ( ex. 'back' stunned by drop kick )

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref state);
                serializer.SerializeValue(ref subState);
                serializer.SerializeValue(ref detail);
            }

            public override bool Equals(object obj)
            {
                if (obj == null)
                    return false;

                if (System.Object.ReferenceEquals(this, obj))
                    return true;

                if (this.GetType() != obj.GetType())
                    return false;



                if (this.state != ((PlayerStateInfo)obj).state || this.subState != ((PlayerStateInfo)obj).subState || this.detail != ((PlayerStateInfo)obj).detail)
                    return false;

                return true;
            }

            public static bool operator ==(PlayerStateInfo l, PlayerStateInfo r)
            {
                return l.Equals(r);
            }
            public static bool operator !=(PlayerStateInfo l, PlayerStateInfo r) => !(l == r);

            public override int GetHashCode() => (state, subState, detail).GetHashCode();

        }



        [SerializeField]
        float maxSpeed = 5f;
        public float MaxSpeed
        {
            get { return maxSpeed; }
        }

        [SerializeField]
        float sprintMultiplier = 2f;

        [SerializeField]
        float rotationSpeed = 480;
        public float RotationSpeed
        {
            get { return rotationSpeed; }
        }


        [SerializeField]
        float acceleration = 10f;

        [SerializeField]
        float deceleration = 30f;

        [SerializeField]
        float diveMaxSpeed = 5;

        public Vector3 Position
        {
            get { return rb.position; }
            set { rb.position = value; }
        }

        public Quaternion Rotation
        {
            get { return rb.rotation; }
            set { rb.rotation = value; }
        }

        public Vector3 Velocity
        {
            get { return rb.velocity; }
            set { rb.velocity = value; }
        }

        float staminaMax = 3; // One for each step
        public float MaxStamina
        {
            get{ return staminaMax; }
        }
        

        NetworkVariable<float> stamina = new NetworkVariable<float>(3);
        public float CurrentStamina
        {
            get{ return stamina.Value; }
        }

        NetworkVariable<bool> sprinting = new NetworkVariable<bool>(false);

        DateTime lastStaminaDecrease;
        float replenishStaminaRate = .2f;
        float staminaReplenishDelay = 1f;

        float lightTackleStaminaCost = 1;

        float heavyTackleStaminaCost = 2;

        #region action fields
        //NetworkVariable<byte> playerState = new NetworkVariable<byte>((byte)PlayerState.Normal);
        NetworkVariable<PlayerStateInfo> playerStateInfo = new NetworkVariable<PlayerStateInfo>(new PlayerStateInfo() { state = (byte)PlayerState.Normal, subState = 0, detail = 0 });

       
        NetworkVariable<float> charge = new NetworkVariable<float>(0);
        public float Charge
        {
            get{ return charge.Value; }
        }
        float chargingSpeed = 1;
        float lightTackleChargeAmount = .2f;
        Vector3 lookDirection = Vector3.zero;

        bool _button4 = false;
        DateTime lastDownButton4;

        float doubleTapRate = .5f;


        float sprintDelay = .25f;

        float staminaSprintingRate = 10;

        float currentSpeed = 0;
        /// <summary>
        /// Used to give more detail about a specific state ( ex. what type of tackle the player is doing ).
        /// </summary>
        //NetworkVariable<byte> playerSubState = new NetworkVariable<byte>(0);
        //byte actionType = 0;
        float playerStateCooldown = 0; // Server only
        public float StateCooldown
        {
            get{ return playerStateCooldown; }
        }
        Animator animator;
        string tackleAnimTrigger = "Tackle";
        string stunAnimTrigger = "Stun";
        string diveAnimTrigger = "Dive";
        string typeAnimParam = "Type";
        string detailAnimParam = "Detail";
        string exitAnimParam = "Exit";

        string loopAnimParam = "Loop";

        float jumpHeightThreshold = 1.7f;
        float passageTime = 1f; //UnityEngine.Random.Range(.8f, 1.2f);

        int customTickTarget = 0;

        int role = -1;
        public PlayerRole Role
        {
            get { return (PlayerRole)role; }
        }

        public float PlayerHeight
        {
            get { return 1.7f; }
        }


        UnityAction diveUpdateFunction;
        #endregion

        /// <summary>
        /// When the ball enters this trigger the player can become the owner under certain conditions.
        /// </summary>
        [SerializeField]
        BallHandlingTrigger ballHandlingTrigger;

        [SerializeField]
        Transform ballHook;
        Transform ballHookDefault;

        NetworkVariable<bool> useGoalkeeperBallHook = new NetworkVariable<bool>(false);
        public bool UseGoalkeeperBallHook
        {
            get{ return useGoalkeeperBallHook.Value; }
        }

        public bool HasBall
        {
            get { return BallController.Instance.Owner == this; }
        }

        bool handlingTheBall = false;
        float ballHookLerpSpeed = 10f;

        Rigidbody rb;


        bool Selected
        {
            get { return TeamController.GetPlayerTeam(this).SelectedPlayer == this; }
        }

        #region input and gameplay data 
        [SerializeField]
        IInputHandler inputHandler;
        InputData input;
        bool button1LastValue, button2LastValue, button3LastValue;


        #endregion

        #region netcode prediction and reconciliation
        // General
        int bufferSize = 1024;

        // Client
        CircularBuffer<InputPayload> clientInputBuffer;
        CircularBuffer<StatePayload> clientStateBuffer;
        StatePayload lastServerState = default;
        StatePayload lastProcessedState;
        float reconciliationThreshold = 0.25f;
        float reconciliationSpeed = 4f;

        // Server
        CircularBuffer<StatePayload> serverStateBuffer;
        Queue<InputPayload> serverInputQueue;

        #endregion

        //NetworkVariable<NetworkObjectReference> playerInfoRef = new NetworkVariable<NetworkObjectReference>(default);
        NetworkVariable<FixedString32Bytes> playerInfoId = new NetworkVariable<FixedString32Bytes>(default);

        //NetworkVariable<PlayerInfo> playerInfo = new NetworkVariable<PlayerInfo>();
        PlayerInfo playerInfo;
        public PlayerInfo PlayerInfo
        {
            get { return playerInfo; }
        }

        NetworkVariable<byte> index = new NetworkVariable<byte>(0);
        public int Index
        {
            get { return index.Value; }
        }

        GoalkeeperAI goalkeeperAI;
        //public GoalkeeperAI GoalkeeperAI { get { return goalkeeperAI; } }

        private void Awake()
        {
            // Get the rigidbody
            rb = GetComponent<Rigidbody>();

            // Get the animator
            animator = GetComponent<Animator>();

            //// Set handling trigger callbacks
            //ballHandlingTrigger.OnBallEnter += HandleOnBallEnter;
            //ballHandlingTrigger.OnBallExit += HandleOnBallExit;
            ballHookDefault = ballHook;
            goalkeeperAI = GetComponent<GoalkeeperAI>();

            // Init netcode for p&r
            clientInputBuffer = new CircularBuffer<InputPayload>(bufferSize);
            clientStateBuffer = new CircularBuffer<StatePayload>(bufferSize);
            serverStateBuffer = new CircularBuffer<StatePayload>(bufferSize);
            serverInputQueue = new Queue<InputPayload>();

            //MatchController.Instance.OnStateChanged += HandleOnMatchStateChanged;
        }


        private void Update()
        {
            if (!IsSpawned)
                return;



            if (IsOwner) // Is client or host
            {
                if (Selected)
                    CheckHumanInput();
                else
                    CheckNotHumanInput(); // AI for singleplayer
            }
            else // Client is the owner, so we are on the dedicated server here
            {
                if (!Selected)
                    CheckNotHumanInput(); // AI for multiplayer
            }


            // Check the cooldown
            UpdateActionCooldown();

        }

        void OnEnable()
        {
            MatchController.OnStateChanged += HandleOnMatchStateChanged;
        }

        void OnDisable()
        {
            MatchController.OnStateChanged -= HandleOnMatchStateChanged;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            PlayerControllerManager.Instance.AddPlayerController(this);

            // Set player info on both client and server
            playerInfo = PlayerInfoManager.Instance.GetPlayerInfoById(playerInfoId.Value.ToString());

            if (IsServer)
            {

                // Server must set the player ownership
                NetworkObject no = GetComponent<NetworkObject>();
                no.ChangeOwnership(PlayerInfo.ClientId);


                // Set handling trigger callbacks
                ballHandlingTrigger.OnBallEnter += HandleOnBallEnter;
                ballHandlingTrigger.OnBallExit += HandleOnBallExit;
            }


            // Change object name on both client and server
            name = $"Player_{PlayerControllerManager.Instance.PlayerControllers.Count}_{playerInfo.Home}";


            // Player state changed event handler
            playerStateInfo.OnValueChanged += HandleOnPlayerStateInfoChanged;
            // Ball hook variable
            useGoalkeeperBallHook.OnValueChanged += HandleOnUseGoalkeeperBallHookChanged;

            NetworkTimer.Instance.OnTimeToTick += HandleOnTimeToTick;

            OnSpawned?.Invoke(this);


        }

        

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
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

            // Both
            CheckForBallHandling();

        }

        private void HandleOnMatchStateChanged(int oldValue, int newValue)
        {
            switch (newValue)
            {
                case (byte)MatchState.StartingMatch:
                    //timer = new NetworkTimer();
                    break;
                case (byte)MatchState.Goal:
                    inputHandler.ResetInput();
                    if (Role != PlayerRole.GK && playerStateInfo.Value.state == (byte)PlayerState.Normal)
                    {
                        Velocity = Vector3.zero;
                        //currentSpeed = 0;
                    }

                    break;
            }


        }

        private void HandleOnUseGoalkeeperBallHookChanged(bool previousValue, bool newValue)
        {
            if (newValue)
                ballHook = goalkeeperAI.GetBallHook();
            else
                ballHook = ballHookDefault;
        }

        #region buttons
        /// <summary>
        /// Called on both client and server
        /// Pass.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="tick"></param>
        /// <param name="client"></param>
        //void CheckButton1(/*bool value, */int tick, bool client)
        void CheckButton1(InputPayload inputPayload, bool client)
        {

            if (!IsServer)
                return;

            // If match state is kickoff and the button1 has been pressed we must set the state to playing

            bool value = inputPayload.button1;

            if (MatchController.Instance.MatchState == MatchState.KickOff && value)
                MatchController.Instance.SetMatchState(MatchState.Playing);

            if (handlingTheBall)
                CheckForShootingInput(value, button1LastValue, inputPayload.tick, isPass:true);
            else if (playerStateInfo.Value.state == (byte)PlayerState.Receiver)
                CheckForShootingInputAsReceiver(value, button1LastValue, inputPayload.tick, isPass: true, passDirection: inputPayload.inputVector);
            else
                CheckForTacklingInput(value, button1LastValue, inputPayload.tick);

            button1LastValue = value;
        }

        /// <summary>
        /// Shoot on goal.
        /// </summary>
        /// <param name="inputPayload"></param>
        /// <param name="client"></param>
        void CheckButton2(InputPayload inputPayload, bool client)
        {


            if (!IsServer)
                return;

            bool value = inputPayload.button2;

            if (Role == PlayerRole.GK && handlingTheBall && useGoalkeeperBallHook.Value) // Goalkeeper handling the ball
            {
                CheckForBallRelease(value, button2LastValue, inputPayload.tick);
            }
            else
            {
                if (handlingTheBall)
                    CheckForShootingInput(value, button2LastValue, inputPayload.tick, isPass: false);
                else if (playerStateInfo.Value.state == (byte)PlayerState.Receiver)
                    CheckForShootingInputAsReceiver(value, button2LastValue, inputPayload.tick, isPass: false);
                else
                    CheckForSwitchingInput(value, button1LastValue, inputPayload.tick); // Check for switching player    
            }
            

            button2LastValue = value;
        }

        /// <summary>
        /// Power up button
        /// </summary>
        /// <param name="inputPayload"></param>
        /// <param name="client"></param>
        void CheckButton3(InputPayload inputPayload, bool client)
        {
            if (!IsServer)
                return;

            // For power up    
        }

        /// <summary>
        /// Tap screen (for both spint and dribbling)
        /// </summary>
        /// <param name="inputPayload"></param>
        /// <param name="client"></param>

        void CheckButton4(InputPayload inputPayload, bool client)
        {
            if (!IsServer)
                return;

            bool doubleTap = false;
            bool sprinting = false;

            bool value = inputPayload.button4;

            if (!_button4 && value)
            {
                if ((DateTime.Now - lastDownButton4).TotalSeconds < doubleTapRate)
                    doubleTap = true;

                lastDownButton4 = DateTime.Now;

            }

            if (doubleTap)
            {
                // Do dribbling here
                Debug.Log("TEST - Dribbling");
            }
            else
            {
                if (value)
                {
                    if ((DateTime.Now - lastDownButton4).TotalSeconds > sprintDelay)
                    {
                        // Do sprint
                        Debug.Log("TEST - Sprinting");
                        
                        sprinting = true;
                    }
                }
                else
                {
                    
                    sprinting = false;
                }

            }

            // Check stamina
            if (sprinting)
            {
                if (stamina.Value == 0)
                {
                    sprinting = false;
                }
                else
                {
                    var amount = staminaSprintingRate * NetworkTimer.Instance.DeltaTick;

                    DecreaseStamina(amount);    
                    
                }
            }

            if (this.sprinting.Value != sprinting)
                this.sprinting.Value = sprinting;

            doubleTap = false;
            _button4 = value;
                
            
        }


        private void CheckForShootingInputAsReceiver(bool buttonValue, bool buttonLastValue, int tick, bool isPass, Vector2 passDirection = default)
        {
            if (playerStateInfo.Value.state != (byte)PlayerState.Receiver)
                return;

            int state = GetButtonState(buttonValue, buttonLastValue);

            switch (state)
            {
                case (byte)ButtonState.None:
                    break;
                case (byte)ButtonState.Pressed:
                    ResetCharge();
                    IncreaseCharge(NetworkTimer.Instance.DeltaTick);
                    break;
                case (byte)ButtonState.Held:
                    IncreaseCharge(NetworkTimer.Instance.DeltaTick);
                    break;
                case (byte)ButtonState.Released:
                    if (isPass)
                        ProcessPass(tick, passDirection);
                    else
                        ProcessShotOnGoal(tick);
                    break;
            }
        }

        void CheckForTacklingInput(bool buttonValue, bool buttonLastValue, int tick)
        {
            if (playerStateInfo.Value.state != (byte)PlayerState.Normal)
                return;

            int state = GetButtonState(buttonValue, buttonLastValue);
            switch (state)
            {
                case (byte)ButtonState.None: // None

                    break;
                case (byte)ButtonState.Pressed: // Button down
                    ResetCharge();
                    IncreaseCharge(NetworkTimer.Instance.DeltaTick);
                    break;
                case (byte)ButtonState.Held:
                    IncreaseCharge(NetworkTimer.Instance.DeltaTick);
                    break;
                case (byte)ButtonState.Released: // Button up
                    // Do tackle
                    // Check opponent to choose the right action type
                    var ps = playerStateInfo.Value;
                    if (charge.Value < lightTackleChargeAmount)
                    {
                        // Check stamina
                        if (stamina.Value > lightTackleStaminaCost)
                        {
                            DecreaseStamina(lightTackleStaminaCost);
                            ps.subState = (byte)TackleType.Slide;    
                        }
                    }
                    else
                    {
                        if (stamina.Value > heavyTackleStaminaCost)
                        {
                            DecreaseStamina(heavyTackleStaminaCost);
                            ps.subState = (byte)TackleType.Kick;
                        }
                    }
                    charge.Value = 0;
                    ps.state = (byte)PlayerState.Tackling;
                    playerStateInfo.Value = ps;
                    break;
            }
        }

        void CheckForSwitchingInput(bool buttonValue, bool buttonLastValue, int tick)
        {
            int state = GetButtonState(buttonValue, buttonLastValue);

            switch (state)
            {
                case (byte)ButtonState.None:
                    break;
                case (byte)ButtonState.Pressed:
                    TeamController.GetPlayerTeam(this).SwitchPlayer();
                    break;
                case (byte)ButtonState.Held:
                case (byte)ButtonState.Released:
                    break;
                
            }
        }

        void CheckForBallRelease(bool buttonValue, bool buttonLastValue, int tick)
        {
            int state = GetButtonState(buttonValue, buttonLastValue);
            switch (state)
            {
                 case (byte)ButtonState.None:
                    break;
                case (byte)ButtonState.Pressed:
                    break;
                case (byte)ButtonState.Held:
                    break;
                case (byte)ButtonState.Released:
                    // Release the ball
                    useGoalkeeperBallHook.Value = false;
                    break;
            }
        }

        void CheckForShootingInput(bool buttonValue, bool buttonLastValue, int tick, bool isPass)
        {
            bool shoot = false;
            int state = GetButtonState(buttonValue, buttonLastValue);
            switch (state)
            {
                case (byte)ButtonState.None:

                    break;
                case (byte)ButtonState.Pressed:
                    ResetCharge();
                    IncreaseCharge(NetworkTimer.Instance.DeltaTick);
                    break;
                case (byte)ButtonState.Held:
                    IncreaseCharge(NetworkTimer.Instance.DeltaTick);
                    if (charge.Value == 1)
                        shoot = true;
                    break;
                case (byte)ButtonState.Released:
                    shoot = true;
                    break;
            }

            if (shoot)
            {
                if (isPass)
                    ProcessPass(tick);
                else
                    ProcessShotOnGoal(tick);
            }

        }

        /// <summary>
        /// 0: none
        /// 1: down
        /// 2: pressed
        /// 3: up
        /// </summary>
        /// <param name="newValue"></param>
        /// <param name="oldValue"></param>
        /// <returns></returns>
        int GetButtonState(bool newValue, bool oldValue)
        {
            if (newValue && !oldValue)
                return 1;
            if (newValue && oldValue)
                return 2;
            if (!newValue && oldValue)
                return 3;
            return 0;
        }


        void ProcessPass(int tick, Vector2 passDirection = default)
        {

            // You can only pass the ball if you are in the normal or receiver state
            if (playerStateInfo.Value.state == (byte)PlayerState.Normal || playerStateInfo.Value.state == (byte)PlayerState.Receiver)
            {
                // We just want to reach the target in a given time 

                if (playerStateInfo.Value.state == (byte)PlayerState.Normal)
                {
                    PassTheBall();
                }
                else // Receiver
                {
                    PassTheBallOnTheFly(tick, passDirection);
                }

            }
        }

        void PassTheBall()
        {
            // Check for an available receiver
            PlayerController receiver = null;
            if (TryGetAvailableReceiver(out receiver))
            {
                //Debug.Log($"Receiver:{receiver.name}");

                // Get the target 
                Vector3 targetPosition;
                float maxError = 1.5f;
                if (charge.Value > 0.5f)
                {
                    // High passage
                    targetPosition = receiver.rb.position + Vector3.up * UnityEngine.Random.Range(receiver.PlayerHeight*1.5f, receiver.PlayerHeight*2f);
                }
                else
                {
                    // Low passage
                    targetPosition = receiver.rb.position + Vector3.up * UnityEngine.Random.Range(0.5f, receiver.PlayerHeight);

                }

                targetPosition += Vector3.forward * UnityEngine.Random.Range(-maxError, maxError) + Vector3.right * UnityEngine.Random.Range(-maxError, maxError);

                int aheadTick = 0;
                // We have ball control while shooting and the player stops so the ball position won't change
                Vector3 estimatedBallPos = BallController.Instance.GetEstimatedPosition(aheadTick);

                // Compute estimated speed
                float speed = Vector3.Distance(estimatedBallPos, targetPosition) / passageTime;

                // Shoot
                BallController.Instance.ShootAtTick(this, receiver, targetPosition, speed, 0, NetworkTimer.Instance.CurrentTick + aheadTick, isPass: true, isOnTheFly: false);

                SetPlayerStateInfo(new PlayerStateInfo() { state = (byte)PlayerState.Shooting });
                // A bit of cooldown
                playerStateCooldown = .1f;

            }
            else
            {
                Debug.Log("No receiver found");

            }
        }

        async void PassTheBallOnTheFly(int tick, Vector2 passDirection)
        {
            // Disable the handling trigger 
            ballHandlingTrigger.SetEnable(false);

            await OnTheFlyJumpAsync(tick);

            // We must be sure the player is still in its receiving state before we can shoot
            if (playerStateInfo.Value.state != (byte)PlayerState.Receiver)
                return;

            //if(rb.velocity.y < math.EPSILON)
            {
                // We can shoot
                // We must check for an available teammate depending on the player input

                PlayerController receiver = null;
                if (TryGetAvailableReceiver(out receiver, passDirection))
                {

                    // Get the target 
                    Vector3 targetPosition;
                    float maxError = 1.5f;
                    if (charge.Value > 0.5f)
                    {
                        // High passage
                        targetPosition = receiver.rb.position + Vector3.up * UnityEngine.Random.Range(4f, 7f);
                    }
                    else
                    {
                        // Low passage
                        targetPosition = receiver.rb.position + Vector3.up * UnityEngine.Random.Range(0.5f, 1.8f);

                    }

                    targetPosition += Vector3.forward * UnityEngine.Random.Range(-maxError, maxError) + Vector3.right * UnityEngine.Random.Range(-maxError, maxError);

                    int aheadTick = 0;
                    Vector3 estimatedBallPos = BallController.Instance.GetEstimatedPosition(aheadTick);

                    // Compute estimated speed
                    float estBallSpeed = Vector3.Distance(estimatedBallPos, targetPosition) / passageTime;

                    //Debug.Log($"Shooting data - receiver:{receiver}, targetPosition:{targetPosition}, estimatedBallSpeed:{estBallSpeed}");
                    // Shoot
                    BallController.Instance.ShootAtTick(this, receiver, targetPosition, estBallSpeed, 0, NetworkTimer.Instance.CurrentTick + aheadTick, isPass: true, isOnTheFly: true);

                    SetPlayerStateInfo(new PlayerStateInfo() { state = (byte)PlayerState.Shooting });


                }
                else
                {
                    Debug.Log("No receiver found");

                }
            }


        }

        void ProcessShotOnGoal(int tick)
        {
            // You can only pass the ball if you are in the normal or receiver state
            if (playerStateInfo.Value.state == (byte)PlayerState.Normal || playerStateInfo.Value.state == (byte)PlayerState.Receiver)
            {
                // We just want to reach the target in a given time 

                if (playerStateInfo.Value.state == (byte)PlayerState.Normal)
                {
                    ShootOnGoal();
                }
                else // Receiver
                {
                    ShootOnGoalOnTheFly(tick);
                }

            }
        }

        void ShootOnGoal()
        {

            // Get the opponent team net controller
            NetController net = NetController.GetOpponentTeamNetController(TeamController.GetPlayerTeam(this));

            float tolleranceAngle = 70;
            Vector3 hDir = Vector3.ProjectOnPlane(net.transform.position - rb.position, Vector3.up);
            float angle = Vector3.SignedAngle(hDir, transform.forward, Vector3.up);
            Debug.Log($"Shot angle:{angle}");
            Vector3 targetPosition;
            if (Mathf.Abs(angle) < tolleranceAngle)
            {


                // If the angle is positive the player is aiming to the right ( which is the net left side ), otherwise is aiming to the left ( the net right side )
                targetPosition = angle > 0 ? net.GetRandomTarget(left: true) : net.GetRandomTarget(left: false);

                // Add some error depending on the shot timing
                // AddError();
                ShotTiming timing = InputTimingUtility.GetShotTimingByCharge(charge.Value);
                if (timing == ShotTiming.Bad)
                    targetPosition += Vector3.up * UnityEngine.Random.Range(1f, 2f);


            }
            else
            {
                targetPosition = transform.position + transform.forward * 6 + transform.right * UnityEngine.Random.Range(-.5f,.5f) + transform.up * UnityEngine.Random.Range(.5f,1f);
            }

            int aheadTick = 0;
            // We have ball control while shooting and the player stops so the ball position won't change
            Vector3 estimatedBallPos = BallController.Instance.GetEstimatedPosition(aheadTick);

            // Compute estimated speed
            // Depending on the timing and the player power
            float speed = 30;

            // Shoot
            BallController.Instance.ShootAtTick(this, receiver: null, targetPosition, speed, 0, NetworkTimer.Instance.CurrentTick + aheadTick, isPass: false, isOnTheFly: false);

            SetPlayerStateInfo(new PlayerStateInfo() { state = (byte)PlayerState.Shooting });
            // A bit of cooldown
            playerStateCooldown = .1f;
        }

        async Task OnTheFlyJumpAsync(int tick)
        {
            // Get the player timing 
            float timing = GetOnTheFlyTiming(tick);

            float offset = 0f; // NOT_IMPLEMENTED_YET: The part of the body player that hits the ball
            Vector3 currentTargetPosition = BallController.Instance.GetShootingDataTargetPosition();
            float targetHeight = currentTargetPosition.y;
            float hitPoint = targetHeight - offset;

            // How much time it will take the ball to reach the target position
            float delay = BallController.Instance.GetShootingDataRemainingTime();

            // If the it point has a minimum height we must jump
            if (hitPoint > jumpHeightThreshold)
            {

                float jumpTime = Mathf.Sqrt(2f * hitPoint / Mathf.Abs(Physics.gravity.y));

                if (delay > jumpTime)
                {
                    await Task.Delay(TimeSpan.FromSeconds(delay - jumpTime));
                    delay = jumpTime;
                }

                //float time = Vector3.Distance(BallController.Instance.Position, currentTargetPosition) / BallController.Instance.Velocity.magnitude;
                float jumpSpeed = Mathf.Sqrt(2f * hitPoint * Mathf.Abs(Physics.gravity.y)); //(hitPoint / delay) + (.5f * math.abs(Physics.gravity.y) * delay);

                rb.velocity += Vector3.up * jumpSpeed;
                //ShootAsReceiver(jumpSpeed, .5f);

                // Add the fall cooldown
                playerStateCooldown += Mathf.Sqrt(2f * hitPoint / Mathf.Abs(Physics.gravity.y));
            }

            if (delay > 0)
                await Task.Delay(TimeSpan.FromSeconds(delay));
        }

        async void ShootOnGoalOnTheFly(int tick)
        {
            // Disable the handling trigger 
            ballHandlingTrigger.SetEnable(false);

            await OnTheFlyJumpAsync(tick);

            // We must be sure the player is still in its receiving state before we can shoot
            if (playerStateInfo.Value.state != (byte)PlayerState.Receiver)
                return;

            // Get the opponent team net controller
            NetController net = NetController.GetOpponentTeamNetController(TeamController.GetPlayerTeam(this));


            Vector3 targetPosition = UnityEngine.Random.Range(0, 2) > 0 ? net.GetRandomTarget(left: true) : net.GetRandomTarget(left: false);

            // Add some error depending on the shot timing
            // AddError();


            Debug.Log("Targeting the opponent net...");
            int aheadTick = 0;
            // We have ball control while shooting and the player stops so the ball position won't change
            Vector3 estimatedBallPos = BallController.Instance.GetEstimatedPosition(aheadTick);

            // Compute estimated speed
            // Depending on the timing and the player power
            float speed = 30;

            // Shoot
            BallController.Instance.ShootAtTick(this, receiver: null, targetPosition, speed, 0, NetworkTimer.Instance.CurrentTick + aheadTick, isPass: false, isOnTheFly: true);

            SetPlayerStateInfo(new PlayerStateInfo() { state = (byte)PlayerState.Shooting });
            // A bit of cooldown
            playerStateCooldown = .1f;
        }

        /// <summary>
        /// Server only.
        /// Did you shoot with the right timing?
        /// </summary>
        /// <returns></returns>
        int GetOnTheFlyTiming(int tick)
        {
            //int tickDiff = timer.CurrentTick - BallController.Instance.CurrentTick;
            //int ballTick = tick - tickDiff;
            Vector3 targetPosition = BallController.Instance.GetShootingDataTargetPosition();
            Vector3 initialPosition = BallController.Instance.GetShootingDataInitialPosition();

            // Get the ball position at that specific tick
            Vector3 ballPosition;
            if (!BallController.Instance.TryGetServerStatePosition(tick, out ballPosition))
            {
                ballPosition = BallController.Instance.Position;
            }

            byte timing = (byte)InputTimingUtility.GetOnTheFlyTiming(InputTimingUtility.GetOnTheFlyNormalizedTimingValue(initialPosition, targetPosition, ballPosition));

            return timing;
        }

        bool TryGetAvailableReceiver(out PlayerController teammate, Vector2 passageDirection = default)
        {
            float tollaranceAngle = 80;
            Vector3 fwd = rb.transform.forward;
            if (playerStateInfo.Value.state == (byte)PlayerState.Receiver) // Using the forward axis
            {
                fwd = new Vector3(passageDirection.x, 0f, passageDirection.y);
            }

            teammate = null;
            float distance = 0;
            float angle = 0f;
            List<PlayerController> teammates = TeamController.GetPlayerTeam(this).GetPlayers();
            foreach (PlayerController player in teammates)
            {
                if (player == this) continue;
                if (player.playerStateInfo.Value.state != (byte)PlayerState.Normal) continue;

                // Get the vector from the current player to the candidate receiver
                Vector3 distV = player.rb.position - rb.position;
                Vector3 distProj = Vector3.ProjectOnPlane(distV.normalized, Vector3.up);
                float a = Vector3.Angle(distProj, fwd);
                // If a candidate teammate already exists and it's closer then skip
                if (teammate != null && angle < a/* && distance < distV.magnitude*/)
                    continue;
                // Get the angle between the direction and the current player forward
                if (a < tollaranceAngle)
                {
                    // It's a valid target, check the distance
                    if (!teammate || a < angle/*|| distance > distV.magnitude*/)
                    {
                        // Set the current candidate as the target
                        teammate = player;
                        distance = distV.magnitude;
                        angle = a;
                    }
                }

            }

            return teammate;
        }

        public float GetTackleCooldown(byte type)
        {
            float ret = 0f;
            switch (type)
            {
                case (byte)TackleType.Slide:
                    ret = 1.67f;
                    break;
                case (byte)TackleType.Kick:
                    ret = 1.67f;
                    break;
                case (byte)TackleType.Slap:
                    ret = 0.8f;
                    break;
            }

            return ret;

        }



        public float GetStunnedCooldown(byte type, byte detail)
        {
            float ret = 0;
            switch (type)
            {
                case (byte)StunType.BySlide:
                    if (detail == (byte)StunDetail.Front)
                        ret = 2;
                    else
                        ret = 2;
                    break;
                case (byte)StunType.ByKick:
                    if (detail == (byte)StunDetail.Front)
                        ret = 4;
                    else
                        ret = 4;
                    break;
                case (byte)StunType.Electrified:
                    if (detail == (byte)StunDetail.Front)
                        ret = 6;
                    else
                        ret = 6;

                    break;
            }

            return ret;
        }

        #endregion



        #region predicted movement
        void HandleClientTick()
        {
            if (!IsClient) return;

            if (IsOwner && Selected) // If the client is the owner and the player is selected you can read input and send data to the server
            {
                // Get the current tick and buffer index
                int currentTick = NetworkTimer.Instance.CurrentTick;
                var bufferIndex = currentTick % bufferSize;

                // Create the input payload
                InputPayload payload = new InputPayload(input, currentTick);

                // Add the input to the buffer
                clientInputBuffer.Add(payload, bufferIndex);

                // Send the input to the server
                SendToServerRpc(payload);

                if (!IsHost)
                {
                    //
                    // Process movement locally
                    //

                    StatePayload statePayload = ClientProcessMovement(payload);
                    clientStateBuffer.Add(statePayload, bufferIndex);
                    //UnityEngine.Debug.Log(statePayload);
                    HandleReconciliation();

                    //
                    // Check ball handling eventually
                    //
                    CheckForBallHandling();

                }

            }
            else // Not the owner or the selected one ( controlled by another client or the AI or busy in someway, for example stunned )
            {

                // If we are playing singleplayer mode then we are the host and we don't need synchronization at all.
                // If we are playing multiplayer mode then we need to receive synch data from the dedicated server, both for opponents and our teammates.
                if (!IsHost)
                {

                    // Simply get the last state processed by the server and apply it
                    if (!lastServerState.Equals(default))
                    {
                        //Debug.Log($"Last server state position:{lastServerState.position}");
                        Vector3 position = Vector3.Lerp(rb.position, lastServerState.position, NetworkTimer.Instance.DeltaTick * reconciliationSpeed);
                        Quaternion rotation = Quaternion.Lerp(rb.rotation, lastServerState.rotation, NetworkTimer.Instance.DeltaTick * reconciliationSpeed);
                        Vector3 velocity = Vector3.Lerp(rb.velocity, lastServerState.velocity, NetworkTimer.Instance.DeltaTick * reconciliationSpeed);
                        Vector3 angularVelocity = Vector3.Lerp(rb.angularVelocity, lastServerState.angularVelocity, NetworkTimer.Instance.DeltaTick * reconciliationSpeed);
                        WriteTransform(position, rotation, velocity, angularVelocity);
                        lastProcessedState = lastServerState;
                    }

                    //
                    // Check ball handling eventually
                    //
                    CheckForBallHandling();
                }

            }
        }



        void HandleServerTick()
        {
            if (!IsServer) return;

            var bufferIndex = -1;

            while (serverInputQueue.Count > 0)
            {
                // Get the first input payload
                var inputPayload = serverInputQueue.Dequeue();

                // Get the buffer index
                bufferIndex = inputPayload.tick % bufferSize;

                //
                // Simulate movement
                //
                StatePayload state = ServerSimulateMovement(inputPayload);
                //UnityEngine.Debug.Log(state);
                serverStateBuffer.Add(state, bufferIndex);

                //
                // Check buttons
                //
                CheckButton1(inputPayload, false);
                CheckButton2(inputPayload, false);
                CheckButton3(inputPayload, false);
                CheckButton4(inputPayload, false);


                //
                // Check for ball handling eventually
                //
                CheckForBallHandling();

                // 
                // Check stamina
                //
                TryReplenishStamina();

                
            }

            if (bufferIndex == -1) return; // No data
                                           // We send to all clients the last state processed by the server

            SendToClientRpc(serverStateBuffer.Get(bufferIndex));
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
            if (positionError > reconciliationThreshold)
            {
                //Debug.Log($"{name} reconciliation - tick:{lastServerState.tick}, serverPos:{rewindState.position}, clientPos:{clientStateBuffer.Get(bufferIndex).position}");
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

            // Get the last server state data
            Vector3 position = Vector3.Lerp(rb.position, state.position, NetworkTimer.Instance.DeltaTick * reconciliationSpeed);
            Quaternion rotation = Quaternion.Lerp(rb.rotation, state.rotation, NetworkTimer.Instance.DeltaTick * reconciliationSpeed);
            Vector3 velocity = Vector3.Lerp(rb.velocity, state.velocity, NetworkTimer.Instance.DeltaTick * reconciliationSpeed);
            Vector3 angularVelocity = Vector3.Lerp(rb.angularVelocity, state.angularVelocity, NetworkTimer.Instance.DeltaTick * reconciliationSpeed);

            WriteTransform(position, rotation, velocity, angularVelocity);

            // Add to the client buffer
            clientStateBuffer.Add(state, state.tick);

            // Replay all the input from the rewind state to the current state
            int tickToReplay = state.tick;
            while (tickToReplay < NetworkTimer.Instance.CurrentTick)
            {
                int bufferIndex = tickToReplay % bufferSize;
                InputPayload payload = clientInputBuffer.Get(bufferIndex);
                StatePayload clientState = ClientProcessMovement(payload);
                clientStateBuffer.Add(clientState, bufferIndex);
                tickToReplay++;
            }
        }

        /// <summary>
        /// Send this controller data to the server
        /// </summary>
        /// <param name="input"></param>
        [ServerRpc]
        void SendToServerRpc(InputPayload input)
        {
            // We don't need to send data to the server if we are the host
            //if (IsHost) return;
            // Put at the end of the queue
            serverInputQueue.Enqueue(input);
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
        StatePayload ServerSimulateMovement(InputPayload inputPayload)
        {
            //Physics.simulationMode = SimulationMode.Script;
            Move(new InputData(inputPayload));
            //Physics.Simulate(timer.DeltaTick);
            //Physics.simulationMode = SimulationMode.FixedUpdate;

            StatePayload state = ReadTransform();
            state.tick = inputPayload.tick;
            return state;
        }


        /// <summary>
        /// Called on the client to apply movement to the local selected player
        /// </summary>
        /// <param name="inputMove"></param>
        /// <param name="tick"></param>
        /// <returns></returns>
        //StatePayload ClientProcessMovement(/*Vector2 inputMove, */int tick)
        StatePayload ClientProcessMovement(InputPayload inputPayload)
        {
            // Move player and return the state payload
            //Physics.simulationMode = SimulationMode.Script;
            Move(new InputData(inputPayload));
            //Physics.Simulate(timer.DeltaTick);
            //Physics.simulationMode = SimulationMode.FixedUpdate;
            StatePayload state = ReadTransform();
            state.tick = inputPayload.tick;
            return state;

        }


        private void Move(InputData inputData)
        {
            switch (playerStateInfo.Value.state)
            {
                case (byte)PlayerState.Normal:
                    UpdateNormalMovement(inputData);
                    break;
                case (byte)PlayerState.Stunned:
                    UpdateStunnedMovement();
                    break;
                case (byte)PlayerState.Tackling:
                    UpdateTackleMovement();
                    break;
                case (byte)PlayerState.Receiver:
                    UpdateReceiverMovement();
                    break;
                // case (byte)PlayerState.BlowingUp:
                //     UpdateBlowingUpMovement();
                //     break;
                case (byte)PlayerState.Diving:
                    UpdateDiveMovement();
                    break;

            }


        }


        void UpdateReceiverMovement()
        {
            if (!IsServer)
                return;

            if (TeamController.GetOpponentTeam(this).GetPlayers().Exists(p => p.HasBall))
            {
                SetPlayerStateInfo(new PlayerStateInfo() { state = (byte)PlayerState.Normal });
                return;
            }

            PlayerController receiver = BallController.Instance.GetShootingDataReceiver();
            if (!receiver || receiver != this)
                return;



            // Read the target
            Vector3 targetPosition = BallController.Instance.GetShootingDataTargetPosition();

            // Player should move towards the target position

            Vector3 dirH = Vector3.ProjectOnPlane(targetPosition - rb.transform.position, Vector3.up);
            float minDist = .5f;
            if (dirH.magnitude > minDist)
            {
                currentSpeed += acceleration * NetworkTimer.Instance.DeltaTick;
                if (currentSpeed > maxSpeed) currentSpeed = maxSpeed;
            }
            else
            {
                currentSpeed -= acceleration * NetworkTimer.Instance.DeltaTick;
                if (currentSpeed < 0) currentSpeed = 0;
            }
            //Debug.Log($"Setting receiver velocity:{dirH.normalized * currentSpeed}");
            float vSpeed = rb.velocity.y;
            rb.velocity = dirH.normalized * currentSpeed + Vector3.up * vSpeed;

            //// Eventually jump
            //float offset = .75f; // This may change depending on the shoot animation
            //float targetHeight = targetPosition.y - offset;
            //if(rb.position.y < targetHeight)
            //{
            //    //rb.AddForce(Vector3.up * 15, ForceMode.VelocityChange);
            //    float time = Vector3.Distance(BallController.Instance.Position, targetPosition) / rb.velocity.magnitude;
            //    float jumpSpeed = (targetHeight - rb.position.y) / time;
            //    Debug.Log($"JumpSpeed:{jumpSpeed}");
            //    rb.velocity += Vector3.up * 4f;
            //}


        }


        void UpdateNormalMovement(InputData inputData)
        {
            Vector2 moveInput = inputData.joystick;
            // Normalize input 
            moveInput.Normalize();

            Vector3 targetVel; // Horizontal target velocity
            float acc = 0;
            if (moveInput.magnitude > 0)
            {
                Vector3 lDir = new Vector3(moveInput.x, 0f, moveInput.y);
                if (lookDirection != Vector3.zero)
                    lDir = lookDirection;

                transform.forward = Vector3.MoveTowards(transform.forward, lDir, rotationSpeed * NetworkTimer.Instance.DeltaTick);

                targetVel = new Vector3(moveInput.x, 0f, moveInput.y) * maxSpeed * (sprinting.Value ? sprintMultiplier : 1f);
                acc = acceleration;
            }
            else
            {
                targetVel = Vector3.zero;
                acc = deceleration;
            }

            Vector3 mDir = transform.forward;
            if (lookDirection != Vector3.zero)
                mDir = new Vector3(moveInput.x, 0f, moveInput.y);

            Vector3 vel = Vector3.MoveTowards(Vector3.ProjectOnPlane(rb.velocity, Vector3.up), targetVel, acc * NetworkTimer.Instance.DeltaTick);
            currentSpeed = vel.magnitude;

            rb.velocity = vel + rb.velocity.y * Vector3.up;

        }

        // void UpdateNormalMovement(InputData inputData)
        // {
        //     Vector2 moveInput = inputData.joystick;
        //     // Normalize input 
        //     moveInput.Normalize();
        //     float speed = currentSpeed;

        //     if (moveInput.magnitude > 0)
        //     {
        //         Vector3 lDir = new Vector3(moveInput.x, 0f, moveInput.y);
        //         if (lookDirection != Vector3.zero)
        //             lDir = lookDirection;

        //         transform.forward = Vector3.MoveTowards(transform.forward, lDir, rotationSpeed * NetworkTimer.Instance.DeltaTick);
        //         //speed += acceleration * Time.fixedDeltaTime; // We are assuming that the ServerTickRate is equal to the PhysicalTickRate
        //         speed += acceleration * NetworkTimer.Instance.DeltaTick; // We are assuming that the ServerTickRate is equal to the PhysicalTickRate
        //         if (speed > maxSpeed) speed = maxSpeed;
        //     }
        //     else
        //     {
        //         //speed -= deceleration * Time.fixedDeltaTime;
        //         speed -= deceleration * NetworkTimer.Instance.DeltaTick;
        //         if (speed < 0) speed = 0;
        //     }
        //     currentSpeed = speed;

        //     Vector3 mDir = transform.forward;
        //     if (lookDirection != Vector3.zero)
        //         mDir = new Vector3(moveInput.x, 0f, moveInput.y);

        //     Vector3 currVelDir = rb.velocity.normalized;
        //     if (currentSpeed > 0)
        //     {

        //         currVelDir = Vector3.MoveTowards(currVelDir, mDir, 5f * NetworkTimer.Instance.DeltaTick);
        //         Debug.Log("CURRVELDIR:" + currVelDir);
        //         Debug.Log("MDIR:" + mDir);
        //     }    

        //     rb.velocity = currVelDir * currentSpeed + rb.velocity.y * Vector3.up;
        //     //rb.velocity = mDir * currentSpeed + rb.velocity.y * Vector3.up;
        // }

        void UpdateDiveMovement()
        {
            if (!IsServer) return;

            diveUpdateFunction.Invoke();

        }

        void UpdateBlowingUpMovement()
        {
            if (!IsServer) return;

            // Avoid checking player is grounded too soon
            if (customTickTarget > NetworkTimer.Instance.CurrentTick)
                return;

            customTickTarget = 0;

            // Check if the player is grounded
            if (Physics.Raycast(Position + Vector3.up * 0.01f, Vector3.down, 0.01f, LayerMask.GetMask(new string[] { "Floor" })))// Is grounded
            {
                // Check the anim detail param
                int detail = animator.GetInteger(detailAnimParam);

                if (detail == 0) // Front
                {
                    animator.SetTrigger(exitAnimParam);
                    SetPlayerStateInfo((byte)PlayerState.Busy, 0, 0, 2.2f);
                }
                else // Back
                {
                    animator.SetTrigger(exitAnimParam);
                    SetPlayerStateInfo((byte)PlayerState.Busy, 0, 0, 2.2f);
                }

                
                // Set stunned state
                //SetPlayerStateInfo(PlayerState.Stunned)
            }
        }

        void UpdateStunnedMovement()
        {

            if (!IsServer) return;

            switch (playerStateInfo.Value.subState)
            {
                case (byte)StunType.ByKick:
                case (byte)StunType.BySlap:
                    //case (byte)StunType.ByKickBack:
                    Vector3 dir = transform.forward;
                    if (playerStateInfo.Value.detail == (byte)StunDetail.Front)
                        dir *= -1;
                    float time = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
                    float maxSpeed = 5f;
                    float acc = 30;
                    if (time < .8f)
                    {
                        //currentSpeed += 10f * Time.fixedDeltaTime;
                        currentSpeed += acc * NetworkTimer.Instance.DeltaTick;
                        if (currentSpeed > maxSpeed) currentSpeed = maxSpeed;
                    }
                    else
                    {
                        //currentSpeed -= 10f * Time.fixedDeltaTime;
                        currentSpeed -= acc * NetworkTimer.Instance.DeltaTick;
                        if (currentSpeed < 0)
                            currentSpeed = 0;
                    }


                    rb.velocity = dir * currentSpeed;
                    break;

                case (byte)StunType.Electrified:
                    currentSpeed = 0;
                    rb.velocity = Vector3.zero;
                    break;
                case (byte)StunType.BlowingUp:
 // Avoid checking player is grounded too soon
                    if (customTickTarget == 0 || customTickTarget > NetworkTimer.Instance.CurrentTick)
                        break;


                    Debug.Log($"TEST - Delay elapsed, current:{NetworkTimer.Instance.CurrentTick}, delay:{customTickTarget}");
                    
                    // Check if the player is grounded
                    if (Physics.Raycast(Position + Vector3.up * 0.01f, Vector3.down, 0.01f, LayerMask.GetMask(new string[] { "Floor" })))// Is grounded
                    {
                        customTickTarget = 0;
                        // Check the anim detail param
                        int detail = playerStateInfo.Value.detail;

                        if (detail == (byte)StunDetail.Front) // Front
                        {
                            animator.SetTrigger(exitAnimParam);
                            //SetPlayerStateInfo((byte)PlayerState.Busy, 0, 0, 2.2f);
                            playerStateCooldown = 2.2f;
                            
                        }
                        else // Back
                        {
                            animator.SetTrigger(exitAnimParam);
                            //SetPlayerStateInfo((byte)PlayerState.Busy, 0, 0, 2.2f);
                            playerStateCooldown = 2.2f;
                            
                        }

                        
                        // Set stunned state
                        //SetPlayerStateInfo(PlayerState.Stunned)
                    }
                    break;
            }
        }

        void UpdateTackleMovement()
        {
            switch (playerStateInfo.Value.subState)
            {
                case (byte)TackleType.Slide:
                case (byte)TackleType.Kick:
                    Vector3 dir = transform.forward;
                    float time = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
                    float acc = 10;
                    if (playerStateInfo.Value.subState == (byte)TackleType.Kick)
                        acc = 15;
                    maxSpeed = 3;
                    if (time < .8f)
                    {
                        //currentSpeed += 10f * Time.fixedDeltaTime;
                        currentSpeed += acc * NetworkTimer.Instance.DeltaTick;
                        if (currentSpeed > maxSpeed)
                            currentSpeed = maxSpeed;
                    }
                    else
                    {
                        //currentSpeed -= 10f * Time.fixedDeltaTime;
                        currentSpeed -= acc * NetworkTimer.Instance.DeltaTick;
                        if (currentSpeed < 0)
                            currentSpeed = 0;
                    }


                    rb.velocity = dir * currentSpeed;
                    break;

                case (byte)TackleType.Slap:

                    Debug.Log("GK - slapping...");
                    dir = transform.forward;
                    time = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
                    acc = 10;
                    maxSpeed = 3;
                    if (time < .5f)
                    {
                        currentSpeed += acc * NetworkTimer.Instance.DeltaTick;
                        if (currentSpeed > maxSpeed)
                            currentSpeed = maxSpeed;

                        float range = 2f;
                        // Check for all the opponent players within the slap range
                        List<PlayerController> others = new List<PlayerController>(TeamController.GetOpponentTeam(this).GetPlayers().Where(p => Vector3.Distance(p.Position, Position) < range && p.playerStateInfo.Value.state != (byte)PlayerState.Stunned));
                        Debug.Log($"GK - others:{others.Count}");
                        // Stun others
                        foreach (PlayerController other in others)
                        {
                            Debug.Log($"GK - slapping player:{other.name}");

                            Vector3 hitDir = other.Position - Position;
                            byte detail = (byte)StunDetail.Front;
                            if (Vector3.Dot(hitDir, other.transform.forward) > 0)
                                detail = (byte)StunDetail.Back;

                            PlayerStateInfo psi = new PlayerStateInfo() { state = (byte)PlayerState.Stunned, /*It's like by kick for now*/ subState = (byte)StunType.ByKick, detail = detail };
                            //other.playerStateInfo.Value = psi;
                            other.SetPlayerStateInfo(psi);
                        }
                    }
                    else
                    {
                        currentSpeed -= acc * NetworkTimer.Instance.DeltaTick;
                        if (currentSpeed < 0)
                            currentSpeed = 0;
                    }
                    break;

            }


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
        void HandleOnPlayerStateInfoChanged(PlayerStateInfo oldState, PlayerStateInfo newState)
        {
            if (oldState == newState) return;

            // if (oldState.state == (byte)PlayerState.Receiver && newState.state != (byte)PlayerState.Shooting && !HasBall)
            // {
            //     // For example, when an opponent intercepts the ball during a pass. In this case is better select the closest player
            //     TeamController.GetPlayerTeam(this).SelectClosestPlayerToBall(goalkeeperAllowed: false);

            // }

            switch (newState.state)
            {
                case (byte)PlayerState.Normal:
                    if (Role != PlayerRole.GK || oldState.state == (byte)PlayerState.Receiver || oldState.state == (byte)PlayerState.Shooting)
                        ballHandlingTrigger.SetEnable(true);
                    break;

                case (byte)PlayerState.Tackling:
                    if (IsServer)
                    {

                        // Get the action cooldown
                        playerStateCooldown = GetTackleCooldown(newState.subState);

                        // Start animation on server
                        animator.SetInteger(typeAnimParam, newState.subState);
                        animator.SetTrigger(tackleAnimTrigger);

                    }

                    break;

                case (byte)PlayerState.Stunned:
                    if (IsServer)
                    {
                        // Get the action cooldown
                        playerStateCooldown = GetStunnedCooldown(newState.subState, newState.detail);

                        // Disable the handling trigger
                        ballHandlingTrigger.SetEnable(false);

                        // Start animation on server
                        // The action type depends on the opponent distance, for now we just test a basic tackle
                        animator.SetInteger(detailAnimParam, newState.detail);
                        animator.SetInteger(typeAnimParam, newState.subState);
                        animator.SetTrigger(stunAnimTrigger);

                        // Select another player
                        TeamController.GetPlayerTeam(this).SelectClosestPlayerToBall(goalkeeperAllowed: false);

                    }

                    break;
                case (byte)PlayerState.Receiver:
                    //playerStateCooldown = 2f; // NOT_IMPLEMENTED_YET: we must compute the cooldown depending on the animation and eventually the jump
                    playerStateCooldown = BallController.Instance.GetShootingDataRemainingTime() + .5f;
                    //ballHandlingTrigger.SetEnable(false);
                    break;

                case (byte)PlayerState.Diving:
                    animator.SetInteger(detailAnimParam, newState.detail);
                    animator.SetInteger(typeAnimParam, newState.subState);
                    animator.SetBool(loopAnimParam, true);
                    animator.SetTrigger(diveAnimTrigger);
                    break;
                    // case (byte)PlayerState.BlowingUp:
                    //     // Disable ball handling trigger
                    //     ballHandlingTrigger.SetEnable(false);

                    //     // Set animation
                    //     break;
            }
        }
        #endregion

        #region misc
        /// <summary>
        /// To replace with an input handler ( in order to support AI )
        /// </summary>
        void CheckHumanInput()
        {
            if (inputHandler == null || !IsOwner || !Selected)
                return;

            //if (Selected)
            input = inputHandler.GetInput();
            //else
            //    input = new InputData() { joystick = Vector2.zero, button1 = false, button2 = false, button3 = false };
            //Debug.Log($"Client input:{input}");
        }

        /// <summary>
        /// Get input from ai and fill the server input queue ( ai always runs on server )
        /// </summary>
        void CheckNotHumanInput()
        {
            InputData input = inputHandler.GetInput();
            serverInputQueue.Enqueue(new InputPayload() { inputVector = input.joystick, button1 = input.button1, button2 = input.button2, button3 = input.button3, button4 = input.button4, tick = NetworkTimer.Instance.CurrentTick });
        }

        /// <summary>
        /// Called by the ball handling trigger on enter.
        /// </summary>
        private void HandleOnBallEnter()
        {
            if (Role == PlayerRole.GK && GetState() == (byte)PlayerState.Diving)// && goalkeeperAI.IsBouncingTheBallBack)
            {
                //TODO: use evaluation function here
                bool save = goalkeeperAI.TrySave();
                if (save)
                {
                    if (!goalkeeperAI.IsBouncingTheBallBack)
                        BallController.Instance.BallEnterTheHandlingTrigger(this);
                    else
                        goalkeeperAI.BounceTheBallBack();
                }
                    

            }
            else
            {
                if (playerStateInfo.Value.state == (byte)PlayerState.Receiver)
                    SetPlayerStateInfo(new PlayerStateInfo() { state = (byte)PlayerState.Normal });

                BallController.Instance.BallEnterTheHandlingTrigger(this);
            }

        }

        /// <summary>
        /// Called by the handling trigger on exit
        /// </summary>
        private void HandleOnBallExit()
        {

            // It's the ball
            BallController.Instance.BallExitTheHandlingTrigger(this);
        }

        /// <summary>
        /// Called both on client and server
        /// </summary>
        public void StartHandlingTheBall()
        {
            // The player is the goalkeeper and they are not blocking the ball 
            //if (Role == PlayerRole.GK && playerStateInfo.Value.state == (byte)PlayerState.Diving && goalkeeperAI.IsBouncingTheBallBack)
            //    return;

            if (handlingTheBall)
                return;
            handlingTheBall = true;

            if (Role == PlayerRole.GK && playerStateInfo.Value.state == (byte)PlayerState.Diving)
            {
                useGoalkeeperBallHook.Value = true;
                ballHook = goalkeeperAI.GetBallHook();
            }
            else
            {
                useGoalkeeperBallHook.Value = false;
                ballHook = ballHookDefault;
            }
                

            // Set the player who owns the ball as the selected one
            TeamController.GetPlayerTeam(this).SetPlayerSelected(this);
        }

        /// <summary>
        /// Called both on client and server
        /// </summary>
        public void StopHandlingTheBall()
        {
            //if(!handlingTheBall)
            //    return;
            handlingTheBall = false;

            ballHook = ballHookDefault;
        }



        public void GiveSlap()
        {
            if (playerStateInfo.Value.state != (byte)PlayerState.Normal) return;

            SetPlayerStateInfo(new PlayerStateInfo() { state = (byte)PlayerState.Tackling, subState = (byte)TackleType.Slap, detail = 0 });
        }



        void CheckForBallHandling()
        {
            if (!handlingTheBall)
                return;

            // Set the ball position depending on the hook
            BallController ball = BallController.Instance;
            ball.Position = Vector3.MoveTowards(ball.Position, ballHook.position, ballHookLerpSpeed * Time.fixedDeltaTime);

        }


        void UpdateActionCooldown()
        {
            if (!IsServer)
                return;

            if (playerStateCooldown > 0)
            {
                playerStateCooldown -= Time.deltaTime;
                if (playerStateCooldown < 0)
                {

                    SetPlayerStateInfo(new PlayerStateInfo() { state = (byte)PlayerState.Normal });
                }

            }

        }

        void SetPlayerStateInfo(PlayerStateInfo playerStateInfo)
        {
            if (!IsServer) return;
            this.playerStateInfo.Value = playerStateInfo;
        }

        public void SetAsReceiver()
        {
            this.playerStateInfo.Value = new PlayerStateInfo() { state = (byte)PlayerState.Receiver };
        }

        public void SetLookDirection(Vector3 lookDirection)
        {
            lookDirection.y = 0f;
            this.lookDirection = lookDirection.normalized;

        }

        public void ResetLookDirection()
        {
            lookDirection = Vector3.zero;
        }

        public bool IsSelected()
        {
            return TeamController.GetPlayerTeam(this).SelectedPlayer == this;
        }

        public bool IsTeammate(PlayerController otherPlayer)
        {
            return TeamController.GetPlayerTeam(this) == TeamController.GetPlayerTeam(otherPlayer);
        }



        public void ResetToKickOff(Transform t)
        {
            rb.isKinematic = true;
            Velocity = Vector3.zero;
            Position = t.position;
            Rotation = t.rotation;
            rb.isKinematic = false;
        }

        public int GetState()
        {
            return playerStateInfo.Value.state;
        }

        public void SetPlayerStateInfo(byte state, byte subState, byte detail, float cooldown)
        {
            SetPlayerStateInfo(new PlayerStateInfo() { state = (byte)state, subState = subState, detail = detail });
            playerStateCooldown = cooldown;
        }

        public void SetRole(int role)
        {
            this.role = role;
        }

        public bool IsReceivingPassage()
        {
            return playerStateInfo.Value.state == (byte)PlayerState.Receiver;
        }

        //public void DisableBallHandlingTrigger()
        //{
        //    ballHandlingTrigger.SetEnable(false);
        //}

        //public void EnableBallHandlingTrigger()
        //{
        //    ballHandlingTrigger.SetEnable(true);
        //}

        //[ClientRpc]
        //public void SetBallHandlingTriggerEnableClientRpc(bool value)
        //{
        //    if (IsServer)
        //        return;

        //    ballHandlingTrigger.SetEnable(value);
        //}

        void IncreaseCharge(float time)
        {
            float ch = charge.Value;
            ch += time * chargingSpeed;
            ch = Mathf.Clamp01(ch);
            charge.Value = ch;
        }

        void ResetCharge()
        {
            charge.Value = 0;
        }

        void DecreaseStamina(float amount)
        {
            if (amount <= 0 || stamina.Value == 0) return;
            var s = stamina.Value;
            s -= amount;
            if (s < 0)
                s = 0;
            stamina.Value = s;
            lastStaminaDecrease = DateTime.Now;
        }

        void IncreaseStamina(float amount)
        {
            if (amount <= 0 || stamina.Value == staminaMax) return;
            var s = stamina.Value;
            s += amount;
            if (s > staminaMax)
                s = staminaMax;
            stamina.Value = s;
        }

        void TryReplenishStamina()
        {
            if (stamina.Value == staminaMax) return;

            if ((DateTime.Now - lastStaminaDecrease).TotalSeconds < staminaReplenishDelay) return;

            var s = stamina.Value;

            s += replenishStaminaRate * NetworkTimer.Instance.DeltaTick;

            if (s > staminaMax)
                s = staminaMax;

            stamina.Value = s;

        }

        /// <summary>
        /// Called on this player when they are tackling.
        /// </summary>
        /// <param name="other"></param>
        private void OnTriggerEnter(Collider other)
        {
            // Each player has a trigger in order to better handle tackles
            if (other.CompareTag(Tags.TackleTrigger))
            {
                if (!IsServer || other.gameObject == gameObject || playerStateInfo.Value.state != (byte)PlayerState.Tackling) return;

                // Tackle trigger only belongs to players, so we have a player controller for sure.
                PlayerController otherPC = other.GetComponentInParent<PlayerController>();

                if (otherPC.playerStateInfo.Value.state == (byte)PlayerState.Normal)
                {
                    var ps = otherPC.playerStateInfo.Value;
                    // Stun the other player
                    switch (playerStateInfo.Value.subState)
                    {
                        case (byte)TackleType.Slide:
                            // Check whether the opponent is facing the player or not.
                            // Player slides in the direction they are facing, so we just need the dot between the player and the opponent facing directions
                            float dot = Vector3.Dot(transform.forward, other.transform.forward);
                            ps.subState = (byte)StunType.BySlide;
                            if (dot < 0) // Facing
                                ps.detail = (byte)StunDetail.Front;
                            else // Not facing
                                ps.detail = (byte)StunDetail.Back;

                            ps.state = (byte)PlayerState.Stunned;

                            break;

                        case (byte)TackleType.Kick:
                            dot = Vector3.Dot(transform.forward, other.transform.forward);
                            //var ps = otherPC.playerState.Value;
                            ps.subState = (byte)StunType.ByKick;
                            if (dot < 0) // Facing
                                ps.detail = (byte)StunDetail.Front;
                            else // Not facing
                                ps.detail = (byte)StunDetail.Back;

                            ps.state = (byte)PlayerState.Stunned;

                            break;
                    }

                    otherPC.playerStateInfo.Value = ps;
                }
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            // Barrier collision
            if (collision.gameObject.CompareTag(Tags.Barrier))
            {
                if (IsServer)
                {
                    // Electrify player

                    // Get facing direction
                    float dot = Vector3.Dot(transform.forward, collision.contacts[0].normal);
                    var ps = playerStateInfo.Value;
                    ps.subState = (byte)StunType.Electrified;
                    if (dot < 0) // Front
                    {
                        Debug.Log($"Electrify front, player:{name}");

                        ps.detail = (byte)StunDetail.Front;

                    }
                    else // Back
                    {
                        Debug.Log($"Electrify back, player:{name}");
                        ps.detail = (byte)StunDetail.Back;
                    }

                    ps.state = (byte)PlayerState.Stunned;
                    playerStateInfo.Value = ps;
                }

            }
        }

        #endregion

        #region initialization

        /// <summary>
        /// Called by the server when a new player controller is spawned
        /// </summary>
        /// <param name="owner"></param>
        public void Init(PlayerInfo owner, int index)
        {
            //this.playerInfo.Value = owner;
            this.playerInfoId.Value = owner.Id;
            this.index.Value = (byte)index;
        }

        public void SetInputHandler(IInputHandler inputHandler)
        {
            this.inputHandler = inputHandler;
        }

        public IInputHandler GetInputHandler()
        {
            return inputHandler;
        }

        public void SetDiveUpdateFunction(UnityAction function)
        {
            diveUpdateFunction = function;
        }

        public void SetBlowUpState(bool front)
        {
            SetPlayerStateInfo((byte)PlayerState.Stunned, (byte)StunType.BlowingUp, front ? (byte)StunDetail.Front : (byte)StunDetail.Back, 0);

            // int detail = front ? 0 : 1;
            // animator.SetInteger(detailAnimParam, detail);

            // animator.SetTrigger(blowUpAnimParam);



            // Set the custom tick delay to avoid the is ground check for the first N ticks
            customTickTarget = NetworkTimer.Instance.CurrentTick + 15;

            Debug.Log($"TEST - Setting delay, curretn:{NetworkTimer.Instance.CurrentTick}, delay:{customTickTarget}");
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
