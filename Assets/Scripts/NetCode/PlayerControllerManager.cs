using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Services.Authentication;
using UnityEngine;
using WLL_NGO.Gameplay;

namespace WLL_NGO.Gameplay
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
            pc.transform.position = Vector3.zero;
            pc.transform.rotation = Quaternion.identity;
            NetworkObject no = go.GetComponent<NetworkObject>();
            no.Spawn();
            

        }

        public void AddPlayerController(PlayerController playerController)
        {
            playerControllers.Add(playerController);
        }
    }

}
