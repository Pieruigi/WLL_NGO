using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WLL_NGO.AI
{
    public abstract class PlayerActionAI : ActionAI
    {
        PlayerAI playerAI;
        public PlayerAI PlayerAI { get { return playerAI; } }


        public override void Initialize(object[] parameters = null)
        {
            base.Initialize(parameters);
            playerAI = Owner.GetComponent<PlayerAI>();
        }
    }

}
