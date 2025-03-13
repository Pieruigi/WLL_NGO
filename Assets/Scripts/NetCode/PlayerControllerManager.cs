using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Services.Authentication;
using UnityEngine;

namespace WLL_NGO.Netcode
{
    public class PlayerControllerManager : SingletonNetwork<PlayerControllerManager>
    {
       
        [SerializeField] NetworkPrefabsList playerPrefabList;

        List<PlayerController> playerControllers = new List<PlayerController>();
        public ICollection<PlayerController> PlayerControllers
        {
            get { Debug.Log($"Returning players, count:{playerControllers.Count}"); return playerControllers.AsReadOnly(); }
        }
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer)
            {
                PlayerInfoManager.OnPlayerInitialized += HandleOnPlayerInitialized;
            }
                
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            if (IsServer)
                PlayerInfoManager.OnPlayerInitialized -= HandleOnPlayerInitialized;
        }

        /// <summary>
        /// Server only.
        /// Spawn charaters when a palyer info is initialized
        /// </summary>
        /// <param name="player"></param>
        void HandleOnPlayerInitialized(PlayerInfo playerInfo)
        {
            Debug.Log($"Player initialized, spawn controllers, player info:{playerInfo}");

            // Spawn the team using the team roster
            for (int i = 0; i < MatchController.Instance.PlayerPerTeam; i++)
            {
                GameObject go = Instantiate(playerPrefabList.PrefabList[0].Prefab);
                PlayerController pc = go.GetComponent<PlayerController>();

                pc.Init(playerInfo, i);
                Transform spawnPoint = playerInfo.Home ? PlayerSpawnPointManager.Instance.GetKickOffHomeSpawnPoint(i) : PlayerSpawnPointManager.Instance.GetAwaySpawnPoint(i);
                Rigidbody rb = go.GetComponent<Rigidbody>();
                rb.position = spawnPoint.position;
                rb.rotation = spawnPoint.rotation;
                NetworkObject no = go.GetComponent<NetworkObject>();
                no.Spawn();
                
            }
            //NetworkObject no = playerPrefabList.PrefabList[0].Prefab.GetComponent<NetworkObject>();
            //Transform spawnPoint = playerInfo.Home ? PlayerSpawnPointManager.Instance.GetHomeSpawnPoint(0) : PlayerSpawnPointManager.Instance.GetAwaySpawnPoint(0);
            //NetworkManager.Singleton.SpawnManager.InstantiateAndSpawn(no, playerInfo.ClientId, true, false, false, spawnPoint.position, spawnPoint.rotation);
        }

        public void AddPlayerController(PlayerController playerController)
        {
            playerControllers.Add(playerController);
            Debug.Log($"Added new player controller, player count:{playerControllers.Count}");
        }

        public List<PlayerController> GetLocalPlayerControllers(bool bot)
        {
            List<PlayerController> ret = new List<PlayerController>();
            foreach(PlayerController playerController in playerControllers) 
            {
                if(playerController.PlayerInfo.IsLocal && playerController.PlayerInfo.Bot == bot)
                    ret.Add(playerController);
            }

            return ret;
        }

    }

}
