using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering.Universal.Internal;
using WLL_NGO.Netcode;

namespace WLL_NGO.AI
{
    public class FormationManager : MonoBehaviour
    {
        [SerializeField]
        List<GameObject> formationPrefabs;

        FormationHelper homeHelper, awayHelper;

        // Start is called before the first frame update
        void Start()
        {

#if TEST_AI
            InitFormationHelper(true, "12");
            InitFormationHelper(false, "12");
#else
            if (!NetworkManager.Singleton.IsServer) // We don't need formation helper on client
                return;

          
            // Create 
            var players = PlayerInfoManager.Instance.GetPlayerInfoAll();
           
            foreach (var player in players)
            {
                InitFormationHelper(player.Home, "12");
            }
            PlayerInfo.OnInitializedChanged += HandleOnPlayerInitialized;
#endif


        }

        void OnDestroy()
        {
            PlayerInfo.OnInitializedChanged -= HandleOnPlayerInitialized;
        }

        private void HandleOnPlayerInitialized(PlayerInfo player)
        {
            InitFormationHelper(player.Home, "12");
        }

        private void InitFormationHelper(bool home, string formationType)
        {

            // Create a new formation helper
            FormationHelper helper = home ? FormationHelper.HomeFormationHelper : FormationHelper.AwayFormationHelper;

            //TODO: you should get the formation from json

            // Get prefab from list
            List<GameObject> prefabs = formationPrefabs.FindAll(f => f.name.EndsWith(formationType));
            GameObject prefab = prefabs[UnityEngine.Random.Range(0, prefabs.Count)];

            // Initialize helper
            helper.Initialize(prefab);

        }
        


    }
    
}
