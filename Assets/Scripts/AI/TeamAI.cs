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
    public class TeamAI/*<T>*/ : MonoBehaviour /*where T : ActionAI*/
    {
        [SerializeField] bool home;

        [SerializeField] List<PlayerAI> players;
        [SerializeField] float waitingLine;
        [SerializeField] float waitingTime;

#if TEST_AI
        [SerializeField]TestBallController ball;
        [SerializeField]int homeScore;
        [SerializeField]int awayScore;
#else
        BallController ball;
#endif

#if TEST_AI
        [SerializeField] bool hasBall;
#endif

        [SerializeField] List<ActionAI> actions = new List<ActionAI>();


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
            if(actions.Count == 0)
            {
                // Create the root action
                ActionAI action = ActionAI.CreateAction<RootActionAI>(updateTime: 0, previousAction: null, parameters: new object[] { this });
                action.name = $"{gameObject.name}_{action.name}";
                actions.Add(action);
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
