using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WLL_NGO.Netcode;

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
            foreach (ZoneTrigger zt in TeamAI.WaitingZoneTriggers)
                zt.Activate(true);

            // Clear player target and dictionary
            moveActions.Clear();
            foreach (PlayerAI player in TeamAI.Players)
            {
                player.TargetPlayer = null;
                moveActions.Add(player, null);
            }
            timer = loopTime;
        }

        protected override void Release()
        {
            ZoneTrigger.OnOpponentPlayerEnter -= HandleOnOpponentPlayerEnter;
            ZoneTrigger.OnOpponentPlayerExit -= HandleOnOpponentPlayerExit;

            // Deactivate triggers
            foreach (ZoneTrigger zt in TeamAI.WaitingZoneTriggers)
                zt.Activate(false);

            foreach (PlayerAI player in TeamAI.Players)
            {
                if (moveActions[player])
                    moveActions[player].DestroyAction();

            }
            moveActions.Clear();
        }



        private void HandleOnOpponentPlayerEnter(ZoneTrigger trigger, PlayerAI player)
        {
            if (player.HasBall || !trigger.Caretaker.TargetPlayer || trigger.Caretaker.IsDoublingGuard)
                trigger.Caretaker.TargetPlayer = player;

        }

        private void HandleOnOpponentPlayerExit(ZoneTrigger trigger, PlayerAI player)
        {
            // We must check the waiting line too
            if (!player.HasBall)
            {
                trigger.Caretaker.TargetPlayer = null;
            }
            else
            {
                // Double guard only if the target is entering the zone of a teammate
                foreach (var t in TeamAI.WaitingZoneTriggers)
                {
                    if (t == trigger)
                        continue;
                    if (t.InTriggerList.Contains(player))
                    {
                        trigger.Caretaker.StartDoubleGuard(player);
                        return;
                    }
                    trigger.Caretaker.TargetPlayer = null;
                }

            }

        }

        protected override void Loop()
        {
            base.Loop();


            // if (timer > 0)
            //     timer -= DeltaTime;//UpdateFunction == ActionUpdateFunction.FixedUpdate ? Time.fixedDeltaTime : Time.deltaTime;

            // if (timer > 0)
            //     return;

            // timer = loopTime;

            CheckTargets();
        }

        private void CheckTargets()
        {

            foreach (PlayerAI player in TeamAI.Players)
            {


                // Not for the goalkeeper
                if (player.Role == PlayerRole.GK)
                    continue;

                if (player.IsSelected && !TeamAI.TeamController.IsBot())
                {
                    // if (moveActions[player])
                    //     moveActions[player].DestroyAction();
                    continue;
                }


                ZoneTrigger trigger = TeamAI.WaitingZoneTriggers.Find(t => t.Caretaker == player);

                //player.TargetPlayer = null; //TEST - Remove

                if (player.TargetPlayer) // We already have a target
                {



                    Vector3 pos = Vector3.ProjectOnPlane(TeamAI.NetController.transform.position - player.TargetPlayer.Position, Vector3.up);
                    pos = pos.normalized * TeamAI.GetDefensiveDistance();
                    pos += player.TargetPlayer.Position;

                    pos = player.TargetPlayer.Position;
                    if (Vector3.Distance(player.Position, pos) < ReachDestinationActionParams.TolleranceDefault)
                        continue;

                    Debug.Log("TEST - AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA player:" + player.gameObject.name);
                    if (!moveActions[player])
                    {

                        moveActions[player] = CreateAction<ReachDestinationActionAI>(player, this, false, ActionUpdateFunction.Update, new ReachDestinationActionParams() { Destination = pos }, () => { return (!player.IsSelected || TeamAI.TeamController.IsBot()); });
                        //moveActions[player].OnActionCompleted += HandleOnMoveActionCompleted;
                    }
                    moveActions[player].Initialize(new ReachDestinationActionParams() { Destination = pos });
                }
                else // We don't have any target yet 
                {
                    Vector3 destination = trigger.DefaultPosition;

#if TEST_AI
                    TestBallController ball = team.BallController;
#else
                    BallController ball = BallController.Instance;
#endif

                    // Check if the ball is over the defence line
                    bool over = BallIsOverTheWaitingLine();
                    Vector3 dist = Vector3.ProjectOnPlane(ball.Position - destination, Vector3.up);

                    //destination += dist / 4f;
                    if (Vector3.Distance(player.Position, destination) < ReachDestinationActionParams.TolleranceDefault)
                        continue;


                    if (!moveActions[player])
                    {
                        moveActions[player] = CreateAction<ReachDestinationActionAI>(player, this, false, ActionUpdateFunction.Update, new ReachDestinationActionParams() { Destination = destination }, () => { return (!player.IsSelected || TeamAI.TeamController.IsBot()); }); ;
                        //moveActions[player].OnActionCompleted += HandleOnMoveActionCompleted;
                    }
                    moveActions[player].Initialize(new ReachDestinationActionParams() { Destination = destination });
                }
            }
        }

        bool BallIsOverTheWaitingLine()
        {
            TeamAI team = TeamAI;
#if TEST_AI
            TestBallController ball = team.BallController;
#else
            BallController ball = BallController.Instance;
#endif
            Vector3 ballDir = ball.Position - team.NetController.transform.position;


            return ballDir.x < TeamAI.WaitingLine;
        }
        
        

    }

}
