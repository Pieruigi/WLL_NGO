using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace WLL_NGO.AI
{
    public class RootActionAI : TeamActionAI
    {
        ActionAI actionAI;

        protected override void Activate()
        {
            //return;

            // // else if(oppTeam.HasBall())
            // //     CreateAction<DefenceActionAI>(Owner, this, restartOnNoChildren: true, conditionFunction: () => { return oppTeam.HasBall(); });
            // // else
            // //     CreateAction<LooseBallActionAI>(Owner, this, restartOnNoChildren: false, conditionFunction: () => { return !oppTeam.HasBall() && !TeamAI.HasBall(); });


        }

        protected override void Loop()
        {
            base.Loop();

            if (actionAI)
                return;
            
            //TeamAI oppTeam = TeamAI == TeamAI.HomeTeamAI ? TeamAI.AwayTeamAI : TeamAI.HomeTeamAI;
            if (TeamAI.IsAttacking())
                actionAI = CreateAction<AttackActionAI>(Owner, this, restartOnNoChildren: true, conditionFunction: () => { return TeamAI.IsAttacking(); });
            else
                actionAI = CreateAction<DefenceActionAI>(Owner, this, restartOnNoChildren: true, conditionFunction: () => { return TeamAI.IsDefending(); });
            // else if (oppTeam.HasBall())
            //     actionAI = CreateAction<DefenceActionAI>(Owner, this, restartOnNoChildren: true, conditionFunction: () => { return oppTeam.HasBall(); });
            // else
            //     actionAI = CreateAction<LooseBallActionAI>(Owner, this, restartOnNoChildren: false, conditionFunction: () => { return !oppTeam.HasBall() && !TeamAI.HasBall(); });
        }

        

    }

}
