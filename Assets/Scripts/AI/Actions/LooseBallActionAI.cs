using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Services.Matchmaker.Models;
using UnityEngine;

namespace WLL_NGO.AI
{
    public class LooseBallActionAI : TeamActionAI
    {
        [SerializeField]
        Dictionary<PlayerAI, ActionAI> moveActions = new Dictionary<PlayerAI, ActionAI>();

        protected override void Activate()
        {
            moveActions.Clear();
            foreach(var player in TeamAI.Players)
            {
                moveActions.Add(player, null);
            }
        }

        protected override void Release()
        {
            foreach (var player in TeamAI.Players)
            {
                if (moveActions[player])
                    moveActions[player].DestroyAction();

            }
            moveActions.Clear();
        }

        protected override void Loop()
        {
            base.Loop();

            // If the ball is in you midfield one of your players always tries to reach it
            bool inMyMidfield = (TeamAI == TeamAI.HomeTeamAI && TeamAI.BallController.Position.x < 0) || (TeamAI == TeamAI.AwayTeamAI && TeamAI.BallController.Position.x > 0);
            if(inMyMidfield)
            {
                // Get the closest player
                List<PlayerAI> players = TeamAI.Players.Where(p=>p.Role != PlayerRole.GK).ToList();
                PlayerAI closest = Utility.GetTheClosestPlayer(TeamAI.BallController.Position, players);
                
                if (moveActions[closest] == null)
                {
                    // Eventually clear other actions
                    foreach(var key in moveActions.Keys)
                    {
                        if (moveActions[key])
                            moveActions[key].DestroyAction();
                    }

                    moveActions[closest] = CreateAction<ReachDestinationActionAI>(closest, this, false, ActionUpdateFunction.Update);

                }
                Vector3 pos = TeamAI.BallController.Position;
                moveActions[closest].Initialize(new object[] { pos });
            }
        }
    }

}
