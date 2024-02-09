using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace WLL_NGO.Gameplay
{
    /// <summary>
    /// InLobby: we are still in the lobby scene
    /// NotReady: we are in the game scene but the game is not ready yet
    /// </summary>
    public enum MatchState { NotReady }

    public class MatchController : NetworkBehaviour
    {
        //ushort numOfPlayers = 1;
        //public ushort NumberOfPlayers
        //{
        //    get { return numOfPlayers; }
        //}

        NetworkVariable<byte> matchState = new NetworkVariable<byte>((byte)MatchState.NotReady);

        private void Update()
        {
            if (IsServer)
                return;
            if (Input.GetKeyDown(KeyCode.Q))
            {
                NetworkManager.Singleton.Shutdown();
            }
        }

        private void FixedUpdate()
        {
            if (IsServer)
            {
                UpdateMatchState();
            }


        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            matchState.OnValueChanged += HandleOnMatchStateChanged;
        }

        private void HandleOnMatchStateChanged(byte previousValue, byte newValue)
        {
            // Do something
        }


        //bool MatchCanStart()
        //{
        //    if (PlayerInfoManager.Instance.PlayerCount() < numOfPlayers || !PlayerInfoManager.Instance.PlayerInitializedAll())
        //        return false;
            
        //    return true;
        //}

        /// <summary>
        /// Called on server to process match states.
        /// </summary>
        void UpdateMatchState()
        {
            switch (matchState.Value)
            {
                //case (byte)MatchState.InLobby:
                //    // If all players are connected and initialized the server starts the match
                //    if (MatchCanStart())
                //    {
                //        matchState.Value = (byte)MatchState.LoadingGameScene;
                //        // Load game scene
                //        NetworkSceneLoader.Instance.LoadGameScene(OnGameSceneLoadingCallback);
                //    }
                //    break;
            }
        }

        //void OnGameSceneLoadingCallback()
        //{
        //    matchState.Value = (byte)MatchState.NotReady;
        //}


    }

}
