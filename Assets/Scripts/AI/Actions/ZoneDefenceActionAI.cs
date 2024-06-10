using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WLL_NGO.AI
{
    public class ZoneDefenceActionAI : TeamActionAI
    {
        protected override void Activate()
        {
            foreach(PlayerAI player in TeamAI.Players)
            {
                CreateAction<ZoneDefencePlayerActionAI>(player, this, ActionUpdateFunction.Update);
            }
        }

        protected override bool CheckConditions()
        {
            return true;
        }

        
    }

}
