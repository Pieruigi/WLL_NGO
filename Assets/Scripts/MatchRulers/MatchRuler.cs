using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Lumin;
using WLL_NGO.Netcode;


namespace WLL_NGO
{
    public class MatchRuler : SingletonNetwork<MatchRuler>
    {

        NetworkVariable<float> timer = new NetworkVariable<float>(0);
        public float Timer
        {
            get { return timer.Value; }
        }


        bool completed = false;

        bool useGoalLimit = false;
        bool useTimeLimit = false;

        int goalLimit;
        float timeLimit;
        

        // Update is called once per frame
        protected virtual void Update()
        {
            if (IsClient) return; // Server only

            if (completed) return; // Completed

            if (MatchController.Instance.MatchState != MatchState.Playing) return; // Only update time and check for completion 

            if (useTimeLimit)
                timer.Value += Time.deltaTime;

            completed = IsMatchCompleted();
            if (completed)
                MatchController.Instance.SetMatchState(MatchState.End);
        }

        protected virtual void OnEnable()
        {
            var gameMode = GameMode.Classic;

            InitGameMode(gameMode);

            NetController.OnGoalScored += HandleOnGoalScored;
        }

        protected virtual void OnDisable()
        {
            NetController.OnGoalScored -= HandleOnGoalScored;
        }

        protected virtual void HandleOnGoalScored(TeamController scorer)
        {
            // TODO: you can chech here if the DoubleScore super power is active
            scorer.Score += 1;

           
        }

        bool IsMatchCompleted()
        {
            if (useTimeLimit && timer.Value > timeLimit)
            {
                return true;
            }
            if (useGoalLimit && (TeamController.HomeTeam.Score >= goalLimit || TeamController.AwayTeam.Score >= goalLimit))
            {
                return true;
            }
            return false;
        }

        void InitGameMode(GameMode gameMode)
        {
            switch (gameMode)
            {
                case GameMode.Fast:
                    useTimeLimit = true;
                    useGoalLimit = false;
                    timeLimit = 60;
                    break;
                case GameMode.Classic:
                    useTimeLimit = true;
                    useGoalLimit = true;
                    timeLimit = 150;
                    goalLimit = 3;
                    break;
                case GameMode.Powered:
                    useTimeLimit = true;
                    useGoalLimit = false;
                    timeLimit = 150;
                    break;
                
            }
        }
    }
    
}
