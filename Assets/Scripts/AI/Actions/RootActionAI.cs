using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WLL_NGO.AI
{
    public class RootActionAI : ActionAI
    {
        TeamAI teamAI;

        protected override bool CheckConditions()
        {
            return true;
        }

        protected override void DoUpdate()
        {

            if (teamAI.HasBall())
            {
                //CreateAction(typeof(AttackActionAI));
                //Attack();
            }
            else
            {

                //Defend();
            }
        }

        protected override bool IsCompleted(out bool succeeded)
        {
            throw new System.NotImplementedException();
        }

        public override void Initialize(float updateTime = 0, ActionAI previousAction = null, object[] parameters = null)
        {
            base.Initialize(updateTime, previousAction, parameters);
            teamAI = (TeamAI)parameters[0];
        }
    }

}
