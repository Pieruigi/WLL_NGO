using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using WLL_NGO.AI;
using WLL_NGO.Netcode;

namespace WLL_NGO.Netcode
{
    public class PowerUpManager : SingletonNetwork<PowerUpManager>
    {
        public const int MaxPowerUps = 2;

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
            SportBagType type = SportBagType.Home;
            var sb = Instantiate(sportBagPrefab);
            sb.GetComponent<SportBag>().Initialize(type, allowedPowerUps[UnityEngine.Random.Range(0, allowedPowerUps.Count)]);
            sb.transform.position = GetRandomSpawnPoint();
            sb.transform.rotation = Quaternion.identity;
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

        Vector3 GetRandomSpawnPoint()
        {
            var l = GameFieldInfo.GetFieldLength() * 3f / 4f;
            var w = GameFieldInfo.GetFieldWidth() * 3f / 4f;

            var x = 0;
            var y = 0;

            var rx = UnityEngine.Random.Range(x - l / 2f, x + l / 2f);
            var ry = UnityEngine.Random.Range(y - w / 2f, y + w / 2f);

            return new Vector3(rx, 4f, ry);

        }

        public void Initialize(float spawnRate)
        {
            this.spawnRate = spawnRate;
        }

        public int PowerUpCount(TeamController team)
        {
            return team.Home ? homeTeamPowerUps.Count : awayTeamPowerUps.Count;
        }

        public bool HasReachedMaxPowerUps(TeamController team)
        {
            return team.Home ? homeTeamPowerUps.Count >= MaxPowerUps : awayTeamPowerUps.Count >= MaxPowerUps;
        }

        public async Task AddPowerUp(TeamController team, PowerUpType type)
        {
            if (HasReachedMaxPowerUps(team)) return;

            if (team.Home)
                homeTeamPowerUps.Add((byte)type);
            else
                awayTeamPowerUps.Add((byte)type);
        }
    }
    
}
