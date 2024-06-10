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
        List<PlayerAI> opponents = new List<PlayerAI>();
        ZoneTrigger defensiveZone;
        PlayerAI targetPlayer;
        ActionAI moveAction;
        

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

            opponents.Clear();
            //opponents = defensiveZone.Opponents.ToList();
            targetPlayer = null;

            // Create a movement action
            
        }

        private void OnDisable()
        {

            if (PlayerAI.Role == PlayerRole.GK)
                return;


            ZoneTrigger.OnOpponentPlayerEnter -= HandleOnPlayerEnter;
            ZoneTrigger.OnOpponentPlayerExit -= HandleOnPlayerExit;
            defensiveZone.Activate(false);
            opponents.Clear();
            targetPlayer = null;
            

        }

        private void HandleOnMoveActionCompleted(ActionAI arg0, bool arg1)
        {
            
        }

        private void HandleOnPlayerExit(ZoneTrigger zone, PlayerAI player)
        {
            if (!zone.IsPlayerZone(PlayerAI))
                return;

            if (PlayerAI.TeamAI == player.TeamAI)
                return;

            Debug.Log($"AI - Opponent left {PlayerAI.gameObject.name} zone:{player.gameObject.name}");
            
            opponents.Remove(player);
            if (opponents.Count == 0)
                targetPlayer = null;
        }

        private void HandleOnPlayerEnter(ZoneTrigger zone, PlayerAI player)
        {
            if (!zone.IsPlayerZone(PlayerAI))
                return;
            if (PlayerAI.TeamAI == player.TeamAI)
                return;

            Debug.Log($"AI - Opponent entered {PlayerAI.gameObject.name} zone:{player.gameObject.name}");
            if(!opponents.Contains(player))
                opponents.Add(player);

            if (!targetPlayer)
                targetPlayer = player;
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

            

            // If there is a target we move accordingly, otherwise we move back to the default position
            if (targetPlayer)
            {
                Vector3 pos = Vector3.ProjectOnPlane(PlayerAI.TeamAI.NetController.transform.position - targetPlayer.transform.position, Vector3.up);
                pos = pos.normalized * PlayerAI.TeamAI.GetDefensiveDistance();
                pos = targetPlayer.transform.position;
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
            else
            {
                if (!moveAction)
                {
                    moveAction = CreateAction<ReachDestinationActionAI>(PlayerAI, this, ActionUpdateFunction.Update, new object[] { defensiveZone.DefaultPosition });
                    moveAction.OnActionCompleted += HandleOnMoveActionCompleted;
                }
                moveAction.Initialize(new object[] { defensiveZone.DefaultPosition });
            }
        }
    }

}
