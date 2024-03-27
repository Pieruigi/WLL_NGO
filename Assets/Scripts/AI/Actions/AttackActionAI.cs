using System.Collections;
using System.Collections.Generic;
using Unity.Services.Matchmaker.Models;
using UnityEngine;

namespace WLL_NGO.AI
{
    public class AttackActionAI : TeamActionAI
    {
        
        public static bool EnterConditions(object[] parameters)
        {
            TeamAI team = (TeamAI)parameters[0];
            Debug.Log($"CheckConditions:{team.HasBall()}");
            return team.HasBall();
        }

        protected override bool CheckConditions()
        {
            
            return EnterConditions(new object[] { TeamAI });
        }

        protected override void Activate()
        {
            Debug.Log("Action - Updating...");
        }
                

        
    }

}
