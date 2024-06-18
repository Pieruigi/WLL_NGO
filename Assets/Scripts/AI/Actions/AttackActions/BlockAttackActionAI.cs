using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Services.Matchmaker.Models;
using UnityEngine;

namespace WLL_NGO.AI
{
    public class BlockAttackActionAI : TeamActionAI
    {
        int[] dfBlocks = new int[] { 59, 38, 52 };
        //int mdBlo

        PlayerActionAI dfAction;
        int dfTargetId = -1;

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
                dfTargetId = 0;
                dfAction = (PlayerActionAI)ActionAI.CreateAction<ReachDestinationActionAI>(player, this, false, ActionUpdateFunction.Update, null, ()=> { return CheckConditions(player); });
                
                dfAction.OnActionCompleted += HandleOnActionCompleted;
                dfAction.OnActionInterrupted += HandleOnActionInterrupted;
                dfAction.Initialize(new object[] { FieldGrid.Instance.GetRandomPositionInsideBlock(dfBlocks[dfTargetId]) });
            }
        }

        private void HandleOnActionInterrupted(ActionAI action)
        {
            
        }

        private void HandleOnActionCompleted(ActionAI action, bool succeeded)
        {
            
            dfTargetId++;
            if (dfTargetId >= dfBlocks.Length)
                dfTargetId = 0;
            PlayerAI player = ((PlayerActionAI)action).PlayerAI;
            dfAction = (PlayerActionAI)ActionAI.CreateAction<ReachDestinationActionAI>(player, this, false, ActionUpdateFunction.Update, null, () => { return CheckConditions(player); });
            dfAction.OnActionCompleted += HandleOnActionCompleted;
            dfAction.OnActionInterrupted += HandleOnActionInterrupted;
            dfAction.Initialize(new object[] { FieldGrid.Instance.GetRandomPositionInsideBlock(dfBlocks[dfTargetId]) });
        }

        bool CheckConditions(PlayerAI player)
        {
            // For example an action may be considered interrupted if the player got stunned ( or even if they are facing an opponent and they need to
            // switch direction )
            return true;
        }
    }

}
