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
            Debug.Log("Player initialized, spawn controllers");

            // Spawn the team using the team roster
            GameObject go = Instantiate(playerPrefabList.PrefabList[0].Prefab);
            PlayerController pc = go.GetComponent<PlayerController>();
            pc.Init(playerInfo);
            Transform spawnPoint = playerInfo.Home ? PlayerSpawnPointManager.Instance.GetHomeSpawnPoint(0) : PlayerSpawnPointManager.Instance.GetAwaySpawnPoint(0);
            pc.transform.position = spawnPoint.position;
            pc.transform.rotation = spawnPoint.rotation;
            NetworkObject no = go.GetComponent<NetworkObject>();
            no.Spawn();
            

        }

        public void AddPlayerController(PlayerController playerController)
        {
            playerControllers.Add(playerController);
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