using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WLL_NGO.AI
{
    public class PressingActionAI : TeamActionAI
    {
        protected override void Activate()
        {
            
        }

        protected override bool CheckConditions()
        {
            // We don't really need any condition here because if you get the ball the whole defend action branch will be destroyed
            return !WaitingActionAI.EnterConditions(new object[] { TeamAI });
        }
    }

}
