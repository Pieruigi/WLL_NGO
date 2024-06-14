using System.Collections;
using System.Collections.Generic;
using Unity.Services.Matchmaker.Models;
using UnityEngine;
using WLL_NGO.AI.Test;

namespace WLL_NGO.AI
{
    public class WaitingActionAI : TeamActionAI
    {
        

        protected override void Activate()
        {
            // Simply tell each player to defend their position
            ActionAI action = CreateAction<ZoneDefenceActionAI>(Owner, this, false, ActionUpdateFunction.Update, conditionFunction: () => { return true; });
        }

        

    }

}
