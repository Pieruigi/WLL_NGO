using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using WLL_NGO.Netcode;

namespace WLL_NGO
{
    public class PowerUpManager : SingletonNetwork<PowerUpManager>
    {
        float spawnRate = -1;
        float spawnElapsed = 0;

        
        TeamController lastScorer = null;

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            if (!IsSpawned || !IsServer) return;

            if (!MatchController.Instance || !MatchController.Instance.IsSpawned) return;

            if (MatchController.Instance.MatchState != MatchState.Playing) return;

            
        }

        void OnEnable()
        {
            MatchController.OnStateChanged += HandleOnMatchStateChanged;
            NetController.OnGoalScored += HandleOnGoalScored;
        }

        void OnDisable()
        {
            MatchController.OnStateChanged -= HandleOnMatchStateChanged;
            NetController.OnGoalScored -= HandleOnGoalScored;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            
        }

        private void HandleOnMatchStateChanged(int oldState, int newState)
        {
            if (!IsSpawned || !IsServer) return;

            switch (newState)
            {
                case (int)MatchState.Playing:
                    if (lastScorer)
                    {
                        TeamController spawnTeam = lastScorer.Home ? TeamController.AwayTeam : TeamController.HomeTeam;
                        lastScorer = null;
                        SpawnPackage(spawnTeam);
                    }
                    break;
                case (int)MatchState.KickOff:
                    break;
                
            }
        }

        private void HandleOnGoalScored(TeamController scorer)
        {
            
        }

        void SpawnPackage(TeamController team)
        {
            Debug.Log($"Spawn package for team {team.gameObject.name}");
        }

        public void Initialize(float spawnRate)
        {
            this.spawnRate = spawnRate;
        }
    }
    
}
