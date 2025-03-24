using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Unity.Netcode;
using UnityEngine;
using WLL_NGO.Netcode;

namespace WLL_NGO.Netcode
{
    public class PowerUpManager : SingletonNetwork<PowerUpManager>
    {
        [SerializeField]
        GameObject sportBagPrefab;
       


        TeamController lastScorer = null;


        NetworkList<byte> homeTeamPowerUps = new NetworkList<byte>();
        NetworkList<byte> awayTeamPowerUps = new NetworkList<byte>();

        List<PowerUpType> allowedPowerUps = new List<PowerUpType>();

        float spawnRate = -1;
        float spawnElapsed = 0;

        // Start is called before the first frame update
        protected override void Awake()
        {
            allowedPowerUps = new List<PowerUpType>(new PowerUpType[] { PowerUpType.ExplosiveCat, PowerUpType.Bazooka, PowerUpType.Pepper, PowerUpType.Shield });
        }

        // Update is called once per frame
        void Update()
        {
            if (!IsSpawned || !IsServer) return;

            if (!MatchController.Instance || !MatchController.Instance.IsSpawned) return;

            if (MatchController.Instance.MatchState != MatchState.Playing) return;

            // Update time
            if (spawnRate > 0)
            {
                spawnElapsed += Time.deltaTime;
                if (spawnElapsed > spawnRate)
                {
                    spawnElapsed -= spawnRate;
                    SpawnRandomPackage();
                }
            }
        }


        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer)
            {
                MatchController.OnStateChanged += HandleOnMatchStateChanged;
                NetController.OnGoalScored += HandleOnGoalScored;
            }
         
            homeTeamPowerUps.OnListChanged += HandleOnHomeTeamPowerUpListChanged;
            awayTeamPowerUps.OnListChanged += HandleOnAwayTeamPowerUpListChanged;
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            if (IsServer)
            {
                MatchController.OnStateChanged -= HandleOnMatchStateChanged;
                NetController.OnGoalScored -= HandleOnGoalScored;    
            }
            
            homeTeamPowerUps.OnListChanged -= HandleOnHomeTeamPowerUpListChanged;
            awayTeamPowerUps.OnListChanged -= HandleOnAwayTeamPowerUpListChanged;
        }

        private void HandleOnHomeTeamPowerUpListChanged(NetworkListEvent<byte> changeEvent)
        {
            Debug.Log($"TEST - Home team power up list changed, changeEvent.index:{changeEvent.Index}, changeEvent.Value:{changeEvent.Value}");
        }

        private void HandleOnAwayTeamPowerUpListChanged(NetworkListEvent<byte> changeEvent)
        {
            
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
            lastScorer = scorer;
        }

        void SpawnRandomPackage()
        {
            if (!IsServer) return;
            Debug.Log("TEST - Spawning a random package");

            // Spawn sport bag
            var sb = Instantiate(sportBagPrefab);
            sb.GetComponent<SportBag>().Initialize(SportBagType.Local, allowedPowerUps[UnityEngine.Random.Range(0, allowedPowerUps.Count)]);
            sb.GetComponent<NetworkObject>().Spawn();
            // if (homeTeamPowerUps.Count == 0)
            //     homeTeamPowerUps.Add((byte)PowerUpType.Bazooka);
            // else if (homeTeamPowerUps.Count == 1)
            //     homeTeamPowerUps.Add((byte)PowerUpType.Shield);
        }

        void SpawnPackage(TeamController team)
        {
            if (!IsServer) return;
            Debug.Log($"TEST - Spawn package for team {team.gameObject.name}");
        }

        public void Initialize(float spawnRate)
        {
            this.spawnRate = spawnRate;
        }
    }
    
}
