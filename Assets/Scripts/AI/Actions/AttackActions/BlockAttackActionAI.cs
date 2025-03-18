using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Services.Matchmaker.Models;
using Unity.VisualScripting;
using UnityEditor.Rendering;
using UnityEngine;
using WLL_NGO.AI.Test;
using WLL_NGO.Netcode;

namespace WLL_NGO.AI
{
    public class BlockAttackActionAI : TeamActionAI
    {
        int maxAttackingPlayers = 2; // Depending on formation and strategy

        
        int actualFormation;

        

        Vector3[] targetPositions;

        List<PlayerActionAI> actions = new List<PlayerActionAI>();


        protected override void Activate()
        {
            base.Activate();

            Debug.Log($"Activate random action ai, players:{TeamAI.Players.Count}");

            // Init dictionary
            actualFormation = TeamAI.Formation;
            // Depending on the goalkeeper we can switch to a kind of 2-2 formation

            // TODO: only for testing purpose
            actualFormation = 1; // 1-2

            targetPositions = new Vector3[TeamAI.Players.Count];

        }

        protected override void Loop()
        {
            ComputeBasePositions();
        }

        void ComputeBasePositions()
        {
            // This action is only available if a team player is bringing the ball.
            int ballOwnerIndex = TeamAI.Players.ToList().FindIndex(p => p.HasBall || p.IsSelected);

            if(ballOwnerIndex < 0)
            {
                return;
            }

            // if (ballOwnerIndex < 0)
            // {
            //     return;
            // }
            Debug.Log("TEST - BallOwnerIndex:" + ballOwnerIndex);

            bool useLerp = false;
            // Compute base positions
            FormationHelper helper = TeamAI.Home ? FormationHelper.HomeFormationHelper : FormationHelper.AwayFormationHelper;
#if TEST_AI
            Vector3 ballPosition = TestBallController.Instance.Position;
#else
            Vector3 ballPosition = BallController.Instance.Position;
#endif
            Vector3[] basePositions = new Vector3[TeamAI.Players.Count];

            // Filter triggers
            Debug.Log("TEST - Current trigger count:" + helper.CurrentTriggers.Count);
            List<FormationHelperTrigger> triggers;
            if(TeamAI.Players[ballOwnerIndex].HasBall)
                triggers = helper.CurrentTriggers.ToList().FindAll(t => t.BallOwnerIndex == ballOwnerIndex);
            else
                triggers = helper.CurrentBallTriggers.ToList().FindAll(t => t.BallOwnerIndex == ballOwnerIndex);

            if (triggers.Count > 0)
            {
                for (int i = 0; i < basePositions.Length; i++)
                {
                    Vector3 offset = Vector3.zero;
                    if (useLerp)
                    {
                        foreach (var trigger in triggers)
                        {
                            Vector3 startPos = /*trigger.BallOwnerIndex < 0 ? ballPosition : */trigger.Positions[trigger.BallOwnerIndex].position;

                            offset += trigger.Positions[i].position - startPos;

                        }

                        offset /= triggers.Count;
                    }
                    else
                    {
                        var trigger = triggers.Last();
                        Vector3 startPos = trigger.Positions[trigger.BallOwnerIndex].position;
                        offset = trigger.Positions[i].position - startPos;
                    }


                    basePositions[i] = ballPosition + offset;


                    targetPositions[i] = Vector3.MoveTowards(targetPositions[i], basePositions[i], 5f * Time.deltaTime);

                }




                // Move each player
                for (int i = 0; i < basePositions.Length; i++)
                {
                    PlayerAI player = TeamAI.Players[i];
                    if (player.Role == PlayerRole.GK || player.HasBall || player.IsSelected)// && !player.HasBall) // Only bot when player owns the ball
                        continue;

                    // Check if an action already exists for this player
                    var moveAction = actions.Find(a => a.PlayerAI == player);

                    if (!moveAction)
                    {
                        moveAction = (PlayerActionAI)ActionAI.CreateAction<ReachDestinationActionAI>(player, this, false, ActionUpdateFunction.Update, new ReachDestinationActionParams() { Destination = targetPositions[i] });
                        moveAction.OnActionCompleted += HandleOnActionCompleted;
                        moveAction.OnActionInterrupted += HandleOnActionInterrupted;
                        actions.Add(moveAction);
                    }
                    else
                    {
                        moveAction.Initialize(new ReachDestinationActionParams() { Destination = targetPositions[i] });
                    }

                    // Create action
                    // moveAction = (PlayerActionAI)ActionAI.CreateAction<ReachDestinationActionAI>(player, this, false, ActionUpdateFunction.Update, new ReachDestinationActionParams() { Destination = targetPositions[i] });
                    // moveAction.OnActionCompleted += HandleOnActionCompleted;
                    // moveAction.OnActionInterrupted += HandleOnActionInterrupted;
                    //player.transform.position = basePositions[i];

                }
            }


        }

        

        

        private void HandleOnActionInterrupted(ActionAI action)
        {
            actions.Remove(action as PlayerActionAI);
        }

        private void HandleOnActionCompleted(ActionAI action, bool succeeded)
        {
            actions.Remove(action as PlayerActionAI);
        }

        bool CheckConditions(PlayerAI player)
        {
            // For example an action may be considered interrupted if the player is stunned ( or even if they are facing an opponent and they need to
            // switch direction )
            return true;
        }


    }

}

