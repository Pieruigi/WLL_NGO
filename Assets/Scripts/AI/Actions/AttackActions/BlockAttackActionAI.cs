#if !BLOCK_OLD
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
        int maxAttackingPlayers = 2; // Depending on formation and strategy

        List<PlayerActionAI> playerActions = new List<PlayerActionAI>();

        int actualFormation;

        float bottom, left, top, right;

        float areaLength;


        protected override void Activate()
        {
            base.Activate();

            Debug.Log($"Activate random action ai, players:{TeamAI.Players.Count}");

            // Init dictionary
            actualFormation = TeamAI.Formation;
            // Depending on the goalkeeper we can switch to a kind of 2-2 formation

            // TODO: only for testing purpose
            actualFormation = 1; // 1-2

            // Field borders
            
        }

        protected override void Loop()
        {
            UpdatePositions(actualFormation);
        }

        void UpdatePositions(int actualFormation)
        {
            switch (actualFormation)
            {
                case 0:
                    //UpdateFormationZero();
                    break;
            }
        }

        private void HandleOnActionInterrupted(ActionAI action)
        {
            playerActions.Remove(action as PlayerActionAI);
        }

        private void HandleOnActionCompleted(ActionAI action, bool succeeded)
        {
            playerActions.Remove(action as PlayerActionAI);
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

                // We can still check if the player is moving back to defend
                if (!lastPlayers.Contains(player))
                {
                    if (playerActions.Exists(a => a.GetType() == typeof(ReachDestinationActionAI) && (a as ReachDestinationActionAI).PlayerAI == player &&
                                              (!ballBlock.IsAttack(TeamAI.Home) && !FieldGrid.Instance.GetTheClosestBlock((a.Parameters as ReachDestinationActionParams).Destination).IsAhead(ballBlock, TeamAI.Home) ||
                                                ballBlock.IsAttack(TeamAI.Home) && FieldGrid.Instance.GetTheClosestBlock((a.Parameters as ReachDestinationActionParams).Destination).IsBehind(ballBlock, TeamAI.Home))))
                    {
                        lastPlayers.Add(player); 
                    }
                }
            }

            return lastPlayers;
        }

        /// <summary>
        /// Cover both left and right sides
        /// </summary>
        /// <param name="defendingPlayers"></param>
        /// <param name="ballBlock"></param>
        void BackToSideBlocks(List<PlayerAI> defendingPlayers, FieldBlock ballBlock)
        {
            Debug.Log($"TEST - Player on block {FieldGrid.Instance.GetTheClosestBlock(defendingPlayers[0].Position).gameObject.name}");

            PlayerAI leftDefender = defendingPlayers.Find(p=>FieldGrid.Instance.GetTheClosestBlock(p.Position).IsLeftSide(TeamAI.Home));
            PlayerAI rightDefender = defendingPlayers.Find(p=>FieldGrid.Instance.GetTheClosestBlock(p.Position).IsRightSide(TeamAI.Home));

            if (!leftDefender || !rightDefender)
            {
                foreach (var player in defendingPlayers)
                {
                    ReachDestinationActionAI action = playerActions.Find(a => a.GetType() == typeof(ReachDestinationActionAI) && (a as ReachDestinationActionAI).PlayerAI == player) as ReachDestinationActionAI;
                    if (action)
                    {
                        if ((TeamAI.Home && (action.Parameters as ReachDestinationActionParams).Destination.z > 0) ||
                            (!TeamAI.Home && (action.Parameters as ReachDestinationActionParams).Destination.z < 0))
                            leftDefender = player;

                        if ((TeamAI.Home && (action.Parameters as ReachDestinationActionParams).Destination.z < 0) ||
                            (!TeamAI.Home && (action.Parameters as ReachDestinationActionParams).Destination.z > 0))
                            rightDefender = player;
                    }
                }
            }

            Debug.Log($"TEST - Left defender is {leftDefender?.gameObject.name}");
            Debug.Log($"TEST - Right defender is {rightDefender?.gameObject.name}");

            int sector = 0; // Defence
            if (ballBlock.IsAttack(TeamAI.Home))
                sector = 1; // Middle field
            Debug.Log($"TEST - Sector to defend: {(sector == 0 ? "defence" : "middle")}");

            return;

            // Check is there are some other players already moving back
            Dictionary<PlayerAI, PlayerActionAI> alreadyMovingBack = new Dictionary<PlayerAI, PlayerActionAI>();
            // foreach (var key in actions.Keys)
            // {
            //     PlayerActionAI action = actions[key].Find(a => a.GetType() == typeof(ReachDestinationActionAI) &&
            //                                              (!ballBlock.IsAttack(TeamAI.Home) && !FieldGrid.Instance.GetTheClosestBlock((a.Parameters as ReachDestinationActionParams).Destination).IsAhead(ballBlock, TeamAI.Home) ||
            //                                                ballBlock.IsAttack(TeamAI.Home) && FieldGrid.Instance.GetTheClosestBlock((a.Parameters as ReachDestinationActionParams).Destination).IsBehind(ballBlock, TeamAI.Home)));

            //     if (action)
            //     {
            //         alreadyMovingBack.Add(key, action);
            //     }
            // }

            //if (alreadyMovingBack.Count < neededPlayers)
            {
                var keys = alreadyMovingBack.Keys.ToArray();
                for (int i = 0; i < 2; i++)
                {
                    bool found = false;

                    for (int k = 0; i < keys.Length && !found; k++)
                    {
                        var key = keys[k];

                        if ((i == 0 && FieldGrid.Instance.GetTheClosestBlock((alreadyMovingBack[key].Parameters as ReachDestinationActionParams).Destination).IsLeftSide(TeamAI.Home)) ||
                            (i == 1 && FieldGrid.Instance.GetTheClosestBlock((alreadyMovingBack[key].Parameters as ReachDestinationActionParams).Destination).IsRightSide(TeamAI.Home)))
                            found = true; // Someone is already moving back to this sector
                    }


                    if (!found) // No player is moving back to this sector
                    {
                        // Get a random destination block 
                        List<FieldBlock> all;
                        if (ballBlock.IsAttack(TeamAI.Home))
                        {
                            all = i == 0 ? FieldGrid.Instance.GetLeftMiddleFieldBlockAll(TeamAI) : FieldGrid.Instance.GetRightMiddleFieldBlockAll(TeamAI);
                        }
                        else
                        {
                            if (ballBlock.IsMiddleField())
                            {
                                all = FieldGrid.Instance.GetLeftMiddleFieldBlockAll(TeamAI);
                            }
                            else
                            {
                                all = i == 0 ? FieldGrid.Instance.GetLeftDefenceBlockAll(TeamAI) : FieldGrid.Instance.GetRightDefenceBlockAll(TeamAI);
                            }
                        }
                        var destination = all[UnityEngine.Random.Range(0, all.Count)].GetRandomPosition();

                        // Get the closest player
                        var available = GetTheClosestAvailablePlayerToMove(destination);
                        if (available)
                        {
                            // Create action
                            var moveAction = (PlayerActionAI)ActionAI.CreateAction<ReachDestinationActionAI>(available, this, false, ActionUpdateFunction.Update, new ReachDestinationActionParams() { Destination = destination });
                            moveAction.OnActionCompleted += HandleOnActionCompleted;
                            moveAction.OnActionInterrupted += HandleOnActionInterrupted;

                            // Add action to dictionary
                            playerActions.Add(moveAction);
                            //moveAction.Initialize(new object[] { FieldGrid.Instance.GetRandomPositionInsideBlock(dfBlocks[dfTargetId]) });
                        }
                        


                    }

                }
                // Left 





            }

        }

        PlayerAI GetTheClosestAvailablePlayerToMove(Vector3 destination)
        {
            float minDist = 0;
            PlayerAI closest = null;
            foreach (var player in TeamAI.Players)
            {
                //TODO: if the player is not available to move then continue
                if (player.HasBall) // TODO: we should check for selected player here
                    continue;

                var dist = Vector3.Distance(destination, player.Position);
                if (!closest || minDist > dist)
                {
                    closest = player;
                    minDist = dist;
                }
            }

            return closest;
        }



    }

}

#else
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

        List<PlayerActionAI> playerActions = new List<PlayerActionAI>();


        protected override void Activate()
        {
            base.Activate();

            Debug.Log($"Activate random action ai, players:{TeamAI.Players.Count}");

            // Init dictionary
           
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
                        BackToSideBlocks(lastDefendingPlayers, ballBlock); // We need to defend both sides
                        break;
                    case 1:
                        //BackToCenterBlock(); // We only need to defence the center
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
            playerActions.Remove(action as PlayerActionAI);
        }

        private void HandleOnActionCompleted(ActionAI action, bool succeeded)
        {
            playerActions.Remove(action as PlayerActionAI);
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

                // We can still check if the player is moving back to defend
                if (!lastPlayers.Contains(player))
                {
                    if (playerActions.Exists(a => a.GetType() == typeof(ReachDestinationActionAI) && (a as ReachDestinationActionAI).PlayerAI == player &&
                                              (!ballBlock.IsAttack(TeamAI.Home) && !FieldGrid.Instance.GetTheClosestBlock((a.Parameters as ReachDestinationActionParams).Destination).IsAhead(ballBlock, TeamAI.Home) ||
                                                ballBlock.IsAttack(TeamAI.Home) && FieldGrid.Instance.GetTheClosestBlock((a.Parameters as ReachDestinationActionParams).Destination).IsBehind(ballBlock, TeamAI.Home))))
                    {
                        lastPlayers.Add(player); 
                    }
                }
            }

            return lastPlayers;
        }

        /// <summary>
        /// Cover both left and right sides
        /// </summary>
        /// <param name="defendingPlayers"></param>
        /// <param name="ballBlock"></param>
        void BackToSideBlocks(List<PlayerAI> defendingPlayers, FieldBlock ballBlock)
        {
            Debug.Log($"TEST - Player on block {FieldGrid.Instance.GetTheClosestBlock(defendingPlayers[0].Position).gameObject.name}");

            PlayerAI leftDefender = defendingPlayers.Find(p=>FieldGrid.Instance.GetTheClosestBlock(p.Position).IsLeftSide(TeamAI.Home));
            PlayerAI rightDefender = defendingPlayers.Find(p=>FieldGrid.Instance.GetTheClosestBlock(p.Position).IsRightSide(TeamAI.Home));

            if (!leftDefender || !rightDefender)
            {
                foreach (var player in defendingPlayers)
                {
                    ReachDestinationActionAI action = playerActions.Find(a => a.GetType() == typeof(ReachDestinationActionAI) && (a as ReachDestinationActionAI).PlayerAI == player) as ReachDestinationActionAI;
                    if (action)
                    {
                        if ((TeamAI.Home && (action.Parameters as ReachDestinationActionParams).Destination.z > 0) ||
                            (!TeamAI.Home && (action.Parameters as ReachDestinationActionParams).Destination.z < 0))
                            leftDefender = player;

                        if ((TeamAI.Home && (action.Parameters as ReachDestinationActionParams).Destination.z < 0) ||
                            (!TeamAI.Home && (action.Parameters as ReachDestinationActionParams).Destination.z > 0))
                            rightDefender = player;
                    }
                }
            }

            Debug.Log($"TEST - Left defender is {leftDefender?.gameObject.name}");
            Debug.Log($"TEST - Right defender is {rightDefender?.gameObject.name}");

            int sector = 0; // Defence
            if (ballBlock.IsAttack(TeamAI.Home))
                sector = 1; // Middle field
            Debug.Log($"TEST - Sector to defend: {(sector == 0 ? "defence" : "middle")}");

            return;

            // Check is there are some other players already moving back
            Dictionary<PlayerAI, PlayerActionAI> alreadyMovingBack = new Dictionary<PlayerAI, PlayerActionAI>();
            // foreach (var key in actions.Keys)
            // {
            //     PlayerActionAI action = actions[key].Find(a => a.GetType() == typeof(ReachDestinationActionAI) &&
            //                                              (!ballBlock.IsAttack(TeamAI.Home) && !FieldGrid.Instance.GetTheClosestBlock((a.Parameters as ReachDestinationActionParams).Destination).IsAhead(ballBlock, TeamAI.Home) ||
            //                                                ballBlock.IsAttack(TeamAI.Home) && FieldGrid.Instance.GetTheClosestBlock((a.Parameters as ReachDestinationActionParams).Destination).IsBehind(ballBlock, TeamAI.Home)));

            //     if (action)
            //     {
            //         alreadyMovingBack.Add(key, action);
            //     }
            // }

            //if (alreadyMovingBack.Count < neededPlayers)
            {
                var keys = alreadyMovingBack.Keys.ToArray();
                for (int i = 0; i < 2; i++)
                {
                    bool found = false;

                    for (int k = 0; i < keys.Length && !found; k++)
                    {
                        var key = keys[k];

                        if ((i == 0 && FieldGrid.Instance.GetTheClosestBlock((alreadyMovingBack[key].Parameters as ReachDestinationActionParams).Destination).IsLeftSide(TeamAI.Home)) ||
                            (i == 1 && FieldGrid.Instance.GetTheClosestBlock((alreadyMovingBack[key].Parameters as ReachDestinationActionParams).Destination).IsRightSide(TeamAI.Home)))
                            found = true; // Someone is already moving back to this sector
                    }


                    if (!found) // No player is moving back to this sector
                    {
                        // Get a random destination block 
                        List<FieldBlock> all;
                        if (ballBlock.IsAttack(TeamAI.Home))
                        {
                            all = i == 0 ? FieldGrid.Instance.GetLeftMiddleFieldBlockAll(TeamAI) : FieldGrid.Instance.GetRightMiddleFieldBlockAll(TeamAI);
                        }
                        else
                        {
                            if (ballBlock.IsMiddleField())
                            {
                                all = FieldGrid.Instance.GetLeftMiddleFieldBlockAll(TeamAI);
                            }
                            else
                            {
                                all = i == 0 ? FieldGrid.Instance.GetLeftDefenceBlockAll(TeamAI) : FieldGrid.Instance.GetRightDefenceBlockAll(TeamAI);
                            }
                        }
                        var destination = all[UnityEngine.Random.Range(0, all.Count)].GetRandomPosition();

                        // Get the closest player
                        var available = GetTheClosestAvailablePlayerToMove(destination);
                        if (available)
                        {
                            // Create action
                            var moveAction = (PlayerActionAI)ActionAI.CreateAction<ReachDestinationActionAI>(available, this, false, ActionUpdateFunction.Update, new ReachDestinationActionParams() { Destination = destination });
                            moveAction.OnActionCompleted += HandleOnActionCompleted;
                            moveAction.OnActionInterrupted += HandleOnActionInterrupted;

                            // Add action to dictionary
                            playerActions.Add(moveAction);
                            //moveAction.Initialize(new object[] { FieldGrid.Instance.GetRandomPositionInsideBlock(dfBlocks[dfTargetId]) });
                        }
                        


                    }

                }
                // Left 





            }

        }

        PlayerAI GetTheClosestAvailablePlayerToMove(Vector3 destination)
        {
            float minDist = 0;
            PlayerAI closest = null;
            foreach (var player in TeamAI.Players)
            {
                //TODO: if the player is not available to move then continue
                if (player.HasBall) // TODO: we should check for selected player here
                    continue;

                var dist = Vector3.Distance(destination, player.Position);
                if (!closest || minDist > dist)
                {
                    closest = player;
                    minDist = dist;
                }
            }

            return closest;
        }



    }

}
#endif