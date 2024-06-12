using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.PlayerSettings;

namespace WLL_NGO.AI
{
    public class ZoneDefenceActionAI : TeamActionAI
    {
        float timer = 0;
        float loopTime = .5f;

        [SerializeField]
        Dictionary<PlayerAI, ActionAI> moveActions = new Dictionary<PlayerAI, ActionAI>();
        
        protected override void Activate()
        {
            // Set triggers callbacks
            ZoneTrigger.OnOpponentPlayerEnter += HandleOnOpponentPlayerEnter;
            ZoneTrigger.OnOpponentPlayerExit += HandleOnOpponentPlayerExit;
            // Activate triggers
            foreach(ZoneTrigger zt in TeamAI.DefenceZoneTriggers)
                zt.Activate(true);

            // Clear player target and dictionary
            moveActions.Clear();
            foreach (PlayerAI player in TeamAI.Players)
            {
                player.TargetPlayer = null;
                moveActions.Add(player, null);
            }
                
        }

        private void OnDisable()
        {
            ZoneTrigger.OnOpponentPlayerEnter -= HandleOnOpponentPlayerEnter;
            ZoneTrigger.OnOpponentPlayerExit -= HandleOnOpponentPlayerExit;

            // Deactivate triggers
            foreach (ZoneTrigger zt in TeamAI.DefenceZoneTriggers)
                zt.Activate(false);
        }

        

        protected override bool CheckConditions()
        {
            return true;
        }

        

        private void HandleOnOpponentPlayerEnter(ZoneTrigger trigger, PlayerAI player)
        {
            if (player.HasBall || !trigger.Caretaker.TargetPlayer)
                trigger.Caretaker.TargetPlayer = player;
            
        }

        private void HandleOnOpponentPlayerExit(ZoneTrigger trigger, PlayerAI player)
        {
            // We must check the waiting line too
            Debug.Log($"Player exit:{player}");
            if(!player.HasBall)
                trigger.Caretaker.TargetPlayer = null;
        }

        protected override void Loop()
        {
            base.Loop();
            

            if (timer > 0)
                timer -= UpdateFunction == ActionUpdateFunction.FixedUpdate ? Time.fixedDeltaTime : Time.deltaTime;

            if (timer > 0)
                return;

            timer = loopTime;

            CheckTargets();
        }

        private void CheckTargets()
        {
            
            foreach (PlayerAI player in TeamAI.Players)
            {
                

                // Not for the goalkeeper
                if (player.Role == PlayerRole.GK)
                    continue;

                

                ZoneTrigger trigger = TeamAI.DefenceZoneTriggers.Find(t=>t.Caretaker == player);

                if (player.TargetPlayer) // We already have a target
                {


                    //if (player.TargetPlayer.HasBall) // The target player is holding the ball
                    //{
                    //    if(trigger.InTriggerList.Contains(player.TargetPlayer)) // The target player is also in the current player zone... done!
                    //    {
                    //        continue;
                    //    }
                    //    else // The target player is not in the current player zone, we must check a timer
                    //    {
                    //        // TO-DO: set a timer in the trigger exit and check here
                    //    }
                    //}
                    //else // The target player is not holding the ball
                    //{
                    //    // If there is an opponent in the current player zone that is holding the ball we change target
                    //    PlayerAI tmp = trigger.InTriggerList.Find(p => !p.IsTeammate(player) && p.HasBall);
                    //    if (tmp) // Found an opponent with the ball in the current player zone
                    //    {
                    //        // Switch the target and continue
                    //        player.TargetPlayer = tmp;
                    //        continue;
                    //    }
                    //    else // No opponent with ball found in our zone
                    //    {
                    //        if(trigger.InTriggerList.Contains(player.TargetPlayer))
                    //    }
                    //}
                    Vector3 pos = Vector3.ProjectOnPlane(TeamAI.NetController.transform.position - player.TargetPlayer.Position, Vector3.up);
                    pos = pos.normalized * TeamAI.GetDefensiveDistance();
                    pos += player.TargetPlayer.Position;
                    if (!moveActions[player])
                    {
                        
                        moveActions[player] = CreateAction<ReachDestinationActionAI>(player, this, false, ActionUpdateFunction.Update, new object[] { pos });
                        //moveActions[player].OnActionCompleted += HandleOnMoveActionCompleted;
                    }
                    Debug.Log($"Move action:{moveActions[player]}");
                    moveActions[player].Initialize(new object[] { pos });
                }
                else // We don't have any target yet 
                {
                    if (!moveActions[player])
                    {
                        moveActions[player] = CreateAction<ReachDestinationActionAI>(player, this, false, ActionUpdateFunction.Update, new object[] { trigger.DefaultPosition }); ;
                        //moveActions[player].OnActionCompleted += HandleOnMoveActionCompleted;
                    }
                    moveActions[player].Initialize(new object[] { trigger.DefaultPosition });
                }
            }
        }
    }

}
