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

        float cursorMax = 2.5f;

        TeamController teamController;

        bool pressed = false;

        PlayerController lastPlayer;

        float chargingSpeed = 1;

        float charge = 0;



        
        // Start is called before the first frame update
        void Start()
        {
            var marker = GetComponentInParent<PlayerMarker>();
            teamController = marker.PlayerInfo.Home ? TeamController.HomeTeam : TeamController.AwayTeam;    
        }

        // Update is called once per frame
        void Update()
        {
            
        }

        void LateUpdate()
        {
            if (!teamController || !teamController.IsSpawned) return;

            ResetTriggers();

            if (!teamController.SelectedPlayer || teamController.SelectedPlayer.GetState() != (byte)PlayerState.Normal || !teamController.SelectedPlayer.HasBall)
            {
                if (!animator.IsInTransition(0) && !animator.GetCurrentAnimatorStateInfo(0).IsName("Hide"))
                    animator.SetTrigger("Hide");

                pressed = false;

                return;
            }

            if (lastPlayer != teamController.SelectedPlayer)
            {
                pressed = false;
                animator.Rebind();
            }
                

            lastPlayer = teamController.SelectedPlayer;
           
            InputHandler ih = (InputHandler)teamController.SelectedPlayer.GetInputHandler();





            if (ih.Button2)
            {
                if (!pressed)
                {
                    charge = 0;
                    Debug.Log($"TEST - Pressed");
                    pressed = true;
                    if (!animator.IsInTransition(0) && !animator.GetCurrentAnimatorStateInfo(0).IsName("Show"))
                        animator.SetTrigger("Show");
                    //root.SetActive(true);

                }

                charge += chargingSpeed * Time.deltaTime;
                charge = Mathf.Clamp01(charge);
                var cursorPos = cursor.transform.localPosition;
                cursorPos.x = Mathf.Lerp(0, cursorMax, charge);
                cursor.transform.localPosition = cursorPos;
                if (charge == 1)
                {
                    Debug.Log("TEST - Charge is 1");
                    if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Glow-G") && !animator.GetCurrentAnimatorStateInfo(0).IsName("Glow-R") && !animator.GetCurrentAnimatorStateInfo(0).IsName("Glow-Y"))
                    {
                        Debug.Log("TEST - Forcing bad timing");
                        var timing = ShotTiming.Bad;
                        animator.SetInteger("Type", (int)timing);
                        animator.SetTrigger("Glow");
                    }
                }
                
            }
            else
            {
                if (pressed)
                {
                    Debug.Log($"TEST - Released");
                    pressed = false;
                    if (!animator.IsInTransition(0))
                    {
                        if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Glow-G") && !animator.GetCurrentAnimatorStateInfo(0).IsName("Glow-R") && !animator.GetCurrentAnimatorStateInfo(0).IsName("Glow-Y"))
                        {
                            var timing = InputTimingUtility.GetShotTimingByCharge(teamController.SelectedPlayer.Charge);
                            animator.SetInteger("Type", (int)timing);
                            animator.SetTrigger("Glow");
                        }
                    }
                }


            }
        }

        void ResetTriggers()
        {
            if (animator.GetCurrentAnimatorStateInfo(0).IsName("Glow-G") || animator.GetCurrentAnimatorStateInfo(0).IsName("Glow-R") || animator.GetCurrentAnimatorStateInfo(0).IsName("Glow-Y"))
                animator.ResetTrigger("Glow");
            else if (animator.GetCurrentAnimatorStateInfo(0).IsName("Show"))
                animator.ResetTrigger("Show");
            else if (animator.GetCurrentAnimatorStateInfo(0).IsName("Hide"))
                animator.ResetTrigger("Hide");
            
        }

    }
}
#endif