using System.Collections;
using System.Collections.Generic;
using Unity.Services.Matchmaker.Models;
using UnityEngine;
using WLL_NGO.AI.Test;

namespace WLL_NGO.AI
{
    public class WaitingActionAI : TeamActionAI
    {
        public static bool EnterConditions(object[] conditions)
        {
            TeamAI team = (TeamAI)conditions[0];
#if TEST_AI
            TestBallController ball = team.BallController;
#else
            BallController ball = team.BallController;
#endif
            Vector3 ballDir = ball.Position - team.NetController.transform.position;
            ballDir.y = 0;

            return (ballDir.magnitude > team.WaitingLine && team.WaitingTime > 0);
            
        }

        protected override void Activate()
        {
            //throw new System.NotImplementedException();
        }

        protected override bool CheckConditions()
        {
            return EnterConditions(new object[] { TeamAI });
        }
    }

}
