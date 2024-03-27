using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WLL_NGO.AI
{
    public class AttackActionAI : ActionAI
    {
        [SerializeField] bool conditions = false;
        [SerializeField] bool succeeded = false;
        [SerializeField] bool completed = false;

        protected override bool CheckConditions()
        {
            //throw new System.NotImplementedException();
            return conditions;
        }

        protected override void DoUpdate()
        {
            Debug.Log("Action - Updating...");
        }

        protected override bool IsCompleted(out bool succeeded)
        {
            succeeded = this.succeeded;
            return completed;
        }
    }

}
