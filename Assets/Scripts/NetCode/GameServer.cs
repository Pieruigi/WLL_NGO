using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Matchmaker.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using WLL_NGO.Multiplay;
using WLL_NGO.Services;

namespace WLL_NGO.Netcode
{
    public class GameServer : MonoBehaviour
    {
        public static GameServer Instance { get; private set; }

        ushort listeningPort;
        string internalIp = "0.0.0.0";

        private void Awake()
        {
            if (!Instance)
            {
                Instance = this;
                listeningPort = ServerManager.Instance.ListeningPort;
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

        private void OnEnable()
        {
            NetworkManager.Singleton.OnServerStarted += HandleOnServerStarted;
            NetworkManager.Singleton.OnClientConnectedCallback += HandleOnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleOnClientDisconnected;
            ServerManager.OnMatchmakerPayload += HandleOnMatchmakerPayoload;
        }

        private void OnDisable()
        {
            NetworkManager.Singleton.OnServerStarted -= HandleOnServerStarted;
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

        void HandleOnClientConnected(ulong clientId)
        {
            Debug.Log($"Client {clientId} connected.");
        }

        void HandleOnClientDisconnected(ulong clientId)
        {
            Debug.Log($"Client {clientId} disconnected");
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
                        Debug.Log($"Starting server on port {listeningPort}");
                        NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(internalIp, listeningPort);
                        NetworkManager.Singleton.StartServer();
                    }

                };
            }

            

            
        }


    }

}
