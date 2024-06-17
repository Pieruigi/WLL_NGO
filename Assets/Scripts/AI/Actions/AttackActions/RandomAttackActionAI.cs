using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WLL_NGO.AI
{
    public class RandomAttackActionAI : TeamActionAI
    {

        protected override void Activate()
        {
            base.Activate();

            Debug.Log($"Activate random action ai, players:{TeamAI.Players.Count}");

            
            // Set an action for each player in the team
            foreach (var player in TeamAI.Players)
            {
              
                if (player != TeamAI.Players[1]) // Testing purpose 
                    continue;

                Debug.Log("Creating player action MoveToDirection()");
                ActionAI playerAction = ActionAI.CreateAction<MoveToDirection>(player, this, false, ActionUpdateFunction.Update, null, ()=> { return CheckConditions(player); });
                playerAction.Initialize(new object[] { player.transform.forward });
            }
        }

        bool CheckConditions(PlayerAI player)
        {
            return true;
        }
    }

}
