using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WLL_NGO.Netcode;

namespace WLL_NGO.AI
{
    public class DefenceActionAI : TeamActionAI
    {

        protected override void Activate()
        {
            if(WaitingActionAI.EnterConditions(new object[] { TeamAI}))
                CreateAction<WaitingActionAI>(Owner, this);
            else
                CreateAction<PressingActionAI>(Owner, this);
            
        }

        public static bool EnterConditions(object[] parameters)
        {
            TeamAI team = (TeamAI)parameters[0];
            return !team.HasBall();
        }

        protected override bool CheckConditions()
        {
            return EnterConditions(new object[] { TeamAI });
        }


 
    }

}
