#define TEST_AI
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using WLL_NGO.AI.Test;
using WLL_NGO.Netcode;

namespace WLL_NGO.AI
{
    

    public class TeamAI : MonoBehaviour
    {
        public static TeamAI HomeTeamAI {  get; private set; }
        public static TeamAI AwayTeamAI { get; private set; }

        [SerializeField] bool home;
        
        [SerializeField] List<PlayerAI> players;
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
        public List<ZoneTrigger> DefenceZoneTriggers { get { return defenceZoneTriggers; } }

        [SerializeField] List<ZoneTrigger> pressingTriggers;
        public List<ZoneTrigger> PressingTriggers { get { return pressingTriggers; } }

#if TEST_AI
        [SerializeField]TestBallController ball;
        public TestBallController BallController { get { return ball; } }
        [SerializeField]int homeScore;
        [SerializeField]int awayScore;
        [SerializeField] TestNetController netController;
        public TestNetController NetController { get { return netController;} }


#else
        BallController ball;
        public TestBallController BallController { get { return ball; } }
        [SerializeField] NetController netController;
        public NetController NetController { get { return netController;} }
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
        int formationId = 0;

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

        

        private void FixedUpdate()
        {
#if !TEST_AI
            if (!MatchController.Instance.IsPlaying())
                return;
#endif
            timeElapsed -= Time.fixedDeltaTime;
            
            if(timeElapsed < 0)
            {
                timeElapsed = updateTime;
                DoUpdate();
            }

        }

#if !TEST_AI
        private void OnEnable()
        {
            BallController.OnBallSpawned += HandleOnBallSpawned;
        }

        private void OnDisable()
        {
            BallController.OnBallSpawned -= HandleOnBallSpawned;
        }

        private void HandleOnBallSpawned()
        {

            ball = BallController.Instance;

    }
#endif

        void DoUpdate()
        {
            if (iaDisabled) return;

            if(!rootAction)
            {
                // Create the root action
                rootAction = ActionAI.CreateAction<RootActionAI>(owner: this, previousAction: null, restartOnNoChildren: true);
                
            }
           
        }

        public bool HasBall()
        {
#if TEST_AI
            return hasBall;
#endif
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

       
    }

}
