using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Mathematics;
using Unity.Netcode;
using Unity.Netcode.Components;
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
        public static UnityAction<int, int> OnStateChanged;

        //ushort numOfPlayers = 1;
        //public ushort NumberOfPlayers
        //{
        //    get { return numOfPlayers; }
        //}

        NetworkVariable<byte> matchState = new NetworkVariable<byte>((byte)MatchState.NotReady);

        public MatchState MatchState
        {
            get { return (MatchState)matchState.Value; }
        }

        int playerPerTeam = 5;
        public int PlayerPerTeam
        {
            get { return playerPerTeam; }
        }

        private void Update()
        {
            //if (IsServer)
            //    return;
            //if (Input.GetKeyDown(KeyCode.Q))
            //{
            //    NetworkManager.Singleton.Shutdown();
            //}
            //NetworkTimer.Instance.Update(Time.deltaTime);
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
            Debug.Log($"TEST - Changing state state from {previousValue} to {newValue}");
            switch (newValue)
            {
                case (byte)MatchState.StartingMatch:
                    // Reset the tick timer
                    NetworkTimer.Instance.StartTimer();
                    // Select the last player

                    //SetPlayingState(true);
                    SetKickOffState(true);

                    break;
                case (byte)MatchState.KickOff:
                    // Check the team who's going to kick off (only after a goal)
                    

                    //TODO: eventually reset stunned or busy states on each player
                    TeamController.HomeTeam.SetPlayerSelected(TeamController.HomeTeam.GetPlayers()[playerPerTeam - 1]);
                    TeamController.AwayTeam.SetPlayerSelected(TeamController.AwayTeam.GetPlayers()[playerPerTeam - 1]);
                    break;
            }

            // Do something
            OnStateChanged?.Invoke(previousValue, newValue);
        }

        // public void SetPlayingState()
        // {
        //     if (!IsServer)
        //         return;

        //     // if (delayed)
        //     //     await Task.Delay(TimeSpan.FromSeconds(NetworkTimer.Instance.DeltaTick));

        //     matchState.Value = (byte)MatchState.Playing;

        // }

        async void SetKickOffState(bool delayed = false)
        {
            if (!IsServer)
                return;

            if (delayed)
                await Task.Delay(TimeSpan.FromSeconds(NetworkTimer.Instance.DeltaTick));

            matchState.Value = (byte)MatchState.KickOff;
        }

        void SetStartingMatchState()
        {
            BallController.OnBallSpawned -= SetStartingMatchState;
            SetMatchState(MatchState.StartingMatch);
        }


        /// <summary>
        /// Called on server to process match states.
        /// </summary>
        void UpdateMatchState()
        {
            switch (matchState.Value)
            {
                case (byte)MatchState.NotReady:
                    if (IsServer)
                    {
                        // If all players are ready we can start the game
                        if (PlayerInfoManager.Instance.PlayerInitializedAll() && PlayerInfoManager.Instance.PlayerReadyAll())
                        {
                            BallController.OnBallSpawned += SetStartingMatchState;
                            BallSpawner.Instance.SpawnBall();
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

        // public bool IsPlaying()
        // {
        //     return matchState.Value == (byte)MatchState.Playing;
        // }


    }

}
