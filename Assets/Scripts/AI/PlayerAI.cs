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

        private void Awake()
        {
#if !TEST_AI
            playerController = GetComponent<PlayerController>();
            role = playerController.PlayerRole;
#endif
        }
    }

}
