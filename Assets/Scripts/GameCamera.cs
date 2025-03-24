using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using WLL_NGO.Netcode;

namespace WLL_NGO
{
    public class GameCamera : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {
            //var lp = PlayerInfoManager.Instance.GetLocalPlayerInfo();
            //Debug.Log($"TEST - Local player info:{lp}");

        }

        // Update is called once per frame
        void Update()
        {

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
            Debug.Log($"TEST - Camera, player:{player}");

            if (NetworkManager.Singleton.IsClient)
            {
            
                if (player.IsLocal && !player.Home && !player.Bot) // Human playing with the away team
                {
                    // Move camera to the other side
                    var newPos = transform.position;
                    newPos.z *= -1;
                    transform.position = newPos;

                    // Rotate camera
                    var newEul = transform.eulerAngles;
                    newEul.y += 180f;
                    transform.eulerAngles = newEul;
                }
            }
        }
    }
    
}
