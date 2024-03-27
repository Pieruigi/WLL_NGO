#define TEST_AI
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.VisualScripting;
using UnityEngine;
using WLL_NGO.AI.Test;
using WLL_NGO.Netcode;

namespace WLL_NGO.AI
{
    public class TeamAI : MonoBehaviour
    {
        [SerializeField] bool home;

        [SerializeField] List<PlayerAI> players;
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


        float updateTime = .5f;
        float timeElapsed = 0;

        private void Awake()
        {
            timeElapsed = updateTime;
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
            if(!rootAction)
            {
                // Create the root action
                rootAction = ActionAI.CreateAction<RootActionAI>(owner: this, previousAction: null);
                
            }
           
        }



        public bool HasBall()
        {
#if TEST_AI
        return hasBall;
#endif
        }
    }

}
