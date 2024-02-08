using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport;
using Unity.Services.Matchmaker.Models;
using UnityEngine;
using WLL_NGO.Multiplay;

namespace WLL_NGO.Netcode
{
    public class GameClient : MonoBehaviour
    {
        ushort port;
        string ip;

        // Start is called before the first frame update
        void Start()
        {
#if NO_MM
            port = Constants.NoMatchmakingTestingPort;
            ip = "127.0.0.1";
            StartClient();
#endif
        }

      
        private void OnEnable()
        {
            ClientManager.OnTicketAssigned += HandleOnTicketAssigned;
            ClientManager.OnTicketFailed += HandleOnTicketFailed;
            NetworkManager.Singleton.OnClientStarted += HandleOnClientStarted;
            NetworkManager.Singleton.OnClientStopped += HandleOnClientStopped;
        }

        private void OnDisable()
        {
            ClientManager.OnTicketAssigned -= HandleOnTicketAssigned;
            ClientManager.OnTicketFailed -= HandleOnTicketFailed;
            NetworkManager.Singleton.OnClientStarted += HandleOnClientStarted;
            NetworkManager.Singleton.OnClientStopped += HandleOnClientStopped;
        }

        void HandleOnClientStarted()
        {
            Debug.Log($"Client started, connecting to {NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Address}:{NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Port}");
        }

        void HandleOnClientStopped(bool hostMode)
        {

        }

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

        void StartClient()
        {
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(ip, port);
            NetworkManager.Singleton.StartClient();
        }

        void StartHost()
        {
            NetworkManager.Singleton.StartHost();
        }
    }

}
