using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Matchmaker.Models;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using WLL_NGO.Netcode;

namespace WLL_NGO
{
    /// <summary>
    /// Load and stream all the scene objects in the client game scene and report the server when the client is ready.
    /// </summary>
    public class ClientGameSceneLoader : MonoBehaviour
    {
        private void Awake()
        {
            if(Utility.IsDedicatedServer())
            {
                Destroy(gameObject);
            }
            // else
            // {
            //     // Load and stream data
            //     Initialize();
            // }
        }

        void OnEnable()
        {
            PlayerInfo.OnInitializedChanged += HandleOnPlayerInitialized;    
        }

        void OnDisable()
        {
            PlayerInfo.OnInitializedChanged -= HandleOnPlayerInitialized;    
        }

        private void HandleOnPlayerInitialized(PlayerInfo player)
        {
            MoveToReady(player);
        }

        async void MoveToReady(PlayerInfo player)
        {
            await Task.Delay(3000); // Just for test

            if (player.IsLocal)
            {
                if (!player.Bot)
                    player.SetReadyServerRpc(true);
                else
                    player.SetReady(true);
            }

            


        }
    }

}
