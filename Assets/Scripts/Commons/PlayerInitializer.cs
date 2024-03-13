using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using WLL_NGO.AI;
using WLL_NGO.Netcode;

namespace WLL_NGO
{
    public class PlayerInitializer : MonoBehaviour
    {
        int playerRole = -1;
        public int PlayerRole
        {
            get { return playerRole; }
        }

        private void OnEnable()
        {
            PlayerController.OnSpawned += HandleOnPlayerSpawned;
        }

        private void OnDisable()
        {
            PlayerController.OnSpawned -= HandleOnPlayerSpawned;
        }

        void HandleOnPlayerSpawned(PlayerController playerController)
        {
            PlayerController pc = GetComponent<PlayerController>();
            if(pc == playerController)
            {
                InitPlayer(pc);
            }
        }

        void InitPlayer(PlayerController pc)
        {
            if(pc.Index == 0) // Goalkeeper
            {
                playerRole = (int)PlayerRole.GK;
                gameObject.AddComponent<GoalkeeperAI>();
            }
        }
    }

}
