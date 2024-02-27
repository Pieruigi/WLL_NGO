using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace WLL_NGO.Netcode
{
    /// <summary>
    /// NotReady: waiting for all players to be ready
    /// </summary>
    

    public class MatchController : SingletonNetwork<MatchController>
    {
        /// <summary>
        /// Param1: old value
        /// Param2: new value
        /// </summary>
        public UnityAction<int, int> OnStateChanged;

        //ushort numOfPlayers = 1;
        //public ushort NumberOfPlayers
        //{
        //    get { return numOfPlayers; }
        //}

        NetworkVariable<byte> matchState = new NetworkVariable<byte>((byte)MatchState.NotReady);

        int playerPerTeam = 5;
        public int PlayerPerTeam
        {
            get { return playerPerTeam; }
        }

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

        /// <summary>
        /// Called when match state changes
        /// </summary>
        /// <param name="previousValue"></param>
        /// <param name="newValue"></param>
        private void HandleOnMatchStateChanged(byte previousValue, byte newValue)
        {
            switch (newValue)
            {
                case (byte)MatchState.StartingMatch:
                    // Reset the tick timer
                    NetworkTimer.Instance.Reset();
                    // Select the last player
                    //foreach(PlayerController p in )
                    List<PlayerController> players = TeamController.HomeTeam.GetPlayers();
                    Debug.Log($"PlayerController.Count:{players.Count}");
                    TeamController.HomeTeam.SetPlayerSelected(players[playerPerTeam-1]);
                    //TeamController.HomeTeam.SetPlayerSelected(players[0]);
                    break;
            }

            // Do something
            OnStateChanged?.Invoke(previousValue, newValue);
        }


        

        /// <summary>
        /// Called on server to process match states.
        /// </summary>
        void UpdateMatchState()
        {
            switch (matchState.Value)
            {
                case (byte)MatchState.NotReady:
                    if(IsServer)
                    {
                        // If all players are ready we can start the game
                        if (PlayerInfoManager.Instance.PlayerInitializedAll() && PlayerInfoManager.Instance.PlayerReadyAll())
                        {
                            SetMatchState(MatchState.StartingMatch);
                        }
                    }
                   
                    break;
            }
        }


        public void SetMatchState(MatchState newMatchState)
        {
            if (!IsServer) return;

            Debug.Log($"Server - Setting new match state: {newMatchState}");
            matchState.Value = (byte)newMatchState;
        }

    }

}
