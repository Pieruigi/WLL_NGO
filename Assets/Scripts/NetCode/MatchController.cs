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
using WLL_NGO.AI;

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

        // [SerializeField]
        // GameObject[] matchRulerPrefabs;

        NetworkVariable<byte> matchState = new NetworkVariable<byte>((byte)MatchState.NotReady);

        public MatchState MatchState
        {
            get { return (MatchState)matchState.Value; }
        }

        
        int playerPerTeam = 4;
        public int PlayerPerTeam
        {
            get { return playerPerTeam; }
        }

        TeamController lastScorer = null;

        //MatchRuler matchRuler;

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
            // If server create match ruler
            // if (IsServer)
            // {
            //     GameObject mr = Instantiate(matchRulerPrefabs[(int)GameMode.Powered]);
            //     mr.GetComponent<NetworkObject>().Spawn(true);
            // }
        }

        void OnEnable()
        {
            NetController.OnGoalScored += HandleOnGoalScored;
        }

        void OnDisable()
        {
            NetController.OnGoalScored -= HandleOnGoalScored;
        }

        private void HandleOnGoalScored(TeamController scorer)
        {
            lastScorer = scorer;
            SetMatchState(MatchState.Goal);
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
                    NetworkTimer.Instance.StartTimer();
                    // Select the last player

                    //SetPlayingState(true);
                    SetKickOffState(true);

                    break;
                case (byte)MatchState.KickOff:
                    // Check the team who's going to kick off (only after a goal)
                    if (IsServer) // Last scorer is not null only on server
                    {
                        if (lastScorer != null)
                        {
                            // List<Transform> homeSP = lastScorer == TeamController.HomeTeam ? PlayerSpawnPointManager.Instance.GetHomeSpawnPoints() : PlayerSpawnPointManager.Instance.GetKickOffHomeSpawnPoints();
                            // List<Transform> awaySP = lastScorer == TeamController.HomeTeam ? PlayerSpawnPointManager.Instance.GetKickOffAwaySpawnPoints() : PlayerSpawnPointManager.Instance.GetAwaySpawnPoints();

                            IList<Transform> homeSP = lastScorer == TeamController.HomeTeam ? FormationHelper.HomeFormationHelper.GetKickOffSpawnPoints() : FormationHelper.HomeFormationHelper.GetKickOffKickerSpawnPoints();
                            IList<Transform> awaySP = lastScorer == TeamController.HomeTeam ? FormationHelper.AwayFormationHelper.GetKickOffKickerSpawnPoints() : FormationHelper.AwayFormationHelper.GetKickOffSpawnPoints();

                            for (int i = 0; i < TeamController.HomeTeam.GetPlayers().Count; i++)
                            {

                                TeamController.HomeTeam.GetPlayers()[i].ResetToKickOff(homeSP[i]);
                                TeamController.AwayTeam.GetPlayers()[i].ResetToKickOff(awaySP[i]);

                            }

                            // Reset the ball
                            BallController.Instance.ResetToKickOff();
                            
                            lastScorer = null;
                        }

                        //TODO: eventually reset stunned or busy states on each player
                        TeamController.HomeTeam.SetPlayerSelected(TeamController.HomeTeam.GetPlayers()[playerPerTeam - 1]);
                        TeamController.AwayTeam.SetPlayerSelected(TeamController.AwayTeam.GetPlayers()[playerPerTeam - 1]);

                        
                    }
                    break;
                case (int)MatchState.Goal:
                    EnterGoalState();
                    break;
                case (int)MatchState.Replay:
                    EnterReplayState();
                    break;
            }

            // Do something
            OnStateChanged?.Invoke(previousValue, newValue);
        }

        async void EnterGoalState()
        {
            // Do celebration or whatever you want here
            await Task.Delay(3000);
            // Move to replay state
            SetMatchState(MatchState.Replay);
        }

        async void EnterReplayState()
        {
            // Do replay here
            await Task.Delay(5000);
            // Move to kick off state
            SetMatchState(MatchState.KickOff);
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

            matchState.Value = (byte)newMatchState;
        }




    }

}
