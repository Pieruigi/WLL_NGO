using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using WLL_NGO.Netcode;

namespace WLL_NGO.Gameplay
{
    /// <summary>
    /// Manager for player info 
    /// </summary>
#if USE_LOBBY_SCENE
    public class PlayerInfoManager : SingletonNetworkPersistent<PlayerInfoManager>
#else
    public class PlayerInfoManager : SingletonNetwork<PlayerInfoManager>
#endif
    {
        [SerializeField]
        NetworkList<PlayerInfo> players = new NetworkList<PlayerInfo>();

        int playersNeeded = 2;

        //private void OnEnable()
        //{
        //    if (IsServer)
        //    {
        //        GameServer.OnClientConnected += AddPlayer;
        //        GameServer.OnClientDisconnected += RemovePlayer;
        //    }
        //    if (IsClient)
        //    {
        //        //GameClient.OnClientStopped += HandleOnShutdown;
        //        NetworkLauncher.OnClientStopped += HandleOnShutdown;
        //    }
        //}

        //private void OnDisable()
        //{
        //    if (IsServer)
        //    {
        //        GameServer.OnClientConnected -= AddPlayer;
        //        GameServer.OnClientDisconnected -= RemovePlayer;
        //    }
        //    if (IsClient)
        //    {
        //        //GameClient.OnClientStopped -= HandleOnShutdown;
        //        NetworkLauncher.OnClientStopped -= HandleOnShutdown;
        //    }
        //}

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer)
            {
                NetworkLauncher.OnClientConnected += AddPlayer;
                NetworkLauncher.OnClientDisconnected += RemovePlayer;
            }
            if (IsClient)
            {
#if USE_LOBBY_SCENE
                NetworkLauncher.OnClientStopped += HandleOnShutdown;
#endif
            }
            // Both client and server register to the player list changed callback
            players.OnListChanged += HandleOnPlayerInfoListChanged;
        }


#if USE_LOBBY_SCENE
        /// <summary>
        /// Using the lobby scene means we created this object in the lobby scene itself, forcing us to destroy it manually because
        /// it's persistent through scenes.
        /// </summary>
        void HandleOnShutdown()
        {
            if (IsClient)
            {
                Destroy(gameObject);
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            NetworkLauncher.OnClientStopped -= HandleOnShutdown;
        }
#endif
        void HandleOnPlayerInfoListChanged(NetworkListEvent<PlayerInfo> changeEvent)
        {
            
            if (IsClient)
            {
                // Every time the server modifies the player list all the players are notified
                if (changeEvent.Value.ClientId == NetworkManager.LocalClientId)
                {
                    Debug.Log($"Local PlayerInfo has been created by the server for local client (id:{NetworkManager.LocalClientId})");
                    PlayerInfo localPlayer = players[changeEvent.Index];
                    // The local player has been added, we need to send the server our initialization data ( ex. the teamroaster )
                    if(!localPlayer.Initialized)
                        SetPlayerDataServerRpc(NetworkManager.LocalClientId, "json-data");
                };
            }

            if (IsServer)
            {
              
            }

            Debug.Log($"Player list has changed");
            foreach (var player in players)
                Debug.Log(player);
        }

        void HandleOnSceneLoaded()
        {
            Debug.Log("Scene has been loaded");
        }

        

        /// <summary>
        /// Executed on the server to initialize the local client
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="data"></param>
        [ServerRpc(RequireOwnership = false)]
        void SetPlayerDataServerRpc(ulong clientId, string data)
        {
            for(int i=0; i<players.Count; i++)
            {
                if(players[i].ClientId == clientId)
                {
                    var p = players[i];
                    p.Initialize(data);
                    players[i] = p;
                    return;
                }
            }
        }

        /// <summary>
        /// Called by the server when a player disconnect.
        /// </summary>
        /// <param name="clientId"></param>
        void RemovePlayer(ulong clientId)
        {
            int index = -1;
            for(int i=0; i<players.Count && index<0; i++)
            {
                if(players[i].ClientId == clientId)
                    index = i;
            }
            if (index >= 0)
                players.RemoveAt(index);
        }

        /// <summary>
        /// Called on the server to add a new player
        /// </summary>
        /// <param name="clientId"></param>
        public void AddPlayer(ulong clientId)
        {
            // Check whether the player must play in the home or away team
            int homeCount = 0;
            for(int i=0; i<players.Count; i++)
            {
                if (players[i].Home)
                    homeCount++;
            }

            bool home = false;
            if (homeCount == 0)
                home = true;

            // Player not found
            players.Add(PlayerInfo.CreateHumanPlayer(clientId, home));
        }

        /// <summary>
        /// Called by the server to add a bot ( for single player ). 
        /// </summary>
        public void AddBot()
        {
            players.Add(PlayerInfo.CreateBotPlayer());
        }

        public int PlayerCount()
        {
            return players.Count;
        }

        public bool PlayerInitializedAll()
        {
            foreach(var player in players)
            {
                if (!player.Initialized)
                    return false;
            }
            return true;
        }
        
    }

}