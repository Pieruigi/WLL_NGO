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
            set { targetPlayer = value; }
        }

        public bool IsTeammate(PlayerAI player)
        {
            return TeamAI == player.TeamAI;
        }


        private void Awake()
        {
#if !TEST_AI
            playerController = GetComponent<PlayerController>();
            role = playerController.PlayerRole;
#endif
        }
    }

}
