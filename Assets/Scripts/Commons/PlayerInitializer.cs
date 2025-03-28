using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using WLL_NGO.AI;
using WLL_NGO.Netcode;
using WLL_NGO.UI;

namespace WLL_NGO
{
    public class PlayerInitializer : MonoBehaviour
    {

        [SerializeField]
        Material homeMaterial, awayMaterial;


        [SerializeField]
        MeshRenderer meshRenderer;

#if !UNITY_SERVER
        [SerializeField]
        GameObject playerMarkerPrefab;
#endif

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
            if (pc == playerController)
            {
                InitPlayer(pc);
            }
        }

        void InitPlayer(PlayerController pc)
        {
            if (pc.Index == 0) // Goalkeeper
            {
                pc.SetRole((int)PlayerRole.GK);
                //gameObject.AddComponent<GoalkeeperAI>();
            }
            else
            {
                // TODO: add all roles
                pc.SetRole((int)PlayerRole.DF);
                //Destroy(gameObject.GetComponent<GoalkeeperAI>());
            }

            // TODO: we could read the team roster and load character from addressables here
            meshRenderer.material = pc.PlayerInfo.Home ? homeMaterial : awayMaterial;

// #if !UNITY_SERVER
//             // Spawn marker
//             GameObject pm = Instantiate(playerMarkerPrefab, transform);
//             pm.transform.localPosition = Vector3.zero;
//             pm.transform.localRotation = Quaternion.identity;
//             pm.GetComponent<PlayerMarker>().Init(pc);
// #endif
        }

    }

}
