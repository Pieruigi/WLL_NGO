using System.Collections;
using System.Collections.Generic;
using Unity.Services.Matchmaker.Models;
using UnityEngine;

namespace WLL_NGO.AI
{
    public abstract class TeamActionAI : ActionAI
    {
        private TeamAI teamAI;
        public TeamAI TeamAI { get { return teamAI; } }


        public override void Initialize(ActionParams parameters = default)
        {
            base.Initialize(parameters);
            teamAI = Owner.GetComponent<TeamAI>();
        }
    }

}
