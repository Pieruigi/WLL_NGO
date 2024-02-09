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

#if NO_MM
        protected override void Awake()
        {
            base.Awake();
            if (Instance == this)
            {
                dedicatedServer = Utility.IsDedicatedServer();
                if (dedicatedServer)
                {

                    SceneManager.sceneLoaded += (s, m) =>
                    {
                        if (Constants.ServerMainScene.Equals(s.name))
                            StartServer();
                    };

                }


            }

        }
#endif

        private void Start()
        {
            dedicatedServer = Utility.IsDedicatedServer();

#if NO_MM
            if (!dedicatedServer)
            {
                port = Constants.NoMatchmakingTestingPort;
                ip = "127.0.0.1";
            }
#else
            if (!dedicatedServer)
            {
             ServerManager.OnMatchmakerPayload += HandleOnMatchmakerPayload;
            }
#endif

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
                ClientManager.OnTicketAssigned += HandleOnTicketAssigned;
                ClientManager.OnTicketFailed += HandleOnTicketFailed;
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
                ClientManager.OnTicketAssigned -= HandleOnTicketAssigned;
                ClientManager.OnTicketFailed -= HandleOnTicketFailed;
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

        async void StartServer()
        {
            // Prefetch data ( for example the full catalog )
            // We load all needed data before we enter the lobby room
            List<Task<FetchDataResponse<string>>> tasks = new List<Task<FetchDataResponse<string>>>();
            tasks.Add(GameServerDataFetcher.FetchFullCatalog());
            await Task.WhenAll(tasks.ToArray());
            bool failed = false;
            foreach (var t in tasks)
            {
                if (!t.Result.Succeeded)
                {
                    failed = true;
                    break;
                }
                else
                {
                    Debug.Log($"Prefetch task result:{t.Result.Data}");
                }
            }

            if (failed)
            {
                // Something goes wrong, shutdown
                ServerManager.Instance.Shutdown();
                Application.Quit();
            }
            else
            {
                // Everything has been loaded, entering the lobby and start server
                SceneManager.LoadSceneAsync(Constants.LobbyScene, LoadSceneMode.Single).completed += (op) =>
                {
                    if (op.isDone)
                    {
                        RegisterCallbacks();
                        Debug.Log($"Starting server on port {ServerManager.Instance.ListeningPort}");
                        NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(ip, ServerManager.Instance.ListeningPort);
                        NetworkManager.Singleton.StartServer();
                    }

                };

            }
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
            NetworkManager.Singleton.StartHost();
        }
#endregion

#region both
        void HandleOnClientConnected(ulong clientId)
        {
            if (dedicatedServer)
            {
                Debug.Log($"Client {clientId} connected.");
                OnClientConnected?.Invoke(clientId);
            }
            
        }

        void HandleOnClientDisconnected(ulong clientId)
        {
            if (dedicatedServer)
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
#endregion




    }

}
