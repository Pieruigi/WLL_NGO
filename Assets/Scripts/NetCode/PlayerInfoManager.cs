using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using WLL_NGO.Netcode;

namespace WLL_NGO.Netcode
{
    /// <summary>
    /// This class handles with the PlayerInfo class.
    /// When a new client connects to the game server a new player info is created to the server and synchronized with all the clients.
    /// The client owning the created player info sends all the needed info to the server ( for example the PlayFabId and the team roster ) that flags 
    /// the player has 'initialized'.
    /// The flag 'ready' instead means that the client has properly loaded the game scene ( I mean addressables and other needed stuff ) and works the same as the 
    /// initialize flag. 
    /// When all players are ready the match can finally start ( check the MatchHandler class for more info ).
    /// </summary>
#if USE_LOBBY_SCENE
    public class PlayerInfoManager : SingletonNetworkPersistent<PlayerInfoManager>
#else
    public class PlayerInfoManager : SingletonNetwork<PlayerInfoManager>
#endif
    {
        //public static UnityAction<PlayerInfo> OnPlayerInitialized;

        [SerializeField]
        GameObject playerInfoPrefab;

        List<PlayerInfo> players = new List<PlayerInfo>();



        //[SerializeField] NetworkList<

        int playersNeeded = 2;



        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer)
            {
                NetworkLauncher.OnClientConnected += CreatePlayer;
                NetworkLauncher.OnClientDisconnected += RemovePlayer;
            }
            if (IsClient)
            {
#if USE_LOBBY_SCENE
                NetworkLauncher.OnClientStopped += HandleOnShutdown;
#endif
            }
        
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
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
      
        void HandleOnSceneLoaded()
        {
            Debug.Log("Scene has been loaded");
        }

        void InitializeBot(string data)
        {
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].Bot)
                {
                    var p = players[i];
                    p.Initialize(data);
                    players[i] = p;

                    //OnPlayerInitialized?.Invoke(players[i]);

                    return;
                }
            }
        }

     
        // /// <summary>
        // /// Executed on the server to initialize the local client
        // /// </summary>
        // /// <param name="clientId"></param>
        // /// <param name="data"></param>
        // [ServerRpc(RequireOwnership = false)]
        // void InizialitePlayerServerRpc(ulong clientId, string data)
        // {
        //     for (int i = 0; i < players.Count; i++)
        //     {
        //         if (players[i].ClientId == clientId && !players[i].Bot)
        //         {
        //             var p = players[i];
        //             p.Initialize(data);
        //             players[i] = p;

        //             //OnPlayerInitialized?.Invoke(players[i]);

        //             return;
        //         }
        //     }
        // }

       
        /// <summary>
        /// Called by the server when a player disconnect.
        /// </summary>
        /// <param name="clientId"></param>
        void RemovePlayer(ulong clientId)
        {
            int index = -1;
            for (int i = 0; i < players.Count && index < 0; i++)
            {
                if (players[i].ClientId == clientId)
                    index = i;
            }
            if (index >= 0)
                players.RemoveAt(index);
        }

        /// <summary>
        /// Called on the server to add a new player
        /// </summary>
        /// <param name="clientId"></param>
        public void CreatePlayer(ulong clientId)
        {
            // Check whether the player must play in the home or away team
            int homeCount = 0;
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].Home)
                    homeCount++;
            }

            bool home = false;
            if (homeCount == 0)
                home = true;

            // Player not found
            var p = Instantiate(playerInfoPrefab);
            p.GetComponent<PlayerInfo>().CreateHumanPlayer(clientId, home);
            p.GetComponent<NetworkObject>().Spawn();
            //players.Add(PlayerInfo.CreateHumanPlayer(clientId, home));
        }

        /// <summary>
        /// Called by the server to add a bot ( for single player ). 
        /// </summary>
        public void CreateBot()
        {
            var p = Instantiate(playerInfoPrefab);
            p.GetComponent<PlayerInfo>().CreateBotPlayer();
            p.GetComponent<NetworkObject>().Spawn();
        }

        public void AddPlayer(PlayerInfo player)
        {
            if (players.Contains(player))
                return;
            players.Add(player);


            if (IsClient)
            {
                // Every time the server modifies the player list all the players are notified
                if (player.ClientId == NetworkManager.LocalClientId)
                {

                    //PlayerInfo player = players[changeEvent.Index];
                    // The local player has been added, we need to send initialization data ( ex. the teamroaster ) to the server
                    if (!player.Bot) // Human player
                    {
                        if (!player.Initialized)
                        {
                            player.InizialiteServerRpc("json-data");
                        }

                    }


                }
                ;
            }

            if (IsServer)
            {

            }

            if (IsHost) // We only manage bot player here
            {
                if (player.ClientId == NetworkManager.LocalClientId)
                {
                    // The local client id means both human and both player
                    //PlayerInfo player = players[changeEvent.Index];
                    if (player.Bot)
                    {
                        if (!player.Initialized)
                            InitializeBot("json-bot-data");
                    }
                    else
                    {
                        if (!BotPlayerInfoExists()) // Bot not created yet
                        {
                            // Create bot
                            CreateBot();
                        }
                    }



                }
            }

            Debug.Log($"New player added:{player}");
        }

        public void RemovePlayer(PlayerInfo player)
        {
            players.Remove(player);
        }

        public int PlayerCount()
        {
            return players.Count;
        }

        public bool PlayerInitializedAll()
        {
            if (players.Count < playersNeeded)
                return false;

            foreach (var player in players)
            {
                if (!player.Initialized)
                    return false;
            }
            return true;
        }

        public bool PlayerReadyAll()
        {
            if (players.Count < playersNeeded)
                return false;

            foreach (var player in players)
            {
                if (!player.Ready) return false;
            }
            return true;
        }

        public PlayerInfo GetBotPlayerInfo()
        {
            foreach (var player in players)
            {
                if (player.ClientId == NetworkManager.LocalClientId && player.Bot) return player;
            }
            return default;
        }

        public bool BotPlayerInfoExists()
        {
            foreach (var player in players)
            {
                if (player.ClientId == NetworkManager.LocalClientId && player.Bot)
                    return true;
            }

            return false;
        }

        public PlayerInfo GetLocalPlayerInfo()
        {
            foreach (var player in players)
            {
                if (player.ClientId == NetworkManager.LocalClientId && !player.Bot) return player;
            }
            return default;
        }

        public bool LocalPlayerInfoExists()
        {
            foreach (var player in players)
            {
                if (player.ClientId == NetworkManager.LocalClientId && !player.Bot)
                    return true;
            }

            return false;
        }

        public PlayerInfo GetPlayerInfoById(string playerInfoId)
        {
            foreach (PlayerInfo player in players)
            {
                if (player.Id == playerInfoId) return player;
            }

            return default;
        }

        // public List<PlayerInfo> GetPlayerInfoAll(bool home)
        // {
        //     List<PlayerInfo> ret = new List<PlayerInfo>();
        //     foreach (var player in players)
        //     {
        //         if (player.Home == home) ret.Add(player);
        //     }
        //     return ret;
        // }

        public List<PlayerInfo> GetPlayerInfoAll()
        {
            List<PlayerInfo> ret = new List<PlayerInfo>();
            foreach (var player in players)
            {
                ret.Add(player);
            }
            return ret;
        }

        public PlayerInfo GetHomePlayerInfo()
        {
            foreach (var p in players)
            {
                if (p.Home)
                    return p;
            }

            return default;
        }

        public PlayerInfo GetAwayPlayerInfo()
        {
            foreach (var p in players)
            {
                if (!p.Home)
                    return p;
            }

            return default;
        }

    }

}
