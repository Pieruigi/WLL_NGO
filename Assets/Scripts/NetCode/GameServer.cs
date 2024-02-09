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
    public class GameServer : MonoBehaviour
    {
        public static UnityAction<ulong> OnClientConnected;
        public static UnityAction<ulong> OnClientDisconnected;

        public static GameServer Instance { get; private set; }

        string internalIp = "0.0.0.0";

        private void Awake()
        {
            if (!Instance)
            {
                Instance = this;
                
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
#if NO_MM
            StartServer();
#else
            //ServerManager.OnMatchmakerPayload += HandleOnMatchmakerPayoload;
#endif
        }

        private void RegisterCallbacks()
        {
            NetworkManager.Singleton.OnServerStarted += HandleOnServerStarted;
            NetworkManager.Singleton.OnServerStopped += HandleOnServerStopped;
            NetworkManager.Singleton.OnClientConnectedCallback += HandleOnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleOnClientDisconnected;
            ServerManager.OnMatchmakerPayload += HandleOnMatchmakerPayoload;
        }

        private void UnregisterCallbacks()
        {
            NetworkManager.Singleton.OnServerStarted -= HandleOnServerStarted;
            NetworkManager.Singleton.OnServerStopped -= HandleOnServerStopped;
            NetworkManager.Singleton.OnClientConnectedCallback -= HandleOnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleOnClientDisconnected;
            ServerManager.OnMatchmakerPayload -= HandleOnMatchmakerPayoload;
        }

        void HandleOnMatchmakerPayoload(MatchmakingResults payload)
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

        void HandleOnClientConnected(ulong clientId)
        {
            Debug.Log($"Client {clientId} connected.");
            OnClientConnected?.Invoke(clientId);
        }

        void HandleOnClientDisconnected(ulong clientId)
        {
            Debug.Log($"Client {clientId} disconnected");
            OnClientDisconnected?.Invoke(clientId);
        }

        async void StartServer()
        {
            // Prefetch data ( for example the full catalog )
            List<Task<FetchDataResponse<string>>> tasks = new List<Task<FetchDataResponse<string>>>();
            tasks.Add(GameServerDataFetcher.FetchFullCatalog());
            await Task.WhenAll(tasks.ToArray());
            bool failed = false;
            foreach(var t in tasks)
            {
                if(!t.Result.Succeeded)
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
                // Shutdown
                ServerManager.Instance.Shutdown();
                Application.Quit();
            }
            else
            {
                
                // Loading lobby and then start the server
                SceneManager.LoadSceneAsync(Constants.LobbyScene, LoadSceneMode.Single).completed += (op) =>
                {
                    if (op.isDone)
                    {
                        RegisterCallbacks();
                        Debug.Log($"Starting server on port {ServerManager.Instance.ListeningPort}");
                        NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(internalIp, ServerManager.Instance.ListeningPort);
                        NetworkManager.Singleton.StartServer();
                    }

                };
            }

            

            
        }


    }

}
