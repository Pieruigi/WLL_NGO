using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Services.Matchmaker.Models;
using Unity.VisualScripting;
using UnityEngine;
using WLL_NGO.AI.Test;
using WLL_NGO.Netcode;

namespace WLL_NGO.AI
{
    public class BlockAttackActionAI : TeamActionAI
    {
        int[] dfBlocks = new int[] { 59, 38, 52 };
        //int mdBlo

        PlayerActionAI dfAction;
        int dfTargetId = -1;

        int maxAttackingPlayers = 2; // Depending on formation and strategy

        Dictionary<PlayerAI, List<PlayerActionAI>> actions = new Dictionary<PlayerAI, List<PlayerActionAI>>();


        protected override void Activate()
        {
            base.Activate();

            Debug.Log($"Activate random action ai, players:{TeamAI.Players.Count}");

            // Init dictionary
            foreach (var player in TeamAI.Players)
                actions.Add(player, new List<PlayerActionAI>());

            maxAttackingPlayers = TeamAI.Formation == 0 ? 1 : 2;
            // If the team is loosing and the game is almost finished then we can move all the player in attack

            return;

            // Set an action for each player in the team
            // foreach (var player in TeamAI.Players)
            // {

            //     if (player != TeamAI.Players[1]) // Testing purpose 
            //         continue;

            //     Debug.Log("Creating player action MoveToDirection()");
            //     dfTargetId = 0;
            //     dfAction = (PlayerActionAI)ActionAI.CreateAction<ReachDestinationActionAI>(player, this, false, ActionUpdateFunction.Update, null, () => { return CheckConditions(player); });

            //     dfAction.OnActionCompleted += HandleOnActionCompleted;
            //     dfAction.OnActionInterrupted += HandleOnActionInterrupted;
            //     dfAction.Initialize(new object[] { FieldGrid.Instance.GetRandomPositionInsideBlock(dfBlocks[dfTargetId]) });
            // }
        }

        protected override void Loop()
        {
            // Every player who's attacking moves depending on the ball position
#if TEST_AI            
            var ballBlock = FieldGrid.Instance.GetTheClosestBlock(TestBallController.Instance.Position);
#else
            var ballBlock = FieldGrid.Instance.GetTheClosestBlock(BallController.Instance.Position);
#endif
            Debug.Log($"Ball is in {ballBlock.gameObject.name}");

            // First of all we must check if the team is unbalanced (someone should keep a defensive position)
            // Give me the last defending players
            List<PlayerAI> lastDefendingPlayers = GetTheLastDefendingPlayers(ballBlock);
            int count = TeamAI.Players[0].HasBall ? TeamAI.Players.Count - maxAttackingPlayers : TeamAI.Players.Count - maxAttackingPlayers - 1;

            Debug.Log($"TEST - We need {count} players behind, we have {lastDefendingPlayers.Count}");

            if (lastDefendingPlayers.Count < count)
            {
                // We need to move at least one player in defence
                switch (TeamAI.Formation)
                {
                    case 0:
                        BackToSideBlock(count-lastDefendingPlayers.Count, ballBlock);
                        break;
                    case 1:
                        //BackToCenterBlock();
                        break;
                }

            }


            // Loop through each player
            foreach (var player in TeamAI.Players)
            {
                if (!player.CanReact()) // Wait for reaction time
                    continue;

                player.SetLastReactionTimeToNow();


            }
        }

        private void HandleOnActionInterrupted(ActionAI action)
        {

        }

        private void HandleOnActionCompleted(ActionAI action, bool succeeded)
        {

            // dfTargetId++;
            // if (dfTargetId >= dfBlocks.Length)
            //     dfTargetId = 0;
            // PlayerAI player = ((PlayerActionAI)action).PlayerAI;
            // dfAction = (PlayerActionAI)ActionAI.CreateAction<ReachDestinationActionAI>(player, this, false, ActionUpdateFunction.Update, null, () => { return CheckConditions(player); });
            // dfAction.OnActionCompleted += HandleOnActionCompleted;
            // dfAction.OnActionInterrupted += HandleOnActionInterrupted;
            // dfAction.Initialize(new object[] { FieldGrid.Instance.GetRandomPositionInsideBlock(dfBlocks[dfTargetId]) });
        }

        bool CheckConditions(PlayerAI player)
        {
            // For example an action may be considered interrupted if the player got stunned ( or even if they are facing an opponent and they need to
            // switch direction )
            return true;
        }


        List<PlayerAI> GetTheLastDefendingPlayers(FieldBlock ballBlock)
        {
            List<PlayerAI> lastPlayers = new List<PlayerAI>();
            //FieldBlock lastBlock = null;

            for (int i = 0; i < TeamAI.Players.Count; i++)
            {
                var player = TeamAI.Players[i];
                // We consider the goalkeeper only if he's bringing the ball
                if (player.Role == PlayerRole.GK && !player.HasBall)
                    continue;

                var block = FieldGrid.Instance.GetTheClosestBlock(player.Position);

                if (!block.IsAttack(TeamAI.Home) && (block.IsBehind(ballBlock, TeamAI.Home) || block.IsInLine(ballBlock, TeamAI.Home)))
                {
                    lastPlayers.Add(player);
                }
                else // Ball is in attack
                {
                    if (block.IsBehind(ballBlock, TeamAI.Home))
                    {
                        lastPlayers.Add(player);
                    }
                }

            }

            return lastPlayers;
        }

        void BackToSideBlock(int neededPlayers, FieldBlock ballBlock)
        {
            var goingBack = actions.Where(p => p.Value.Exists(a => a.GetType() == typeof(ReachDestinationActionAI) &&
                                                                (!ballBlock.IsAttack(TeamAI.Home) && !FieldGrid.Instance.GetTheClosestBlock((a.Parameters as ReachDestinationActionParams).Destination).IsAhead(ballBlock, TeamAI.Home) ||
                                                                 ballBlock.IsAttack(TeamAI.Home) && FieldGrid.Instance.GetTheClosestBlock((a.Parameters as ReachDestinationActionParams).Destination).IsBehind(ballBlock, TeamAI.Home))));
            
            
        }

        


    }

}
