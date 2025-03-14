//#define TEST_AI
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Services.Matchmaker.Models;
using Unity.VisualScripting;
using UnityEngine;
using WLL_NGO.AI.Test;
using WLL_NGO.Netcode;

namespace WLL_NGO.AI
{
    public class PlayerAI : MonoBehaviour
    {
        public TeamAI TeamAI { get; set; }

#if TEST_AI
        [SerializeField]
        private PlayerRole role;
        public PlayerRole Role
        {
            get { return role; }
        }

#else
        PlayerController controller;
        public PlayerController Controller
        {
            get { return controller; }
        }
        public PlayerRole Role
        {
            get { return controller.Role; }
        }
#endif


#if TEST_AI
        [SerializeField]
        bool hasBall;
#endif

#if TEST_AI
        public Vector3 Position { get { return transform.position; } }
#else
        public Vector3 Position { get { return transform.position; } }
#endif

        public bool HasBall
        {
            get
            {
#if TEST_AI
                return hasBall;
#else
                return controller.HasBall;
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

        public float Speed
        {
            get
            {
#if TEST_AI
                return 1.5f;
#else
                return controller.MaxSpeed;
#endif
            }
        }

        public float RotationSpeed
        {
            get
            {
#if TEST_AI
                return 480f;
#else
                return controller.RotationSpeed;
#endif
            }
        }


        float ReactionTime { get; set; } = 0.5f;

        DateTime LastReactionTime;

        public bool IsOwner
        {
            get{ return controller.IsOwner; }
        }

        public bool IsSelected
        {
            get{ return controller.IsSelected(); }
        } 

        private void Awake()
        {
#if !TEST_AI
            controller = GetComponent<PlayerController>();
            //role = playerController.PlayerRole;
#endif

        }

        private void Update()
        {
#if TEST_AI
            if (hasBall)
            {
                TestBallController.Instance.transform.position = transform.position + transform.forward * 1f + Vector3.up * .5f;
            }
#endif
            if (!IsSelected)
            {
                if (Input.GetKey(KeyCode.P))
                    controller.GetInputHandler().SetJoystick(new Vector2(1f, 0f));
                else
                    controller.GetInputHandler().SetJoystick(Vector3.zero);
            }

            if (doubleGuard)
            {
                doubleGuardElapsed += Time.deltaTime;
                if (doubleGuardElapsed > doubleGuardTime || !targetPlayer.HasBall)
                    StopDoubleGuard();
            }


        }

        public bool CanReact()
        {
            return (DateTime.Now - LastReactionTime).TotalSeconds > ReactionTime;
        }

        public void SetLastReactionTimeToNow()
        {
            LastReactionTime = DateTime.Now;
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
