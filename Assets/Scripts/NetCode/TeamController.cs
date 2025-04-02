using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.Services.Matchmaker.Models;
using UnityEngine;
using UnityEngine.Events;
using WLL_NGO.Services;

namespace WLL_NGO.Netcode
{
    public class TeamController : NetworkBehaviour
    {
        public static UnityAction<TeamController, /*Old*/PlayerController, /*New*/PlayerController> OnSelectedPlayerChanged;

        public static UnityAction<TeamController> OnTeamControllerSpawned;
        public static UnityAction<TeamController> OnTeamControllerDespawned;

        static TeamController homeTeamController, awayTeamController;
        public static TeamController HomeTeam
        {
            get { return homeTeamController; }
        }
        public static TeamController AwayTeam
        {
            get { return awayTeamController; }
        }

        //private NetworkVariable<bool> home = new NetworkVariable<bool>();
        [SerializeField]
        bool home;
        public bool Home
        {
            get { return home; }
        }

        private NetworkVariable<NetworkObjectReference> selectedPlayerRef = new NetworkVariable<NetworkObjectReference>(default);
        private PlayerController selectedPlayer = null;
        public PlayerController SelectedPlayer
        {
            get { return selectedPlayer; }
        }

        NetworkVariable<int> score = new NetworkVariable<int>(0);
        public int Score
        {
            get { return score.Value; }
            set { score.Value = value; }
        }

        private void Awake()
        {
            if (home)
                homeTeamController = this;
            else
                awayTeamController = this;


        }

        void Update()
        {
            if (!IsSpawned) return;
            if (!MatchController.Instance || !MatchController.Instance.IsSpawned) return;

            if (MatchController.Instance.MatchState == MatchState.Playing)
            {
                // Only control gk when has or is receiving the ball
                var gk = GetPlayers()[0];

                if (gk.IsSelected() && !gk.HasBall && gk.GetState() != (byte)PlayerState.Receiver)
                {
                    // Select another player 
                    SelectClosestPlayerToBall(goalkeeperAllowed: false);
                }

            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            selectedPlayerRef.OnValueChanged += HandleOnSelectedPlayerRefValueChanged;
            MatchController.OnStateChanged += HandleOnMatchStateChanged;

            OnTeamControllerSpawned?.Invoke(this);
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            MatchController.OnStateChanged -= HandleOnMatchStateChanged;
            OnTeamControllerDespawned?.Invoke(this);
        }

        private void HandleOnMatchStateChanged(int oldState, int newState)
        {

        }

        private void HandleOnSelectedPlayerRefValueChanged(NetworkObjectReference previousValue, NetworkObjectReference newValue)
        {


            // Read the new network object
            NetworkObject newNetObj;
            newValue.TryGet(out newNetObj);

            // Try to get the current selected net objet if any
            NetworkObject preNetObject;
            previousValue.TryGet(out preNetObject);

            if (newNetObj == preNetObject)
                return; // Nothing changed

            // We reset the input of the previous player since the ai will take care of it
            if (preNetObject)
                preNetObject.GetComponent<PlayerController>().GetInputHandler().ResetInput();

            // Something changed
            if (newNetObj)
                selectedPlayer = newNetObj.GetComponent<PlayerController>();
            else
                selectedPlayer = null;


            OnSelectedPlayerChanged?.Invoke(this, preNetObject ? preNetObject.GetComponent<PlayerController>() : null, selectedPlayer);


        }


        public bool IsBot()
        {
            // List<PlayerInfo> list = PlayerInfoManager.Instance.GetPlayerInfoAll(home);
            // return list[0].Bot;
            if (home)
                return false;

            return PlayerInfoManager.Instance.BotPlayerInfoExists();

        }

        /// <summary>
        /// Returns the team of a specific player info
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static TeamController GetPlayerTeam(PlayerController player)
        {
            return player.PlayerInfo.Home ? TeamController.HomeTeam : TeamController.AwayTeam;
        }

        public static TeamController GetOpponentTeam(PlayerController player)
        {
            return !player.PlayerInfo.Home ? TeamController.HomeTeam : TeamController.AwayTeam;
        }

        /// <summary>
        /// Returns all the players in the team
        /// </summary>
        /// <returns></returns>
        public List<PlayerController> GetPlayers()
        {
            List<PlayerController> ret = new List<PlayerController>();
            foreach (PlayerController player in PlayerControllerManager.Instance.PlayerControllers)
            {
                if (player.PlayerInfo.Home == home)
                    ret.Add(player);
            }

            return ret;
        }

        /// <summary>
        /// Set a specific player as the selected one ( null accepted )
        /// </summary>
        /// <param name="player"></param>
        public void SetPlayerSelected(PlayerController player)
        {
            //Debug.Log($"Selecting a new player:{player?.name}");
            if (!IsServer) return;

            if (player == selectedPlayer)
                return;

            if (player)
                selectedPlayerRef.Value = new NetworkObjectReference(player.NetworkObject);
            else
                selectedPlayerRef.Value = default;
        }

        public void SelectClosestPlayerToBall(bool goalkeeperAllowed = false)
        {
            List<PlayerController> players = GetPlayers().FindAll(p => p.GetState() == (byte)PlayerState.Normal && (p.Role != PlayerRole.GK || goalkeeperAllowed));
            if (players == null) // No players in normal state, check from all players
            {
                players = GetPlayers().FindAll(p => p.Role != PlayerRole.GK || goalkeeperAllowed);
            }
            // Get the closest one
            float minDist = 0;
            PlayerController select = null;
            foreach (var player in players)
            {
                var dist = Vector3.Distance(player.Position, BallController.Instance.Position);
                if (!select || dist < minDist)
                {
                    select = player;
                    minDist = dist;
                }
            }

            // Set player selected
            SetPlayerSelected(select);
        }
    }

}
