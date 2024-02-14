using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Matchmaker.Models;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using WLL_NGO.Multiplay;
using WLL_NGO.Services;

namespace WLL_NGO.Netcode
{
    public class NetworkLauncher : SingletonPersistent<NetworkLauncher>
    {
        public static UnityAction<ulong> OnClientConnected;
        public static UnityAction<ulong> OnClientDisconnected;
        public static UnityAction OnClientStopped;

        bool dedicatedServer = false;
        string ip = "0.0.0.0";
        ushort port;

        private void Start()
        {
            dedicatedServer = Utility.IsDedicatedServer();

            if (!dedicatedServer) // For client
            {
#if NO_MM
                // Local test with no matchmaking
                port = Constants.NoMatchmakingTestingPort;
                ip = "127.0.0.1";

#else
                ClientMatchmaker.OnTicketAssigned += HandleOnTicketAssigned;
#endif
            }


            if (dedicatedServer)
            {
                ServerMatchmaker.OnMatchmakerPayload += HandleOnMatchmakerPayload; // Wait for payload to set the connection ip and port
            }
        }


        private void RegisterCallbacks()
        {
            if (dedicatedServer)
            {
                NetworkManager.Singleton.OnServerStarted += HandleOnServerStarted;
                NetworkManager.Singleton.OnServerStopped += HandleOnServerStopped;
            }
            else
            {
                ClientMatchmaker.OnTicketAssigned += HandleOnTicketAssigned;
                ClientMatchmaker.OnTicketFailed += HandleOnTicketFailed;
                NetworkManager.Singleton.OnClientStarted += HandleOnClientStarted;
                NetworkManager.Singleton.OnClientStopped += HandleOnClientStopped;
            }

            NetworkManager.Singleton.OnClientConnectedCallback += HandleOnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleOnClientDisconnected;
        }

        private void UnregisterCallbacks()
        {
            if (dedicatedServer)
            {
                NetworkManager.Singleton.OnServerStarted -= HandleOnServerStarted;
                NetworkManager.Singleton.OnServerStopped -= HandleOnServerStopped;
            }
            else
            {
                ClientMatchmaker.OnTicketAssigned -= HandleOnTicketAssigned;
                ClientMatchmaker.OnTicketFailed -= HandleOnTicketFailed;
                NetworkManager.Singleton.OnClientStarted -= HandleOnClientStarted;
                NetworkManager.Singleton.OnClientStopped -= HandleOnClientStopped;
            }

            NetworkManager.Singleton.OnClientConnectedCallback -= HandleOnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleOnClientDisconnected;
        }

#region server only
        /// <summary>
        /// Called on server
        /// </summary>
        /// <param name="payload"></param>
        void HandleOnMatchmakerPayload(MatchmakingResults payload)
        {
            SetIpAndPort("0.0.0.0", Utility.GetPortFromCommandLineArgs());
            StartServer();
        }

        void HandleOnServerStarted()
        {
            Debug.Log($"Server listening on port:{NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Port}");
        }

        void HandleOnServerStopped(bool isHost)
        {
            Debug.Log($"Server has stopped");
            UnregisterCallbacks();
        }

        public void StartServer()
        {
#if USE_LOBBY_SCENE
            SceneManager.LoadSceneAsync(Constants.LobbyScene, LoadSceneMode.Single).completed += (op) =>
#else
            SceneManager.LoadSceneAsync(Constants.DefaultGameScene, LoadSceneMode.Single).completed += (op) =>
#endif
            {
                if (op.isDone)
                {
                    RegisterCallbacks();
                    Debug.Log($"Starting server on port {port}");
                    NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(ip, port);
                    NetworkManager.Singleton.StartServer();
                }

            };
        }

#endregion

#region client only
        void HandleOnTicketAssigned(MultiplayAssignment assignment)
        {
            port = (ushort)assignment.Port;
            ip = assignment.Ip;
            StartClient();
        }

        void HandleOnTicketFailed()
        {
            StartHost();
        }

        void HandleOnClientStarted()
        {
            Debug.Log($"Client started, connecting to {NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Address}:{NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Port}");
        }

        void HandleOnClientStopped(bool hostMode)
        {
            UnregisterCallbacks();
            OnClientStopped?.Invoke();
        }

        void Shutdown()
        {
            NetworkManager.Singleton.Shutdown();
            UnregisterCallbacks();
            SceneManager.LoadScene(Constants.ClientMainScene, LoadSceneMode.Single);
        }

        public void StartClient()
        {
            RegisterCallbacks();
            Debug.Log($"ip:{ip}");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(ip, port);
            NetworkManager.Singleton.StartClient();
        }

        public void StartHost()
        {
            // Load game scene
            SceneManager.LoadSceneAsync(Constants.DefaultGameScene, LoadSceneMode.Single).completed += (op) =>
            {
                if (op.isDone)
                {
                    RegisterCallbacks();
                    NetworkManager.Singleton.StartHost();
                }

            };

            
        }
#endregion

#region both
        void HandleOnClientConnected(ulong clientId)
        {
            //if (dedicatedServer || NetworkManager.Singleton.IsHost)
            if (NetworkManager.Singleton.IsServer)
            {
                Debug.Log($"Client {clientId} connected.");
                OnClientConnected?.Invoke(clientId);
            }
            
        }

        void HandleOnClientDisconnected(ulong clientId)
        {
            //if (dedicatedServer)
            if(NetworkManager.Singleton.IsServer)
            {
                Debug.Log($"Client {clientId} disconnected");
                OnClientDisconnected?.Invoke(clientId);
            }
            else
            {
                Debug.Log($"Client {clientId} disconnected");
                Shutdown();
            }
        }

        public void SetIpAndPort(string ip, ushort port)
        {
            this.ip = ip;
            this.port = port;
        }

#endregion




    }

}
