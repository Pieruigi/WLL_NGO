
//using System.Collections;
//using System.Collections.Generic;
//using Unity.Netcode;
//using Unity.Netcode.Transports.UTP;
//using Unity.Networking.Transport;
//using Unity.Services.Matchmaker.Models;
//using UnityEngine;
//using UnityEngine.Events;
//using UnityEngine.SceneManagement;
//using WLL_NGO.Gameplay;
//using WLL_NGO.Multiplay;

//namespace WLL_NGO.Netcode
//{
//    public class GameClient : SingletonPersistent<GameClient>
//    {
       
//        public static UnityAction OnClientStopped;
        
//        ushort port;
//        string ip;

//        // Start is called before the first frame update
//        void Start()
//        {
//#if NO_MM
//            port = Constants.NoMatchmakingTestingPort;
//            ip = "127.0.0.1";
//            //StartClient();
//#endif
//        }

        
//        private void OnEnable()
//        {
//            ClientManager.OnTicketAssigned += HandleOnTicketAssigned;
//            ClientManager.OnTicketFailed += HandleOnTicketFailed;
//            NetworkManager.Singleton.OnClientStarted += HandleOnClientStarted;
//            NetworkManager.Singleton.OnClientStopped += HandleOnClientStopped;
//            NetworkManager.Singleton.OnClientConnectedCallback += HandleOnClientConnected;
//            NetworkManager.Singleton.OnClientDisconnectCallback += HandleOnClientDisconnected;
//        }

//        private void OnDisable()
//        {
//            ClientManager.OnTicketAssigned -= HandleOnTicketAssigned;
//            ClientManager.OnTicketFailed -= HandleOnTicketFailed;
//            NetworkManager.Singleton.OnClientStarted -= HandleOnClientStarted;
//            NetworkManager.Singleton.OnClientStopped -= HandleOnClientStopped;
//            NetworkManager.Singleton.OnClientConnectedCallback -= HandleOnClientConnected;
//            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleOnClientDisconnected;
//        }

//        void HandleOnClientStarted()
//        {
//            Debug.Log($"Client started, connecting to {NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Address}:{NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Port}");
//        }

//        void HandleOnClientStopped(bool hostMode)
//        {
//            OnClientStopped?.Invoke();
//        }

//        void HandleOnClientConnected(ulong clientId)
//        {
//            Debug.Log($"Client {clientId} connected.");
//        }

//        void HandleOnClientDisconnected(ulong clientId)
//        {

//            Debug.Log($"Client {clientId} disconnected");
//            bool isLocalClient = NetworkManager.Singleton.LocalClientId == clientId;
//            Shutdown();
//        }

//        void HandleOnTicketAssigned(MultiplayAssignment assignment)
//        {
//            port = (ushort)assignment.Port;
//            ip = assignment.Ip;
//            StartClient();
//        }

//        void HandleOnTicketFailed()
//        {
//            StartHost();
//        }

//        void Shutdown()
//        {
//            NetworkManager.Singleton.Shutdown();
//            SceneManager.LoadScene(Constants.ClientMainScene, LoadSceneMode.Single);
//        }

//        public void StartClient()
//        {
//            NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(ip, port);
//            NetworkManager.Singleton.StartClient();
//        }

//        public void StartHost()
//        {
//            NetworkManager.Singleton.StartHost();
//        }
//    }

//}
