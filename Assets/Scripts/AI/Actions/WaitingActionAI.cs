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
            // Tell each player to defend the position

            
            // That's just a test, we need to create a DefendZoneActionAI instead
            List<PlayerAI> players = new List<PlayerAI>(TeamAI.Players);
            ActionAI action = CreateAction<ReachDestinationActionAI>(players[1], this, ActionUpdateFunction.Update, new object[] { TeamAI.BallController.Position });
            //action = CreateAction<ReachDestinationActionAI>(players[2], this, ActionUpdateFunction.Update, new object[] { TeamAI.BallController.Position - players[2].transform.forward*.8f });
        }

        protected override bool CheckConditions()
        {
            return EnterConditions(new object[] { TeamAI });
        }

        protected override void HandleOnChildActionCompleted(ActionAI childAction, bool succeeded)
        {
            childAction.Initialize(new object[] { TeamAI.BallController.Position });
            childAction.Restart();

        }

    }

}
