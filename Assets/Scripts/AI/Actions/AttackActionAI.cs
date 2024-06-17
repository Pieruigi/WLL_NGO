using System.Collections;
using System.Collections.Generic;
using Unity.Services.Matchmaker.Models;
using UnityEngine;

namespace WLL_NGO.AI
{
    public class AttackActionAI : TeamActionAI
    {

        protected override void Activate()
        {
            base.Activate();

            CreateAction<RandomAttackActionAI>(Owner, this, false, ActionUpdateFunction.Update, null, () => { return true; });
        }
    }

}
