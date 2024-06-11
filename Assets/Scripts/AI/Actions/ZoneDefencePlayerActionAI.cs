using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using WLL_NGO.AI;
using static UnityEditor.PlayerSettings;

namespace WLL_NGO.AI
{
    public class ZoneDefencePlayerActionAI : PlayerActionAI
    {
        List<PlayerAI> opponentsInZone = new List<PlayerAI>();
        ZoneTrigger defensiveZone;
        PlayerAI targetPlayer;
        ActionAI moveAction;
        TeamAI opponentTeam;

        public override void Initialize(object[] parameters = null)
        {
            base.Initialize(parameters);
            if (PlayerAI.Role == PlayerRole.GK)
                return;

            // You can have only one defensive zone
            defensiveZone = ZoneTrigger.GetZoneTriggers(PlayerAI)[0];

            ZoneTrigger.OnOpponentPlayerEnter += HandleOnPlayerEnter;
            ZoneTrigger.OnOpponentPlayerExit += HandleOnPlayerExit;
            defensiveZone.Activate(true);

            opponentsInZone.Clear();
            //opponents = defensiveZone.Opponents.ToList();
            targetPlayer = null;
            
            opponentTeam = FindObjectsOfType<TeamAI>().First<TeamAI>(t=>t != PlayerAI.TeamAI);
            
        }

        private void OnDisable()
        {

            if (PlayerAI.Role == PlayerRole.GK)
                return;


            ZoneTrigger.OnOpponentPlayerEnter -= HandleOnPlayerEnter;
            ZoneTrigger.OnOpponentPlayerExit -= HandleOnPlayerExit;
            defensiveZone.Activate(false);
            opponentsInZone.Clear();
            targetPlayer = null;
            

        }

        private void HandleOnMoveActionCompleted(ActionAI arg0, bool arg1)
        {
            
        }

        private void HandleOnPlayerExit(ZoneTrigger zone, PlayerAI player)
        {
            if (!zone.IsPlayerZone(PlayerAI) || PlayerAI.TeamAI == player.TeamAI)
                return;

            if (PlayerAI.TeamAI == player.TeamAI)
                return;

            Debug.Log($"AI - Opponent left {PlayerAI.gameObject.name} zone:{player.gameObject.name}");
            
            
            opponentsInZone.Remove(player);

            //if (opponents.Count == 0 && targetPlayer != player)
            //    targetPlayer = null;

            
        }

        private void HandleOnPlayerEnter(ZoneTrigger zone, PlayerAI player)
        {
            if (!zone.IsPlayerZone(PlayerAI) || PlayerAI.TeamAI == player.TeamAI || player.Role == PlayerRole.GK)
                return;
            

            Debug.Log($"AI - Opponent entered {PlayerAI.gameObject.name} zone:{player.gameObject.name}");
            if(!opponentsInZone.Contains(player))
                opponentsInZone.Add(player);

            //if (!targetPlayer || player.HasBall)
            //    targetPlayer = player;
        }



        protected override bool CheckConditions()
        {
            return true;
        }

        protected override void Activate()
        {
            
        }



        protected override void Loop()
        {
            base.Loop();

            CheckTarget(); 

           
            
           
        }

        void CheckTarget()
        {
            // If there is a target we move accordingly, otherwise we move back to the default position
            if (!targetPlayer)
            {
                targetPlayer = opponentsInZone.Find(o => o.HasBall);
                if(!targetPlayer)
                {
                    List<PlayerAI> list = opponentsInZone;
                    if (list.Count == 0)
                        list = opponentTeam.Players.ToList();
                    targetPlayer = GetTheClosestOpponent(list);
                }
                    
            }
            else
            {
                if(opponentsInZone.Count > 0)
                {
                    // The target the player is following is in its zone
                    if (opponentsInZone.Contains(targetPlayer))
                    {
                        // Is there another player with the ball?
                        PlayerAI tmp = opponentsInZone.Find(p => p.HasBall);
                        if (tmp)
                        {
                            // We must decide if attack the player with the ball
                        }
                    }
                    else
                    {
                        // The target the player is following is not in its zone... we can move back to our zone and start following another player
                        targetPlayer = GetTheClosestOpponent(opponentsInZone);
                    }
                }
            }
           
            Vector3 pos = Vector3.ProjectOnPlane(PlayerAI.TeamAI.NetController.transform.position - targetPlayer.Position, Vector3.up);
            pos = pos.normalized * PlayerAI.TeamAI.GetDefensiveDistance();
            pos += targetPlayer.Position;
            //Vector3 dir = pos - transform.position;
            //dir = dir.normalized;
#if TEST_AI
            if (!moveAction)
            {
                moveAction = CreateAction<ReachDestinationActionAI>(PlayerAI, this, ActionUpdateFunction.Update, new object[] { pos });
                moveAction.OnActionCompleted += HandleOnMoveActionCompleted;
            }
            moveAction.Initialize(new object[] { pos });

           
#endif
        }

        public PlayerAI GetTheClosestOpponent(List<PlayerAI> playerList)
        {
            float dist = 0;
            PlayerAI ret = null;
            foreach(PlayerAI opp in opponentTeam.Players)
            {
                if (opp.Role == PlayerRole.GK)
                    continue;
                float d = Vector3.ProjectOnPlane(opp.Position-PlayerAI.Position, Vector3.up).magnitude;
                if(!ret || d < dist)
                {
                    ret = opp;
                    dist = d;
                }
            }

            return ret;
        }
    }

}
