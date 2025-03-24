using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using WLL_NGO.AI.Test;
using WLL_NGO.Netcode;

namespace WLL_NGO.AI
{
    public class DefenceActionAI : TeamActionAI
    {

        protected override void Activate()
        {
            if (WaitingCondition())
                CreateAction<WaitingActionAI>(Owner, this, true, conditionFunction: WaitingCondition);
            else
                CreateAction<PressingActionAI>(Owner, this, false, conditionFunction: PressingCondition);
        }


        bool WaitingCondition()
        {
            TeamAI team = TeamAI;
#if TEST_AI
            TestBallController ball = team.BallController;
#else
            BallController ball = BallController.Instance;
#endif
            Debug.Log($"Ball:{ball}");            
            Debug.Log($"Net:{team.NetController}");            
            Vector3 ballDir = ball.Position - team.NetController.transform.position;
            ballDir.y = 0;

            return (ballDir.magnitude > team.WaitingLine && team.WaitingTime > 0);

        }

        bool PressingCondition()
        {
            return !WaitingCondition();
        }


    }



}
