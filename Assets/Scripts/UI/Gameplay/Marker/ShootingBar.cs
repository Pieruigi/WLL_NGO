#if !UNITY_SERVER
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.Threading.Tasks;
using UnityEngine;
using WLL_NGO.AI;
using WLL_NGO.Interfaces;
using WLL_NGO.Netcode;

namespace WLL_NGO.UI
{
    public class ShootingBar : MonoBehaviour
    {
        [SerializeField]
        GameObject root;

        [SerializeField]
        GameObject cursor;

        [SerializeField]
        Animator animator;

        float max = 2.5f;

        TeamController teamController;

        bool active = false;


        // Start is called before the first frame update
        void Start()
        {
            
        }

        // Update is called once per frame
        void Update()
        {
            
        }

        void LateUpdate()
        {
            if (!teamController || !teamController.IsSpawned) return;

            Debug.Log("TEST - ddddddddddddddddddddddddddddddd");

            if (!teamController.SelectedPlayer || teamController.SelectedPlayer.GetState() != (byte)PlayerState.Normal)
            {

                animator.SetTrigger("Hide");
                // if (root.activeSelf)
                //     root.SetActive(false);
                return;
            }
                
           
            InputHandler ih = (InputHandler)teamController.SelectedPlayer.GetInputHandler();

            if (ih.Button2)
            {
                //if (!root.activeSelf)
                {
                     animator.SetTrigger("Show");
                    //root.SetActive(true);
                }
            }
            else
            {
                //if (root.activeSelf)
                {
                    animator.SetInteger("Type", 1);
                    animator.SetTrigger("Glow");
                    //root.SetActive(false);
                }     
            }
        }

        void OnEnable()
        {
            PlayerInfo pinfo = FindObjectsOfType<PlayerInfo>().ToList().Find(p => p.IsLocal && !p.Bot);
            if (pinfo) HandleOnPlayerInitializedChanged(pinfo);
            PlayerInfo.OnInitializedChanged += HandleOnPlayerInitializedChanged;
        }

        void OnDisable()
        {
            PlayerInfo.OnInitializedChanged -= HandleOnPlayerInitializedChanged;
        }

       

        

        private void HandleOnPlayerInitializedChanged(PlayerInfo player)
        {
            // Setting up team controller
            if (player.IsLocal && !player.Bot)
            {
                teamController = player.Home ? TeamController.HomeTeam : TeamController.AwayTeam;
            }
        }
        

    }
}
#endif