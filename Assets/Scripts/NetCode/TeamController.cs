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
        public static UnityAction<TeamController, PlayerController> OnSelectedPlayerChanged;

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

        private NetworkVariable<NetworkObjectReference> selectedPlayerRef = new NetworkVariable<NetworkObjectReference>(default);
        private PlayerController selectedPlayer = null;
        public PlayerController SelectedPlayer
        {
            get { return selectedPlayer; }
        }

        private void Awake()
        {
            if (home)
                homeTeamController = this;
            else
                awayTeamController = this;
        }


        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            selectedPlayerRef.OnValueChanged += HandleOnSelectedPlayerRefValueChanged;
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

            // Something changed
            if (newNetObj)
                selectedPlayer = newNetObj.GetComponent<PlayerController>();
            else
                selectedPlayer = null;

            OnSelectedPlayerChanged?.Invoke(this, selectedPlayer);

            Debug.Log($"New player selected:{selectedPlayer?.name}");
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

        /// <summary>
        /// Returns all the players in the team
        /// </summary>
        /// <returns></returns>
        public List<PlayerController> GetPlayers()
        {
            List<PlayerController> ret = new List <PlayerController>();
            foreach(PlayerController player in PlayerControllerManager.Instance.PlayerControllers)
            {
                Debug.Log($"{player.PlayerInfo}");
                if(player.PlayerInfo.Home == home)
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
            Debug.Log($"Selecting a new player:{player?.name}");
            if(!IsServer) return;

            if (player == selectedPlayer)
                return;

            if (player)
                selectedPlayerRef.Value = new NetworkObjectReference(player.NetworkObject);
            else
                selectedPlayerRef.Value = default;
        }
    }

}
