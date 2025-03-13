using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace WLL_NGO.AI
{
    public class RootActionAI : TeamActionAI
    {
       

        protected override void Activate()
        {
            return;
            TeamAI oppTeam = TeamAI == TeamAI.HomeTeamAI ? TeamAI.AwayTeamAI : TeamAI.HomeTeamAI;
            if (TeamAI.HasBall())
                CreateAction<AttackActionAI>(Owner, this, restartOnNoChildren: true, conditionFunction: () => { return TeamAI.HasBall(); });
            else if(oppTeam.HasBall())
                CreateAction<DefenceActionAI>(Owner, this, restartOnNoChildren: true, conditionFunction: () => { return oppTeam.HasBall(); });
            else
                CreateAction<LooseBallActionAI>(Owner, this, restartOnNoChildren: false, conditionFunction: () => { return !oppTeam.HasBall() && !TeamAI.HasBall(); });

           
        }

    }

}
