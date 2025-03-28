//#define TEST_AI
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using WLL_NGO.AI.Test;
using WLL_NGO.Netcode;

namespace WLL_NGO.AI
{


    public class TeamAI : MonoBehaviour
    {
        public static TeamAI HomeTeamAI { get; private set; }
        public static TeamAI AwayTeamAI { get; private set; }

        [SerializeField] bool home;
        public bool Home
        {
            get { return home; }
        }

        [SerializeField] List<PlayerAI> players = new List<PlayerAI>();
        public IList<PlayerAI> Players
        {
            get { return players.AsReadOnly(); }
        }
        [SerializeField] float waitingLine;
        public float WaitingLine
        {
            get { return waitingLine; }
        }

        [SerializeField] float waitingTime;
        public float WaitingTime
        {
            get { return waitingTime; }
        }

        [SerializeField] float maxDefensiveDistance = 4;


        //[SerializeField]
        //List<ZoneTrigger> defenceZoneList;
        //int formationId = 0;
        //public int FormationId { get { return formationId; } }

        [SerializeField] List<ZoneTrigger> defenceZoneTriggers;
        public List<ZoneTrigger> WaitingZoneTriggers { get { return defenceZoneTriggers; } }

        [SerializeField] List<ZoneTrigger> pressingTriggers;
        public List<ZoneTrigger> PressingZoneTriggers { get { return pressingTriggers; } }

#if TEST_AI
        [SerializeField]TestBallController ball;
        public TestBallController BallController { get { return ball; } }
        [SerializeField]int homeScore;
        [SerializeField]int awayScore;
        [SerializeField] TestNetController netController;
        public TestNetController NetController { get { return netController;} }


#else
        BallController ball;
        public BallController BallController { get { return ball; } }
        [SerializeField] NetController netController;
        public NetController NetController { get { return netController; } }
#endif

#if TEST_AI
        [SerializeField] bool hasBall;
#endif

        [SerializeField] ActionAI rootAction;

        [SerializeField] bool iaDisabled = false;


        float updateTime = .5f;
        float timeElapsed = 0;

        /// <summary>
        /// 0: 2-1
        /// 1: 1-2
        /// </summary>
        int formation = 0;
        public int Formation
        {
            get { return formation; }
        }

        public TeamController TeamController { get; private set; }

        private void Awake()
        {
            

            if (home)
                HomeTeamAI = this;
            else
                AwayTeamAI = this;

            timeElapsed = updateTime;

#if TEST_AI
            // In netcode we need to fo this for each player after spawning
            foreach (PlayerAI player in players)
            {
                player.TeamAI = this;
            }

#endif


        }


        void Start()
        {
            if (!NetworkManager.Singleton.IsServer) return;
           

#if TEST_AI
            netController = FindObjectsOfType<TestNetController>().ToList().Find(n => n.Home == home);           

            CreateRootAction();
#else
            netController = home ? NetController.HomeNetController : NetController.AwayNetController;
#endif

        }

        private void FixedUpdate()
        {
            if (!NetworkManager.Singleton.IsServer) return;

#if !TEST_AI
            if (MatchController.Instance.MatchState != MatchState.Playing)
                return;
#endif
            timeElapsed -= Time.fixedDeltaTime;

            if (timeElapsed < 0)
            {
                timeElapsed = updateTime;
                DoUpdate();
            }

        }

#if !TEST_AI
        private void OnEnable()
        {
            
            BallController.OnBallSpawned += HandleOnBallSpawned;
            PlayerController.OnSpawned += HandleOnPlayerControllerSpawned;
            TeamController.OnTeamControllerSpawned += HandleOnTeamControllerSpawned;
            MatchController.OnStateChanged += HandleOnMatchStateChanged;
        }

        private void OnDisable()
        {
            
            BallController.OnBallSpawned -= HandleOnBallSpawned;
            PlayerController.OnSpawned -= HandleOnPlayerControllerSpawned;
            TeamController.OnTeamControllerSpawned -= HandleOnTeamControllerSpawned;
            MatchController.OnStateChanged -= HandleOnMatchStateChanged;
        }

        private void HandleOnMatchStateChanged(int oldState, int newState)
        {
            if (!NetworkManager.Singleton.IsServer) return;

            switch (newState)
            {
                case (int)MatchState.End:
                case (int)MatchState.Goal:
                    DestroyRootAction();
                    break;
                case (int)MatchState.Playing:
                    CreateRootAction();
                    break;
            }
        }

        private void HandleOnTeamControllerSpawned(TeamController teamController)
        {
            if (!NetworkManager.Singleton.IsServer) return;

            if (home != teamController.Home)
                return;

            TeamController = teamController;
        }

        private void HandleOnPlayerControllerSpawned(PlayerController playerController)
        {
            if (!NetworkManager.Singleton.IsServer) return;

            // Team check
            if (home != playerController.PlayerInfo.Home)
                return;

            // Add the new player
            PlayerAI player = playerController.GetComponent<PlayerAI>();
            player.TeamAI = this;
            AddPlayer(player);
        }

        private void HandleOnBallSpawned()
        {
            if (!NetworkManager.Singleton.IsServer) return;
            
            ball = BallController.Instance;

        }
#endif

        void DoUpdate()
        {
            // if (iaDisabled) return;

            // if (!rootAction)
            // {
            //     // Create the root action
            //     rootAction = ActionAI.CreateAction<RootActionAI>(owner: this, previousAction: null, restartOnNoChildren: true);

            // }

        }

        void AddPlayer(PlayerAI player)
        {
            if (!players.Contains(player))
                players.Add(player);
        }

        void CreateRootAction()
        {
            if(!rootAction)
            {
                rootAction = ActionAI.CreateAction<RootActionAI>(owner: this, previousAction: null, restartOnNoChildren: true);
            }
        }


        public void DestroyRootAction()
        {
            if (rootAction)
            {
                Destroy(rootAction.gameObject);
                rootAction = null;
            }
        }

        public bool HasBall()
        {
#if TEST_AI
            return players.Exists(p => p.HasBall);

#else
            return (players.Exists(p => p.HasBall) || players.Exists(p => p.IsReceivingPassage));
#endif
        }

        public bool IsAttacking()
        {
            if(HasBall())
                return true;

            TeamAI opponent = home ? AwayTeamAI : HomeTeamAI;
            if(opponent.HasBall())
                return false;
#if TEST_AI
            return (TestBallController.Instance.Position.x > 0 && home) || (TestBallController.Instance.Position.x < 0 && !home);
#else
            return (BallController.Instance.Position.x > 0 && home) || (BallController.Instance.Position.x < 0 && !home);
#endif
        }

        public bool IsDefending()
        {
            return !IsAttacking();
        }        

        //public PlayerAI GetZoneDefender(ZoneTrigger zone)
        //{
        //    int id = defenceZoneList.IndexOf(zone);
        //    return players[id + 1];
        //}

        public bool IsTeammate(PlayerAI player)
        {
            return players.Contains(player);
        }

        /// <summary>
        /// We should return a value depending on the AI level, the defensive line and the target position 
        /// </summary>
        /// <returns></returns>
        public float GetDefensiveDistance()
        {
            return maxDefensiveDistance;
        }

        public void SetDefenceZoneTriggerList(List<ZoneTrigger> zoneTriggers)
        {
            defenceZoneTriggers = zoneTriggers;

        }

        public void SetPressingZoneTriggerList(List<ZoneTrigger> zoneTriggers)
        {
            pressingTriggers = zoneTriggers;
        }
    }
}
