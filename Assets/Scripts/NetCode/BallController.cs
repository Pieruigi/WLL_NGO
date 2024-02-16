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
    /// The ball uses the same logic of the client controller but with a different implementation.
    /// Basically the client reads the input and sends it to the server with the corresponding tick; the server then processes the input and, for example, tells the 
    /// client to shoot, but since we have to play some animation bofore we can physically shoot, the server can tell all the clients that the player is shooting 
    /// at a given tick; doing so all the clients and the server would be able to shoot at the same time ( if you have 1 second of lag this is not going to work 
    /// for you of course, but in that case nothing is going to work ).
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

        /// <summary>
        /// Stored value is the PlayerController.NetworkObjectId
        /// </summary>
        NetworkVariable<ulong> ownerNetObjId = new NetworkVariable<ulong>(0);

        PlayerController owner = null;

        Rigidbody rb;

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

        protected override void Awake()
        {
            base.Awake();
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

                if(ownerNetObjId.Value == 0)
                {
                    PlayerController pc = FindObjectsOfType<PlayerController>().Where(p=>p.OwnerClientId == NetworkManager.Singleton.LocalClientId).First();
                    Debug.Log($"Ball - pc:{pc.PlayerInfo}");
                    ownerNetObjId.Value = pc.NetworkObjectId;
                }
                else
                {

                    ownerNetObjId.Value = 0;
                }
            }
#endif
        }

        private void FixedUpdate()
        {
            if (timer.TimeToTick())
            {
                HandleClientTick();
                HandleServerTick();
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            // Owner change event handler
            ownerNetObjId.OnValueChanged += HandleOnOwnerChanged;
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            // Owner change event handler
            ownerNetObjId.OnValueChanged -= HandleOnOwnerChanged;
        }
        
        void HandleOnOwnerChanged(ulong oldValue, ulong newValue)
        {
            Debug.Log($"Owner changed:{oldValue}->{newValue}");
            owner = newValue > 0 ? PlayerControllerManager.Instance.GetPlayerCotrollerByNetworkObjectId(newValue) : null;
        }

        #region prediction and reconciliation
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

        [ClientRpc]
        private void ShootAtTickClientRpc(ulong playerNetObjId, Vector3 force, int tick)
        {
            ShootAtTick(PlayerControllerManager.Instance.GetPlayerCotrollerByNetworkObjectId(playerNetObjId), force, tick);
        }


        public async void ShootAtTick(PlayerController player, Vector3 velocity, int tick)
        {
            Debug.Log($"Shoot at tick {tick}");

            // You can not shoot the ball it's controlled by another player
            if (owner != null && owner != player) return;
            
            if(IsServer && !IsHost)
                ShootAtTickClientRpc(player.NetworkObjectId, velocity, tick);
            
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
    }

}
