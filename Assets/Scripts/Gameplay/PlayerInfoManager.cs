using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using WLL_NGO.Netcode;

namespace WLL_NGO.Gameplay
{
    public class PlayerInfoManager : NetworkBehaviour
    {
        [SerializeField]
        NetworkList<PlayerInfo> players = new NetworkList<PlayerInfo>();

        private void OnEnable()
        {
            GameServer.OnClientConnected += AddOrUpdatePlayer;
            GameServer.OnClientDisconnected += SetPlayerDisconnected;
        }

        private void OnDisable()
        {
            GameServer.OnClientConnected -= AddOrUpdatePlayer;
            GameServer.OnClientDisconnected -= SetPlayerDisconnected;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsClient)
            {
                players.OnListChanged += HandleOnPlayerInfoListChanged;
            }
        }

        void HandleOnPlayerInfoListChanged(NetworkListEvent<PlayerInfo> changeEvent)
        {
            Debug.Log($"PlayerInfoList has changed; element count:{players.Count}");
        }

        bool ClientExists(ulong clientId)
        {
            foreach (var player in players)
            {
                if (player.ClientId == clientId)
                    return true;
            }
            return false;
        }

        void SetPlayerDisconnected(ulong clientId)
        {
            for(int i=0; i<players.Count; i++)
            {
                if(players[i].ClientId == clientId)
                {
                    PlayerInfo p = players[i];
                    p.Connected = false;
                    return;
                }
            }
            
        }

        
        public void AddOrUpdatePlayer(ulong clientId)
        {
            
            foreach(var p in players)
            {
                if(p.ClientId == clientId)
                {
                    PlayerInfo pi = p;
                    pi.Connected = true;
                    return;
                }
            }
            
            
            players.Add(new PlayerInfo(clientId, true));
            

        }

        
    }

}
