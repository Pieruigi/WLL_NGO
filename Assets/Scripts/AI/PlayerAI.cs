#define TEST_AI
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WLL_NGO.Netcode;

namespace WLL_NGO.AI
{
    public class PlayerAI : MonoBehaviour
    {
        public TeamAI TeamAI { get; set; }

#if TEST_AI
        [SerializeField]
        private PlayerRole role;

#else
        PlayerController controller;
#endif
        public PlayerRole Role
        {
            get { return role; }
        }

#if TEST_AI
        [SerializeField]
        bool hasBall;
#endif

#if TEST_AI
        public Vector3 Position { get { return transform.position; } }
#endif

        public bool HasBall
        {
            get
            {
#if TEST_AI
                return hasBall;
#else
                return playerController.HasBall;
#endif

            }
        }

        PlayerAI targetPlayer = null;
        public PlayerAI TargetPlayer
        {
            get { return targetPlayer; }
            set 
            { 
                if (doubleGuard)
                {
                    Debug.Log($"AI - savedTarget:{value}");
                    savedTarget = value;
                    return;
                }
                    
                targetPlayer = value; 
            }
        }
        /// <summary>
        ///  Safe code to be sure we won't lose targets ( eventually set by triggers ) when player target can not be changed ( ex in double guard )
        /// </summary>
        PlayerAI savedTarget = null;
                                    


        public bool IsTeammate(PlayerAI player)
        {
            return TeamAI == player.TeamAI;
        }

        float doubleGuardTime = 3f;
        //public float DoubleGuardTime { get { return doubleGuardTime; }  }
        float doubleGuardElapsed;
        bool doubleGuard = false;
        public bool IsDoublingGuard { get { return  doubleGuard; } }

        private void Awake()
        {
#if !TEST_AI
            playerController = GetComponent<PlayerController>();
            role = playerController.PlayerRole;
#endif
        }

        private void Update()
        {
            if (doubleGuard)
            {
                doubleGuardElapsed += Time.deltaTime;
                if(doubleGuardElapsed > doubleGuardTime || !targetPlayer.hasBall)
                    StopDoubleGuard();
            }
        }

        public void StartDoubleGuard(PlayerAI player)
        {
            doubleGuardElapsed = 0;
            savedTarget = null; // You should release this target because is no longer in your zone
            targetPlayer = player;
            doubleGuard = true;
            
        }

        void StopDoubleGuard()
        {
            doubleGuard = false;
            targetPlayer = savedTarget;

        }
    }

}
