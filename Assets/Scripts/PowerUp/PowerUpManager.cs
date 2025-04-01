using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using WLL_NGO.AI;
using WLL_NGO.Netcode;
using WLL_NGO.Scriptables;

namespace WLL_NGO.Netcode
{
    public class PowerUpManager : SingletonNetwork<PowerUpManager>
    {
        public static UnityAction<TeamController, string> OnPowerUpPushed;
        public static UnityAction<TeamController, string> OnPowerUpPopped;

        public const int MaxPowerUps = 2;

        [SerializeField]
        GameObject sportBagPrefab;



        TeamController lastScorer = null;


        [SerializeField] NetworkList<FixedString32Bytes> homeTeamPowerUps = new NetworkList<FixedString32Bytes>();
        public NetworkList<FixedString32Bytes> HomeTeamPowerUps
        {
            get { return homeTeamPowerUps; }
        }

        [SerializeField] NetworkList<FixedString32Bytes> awayTeamPowerUps = new NetworkList<FixedString32Bytes>();
        public NetworkList<FixedString32Bytes> AwayTeamPowerUps
        {
            get { return awayTeamPowerUps; }
        }

        List<PowerUpAsset> allowedPowerUps = new List<PowerUpAsset>();

        float spawnRate = -1;
        float spawnElapsed = 0;



        // Start is called before the first frame update
        protected override void Awake()
        {
            base.Awake();


            allowedPowerUps = Resources.LoadAll<PowerUpAsset>(PowerUpAsset.ResourceFolder).ToList();
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

#if UNITY_EDITOR
            // TEST - 
            if (Input.GetKeyDown(KeyCode.P))
            {
                UsePowerUp(TeamController.HomeTeam);
            }
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                SpawnRandomPackage();
            }
            if (Input.GetKeyDown(KeyCode.Alpha0))
            {
                PlayerController p = TeamController.HomeTeam.SelectedPlayer;
                //p.SetPlayerStateInfo((byte)PlayerState.BlowingUp, 0, (byte)StunDetail.Front, 0);
                p.GetComponent<Rigidbody>().AddForce(Vector3.up * 5 + Vector3.right * 5, ForceMode.VelocityChange);
                //p.GetComponent<Animator>().SetInteger("Detail", 0);
                //p.GetComponent<Animator>().SetTrigger("BlowUp");
                p.SetBlowUpState(true);

            }
#endif
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

        private void HandleOnHomeTeamPowerUpListChanged(NetworkListEvent<FixedString32Bytes> changeEvent)
        {

            if (changeEvent.Type == NetworkListEvent<FixedString32Bytes>.EventType.Add)
                OnPowerUpPushed?.Invoke(TeamController.HomeTeam, changeEvent.Value.ToString());
            else if (changeEvent.Type == NetworkListEvent<FixedString32Bytes>.EventType.RemoveAt)
                OnPowerUpPopped?.Invoke(TeamController.HomeTeam, changeEvent.Value.ToString());
        }

        private void HandleOnAwayTeamPowerUpListChanged(NetworkListEvent<FixedString32Bytes> changeEvent)
        {

            if (changeEvent.Type == NetworkListEvent<FixedString32Bytes>.EventType.Add)
                OnPowerUpPushed?.Invoke(TeamController.AwayTeam, changeEvent.Value.ToString());
            else if (changeEvent.Type == NetworkListEvent<FixedString32Bytes>.EventType.RemoveAt)
                OnPowerUpPopped?.Invoke(TeamController.AwayTeam, changeEvent.Value.ToString());
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
            // Spawn sport bag
            SportBagType type = SportBagType.Home;
            var sb = Instantiate(sportBagPrefab);
            //sb.GetComponent<SportBag>().Initialize(type, allowedPowerUps[UnityEngine.Random.Range(0, allowedPowerUps.Count)]);
            sb.GetComponent<SportBag>().Initialize(type, allowedPowerUps[1].name);
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

        string Pop(TeamController team)
        {
            if (!IsServer) return "";

            var l = team.Home ? homeTeamPowerUps : awayTeamPowerUps;

            if (l.Count == 0)
                return "";

            string name = l[0].ToString();
            l.RemoveAt(0);

            return name;
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

        public void Push(TeamController team, string powerUpName)
        {
            if (!IsServer) return;

            if (HasReachedMaxPowerUps(team)) return;

            if (team.Home)
                homeTeamPowerUps.Add((FixedString32Bytes)powerUpName);
            else
                awayTeamPowerUps.Add((FixedString32Bytes)powerUpName);


        }



        public PowerUpAsset GetPowerUpAssetByName(string name)
        {
            return allowedPowerUps.Find(p => p.name == name);
        }

        public void UsePowerUp(TeamController team)
        {
            // Pop the first power up
            var powerName = Pop(team);

            // Get the asset
            var asset = GetPowerUpAssetByName(powerName);

            // Spawn power up
            var obj = Instantiate(asset.ControllerPrefab);
            obj.Initialize(team.SelectedPlayer); // Initialize
            obj.Launch();
            //obj.GetComponent<NetworkObject>().Spawn();

        }

        public void DespawnPackage(SportBag package)
        {
            package.GetComponent<NetworkObject>().Despawn(true);
        }
    }

    
}
