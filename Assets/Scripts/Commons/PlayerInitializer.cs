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
                pc.SetRole((int)PlayerRole.GK);
                //gameObject.AddComponent<GoalkeeperAI>();
            }
            else
            {
                pc.SetRole((int)PlayerRole.DF);
                //Destroy(gameObject.GetComponent<GoalkeeperAI>());
            }
        }
    }

}
