using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace WLL_NGO.AI
{
    public class RootActionAI : TeamActionAI
    {
        
        protected override bool CheckConditions()
        {
            return true;
        }

        protected override void Activate()
        {
            object[] conditions = new object[] { TeamAI };
            if (AttackActionAI.EnterConditions(conditions))
                CreateAction<AttackActionAI>(Owner, this);
            else
                CreateAction<DefenceActionAI>(Owner, this);


        }

    }

}
