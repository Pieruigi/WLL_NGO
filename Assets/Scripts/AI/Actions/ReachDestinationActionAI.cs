using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WLL_NGO.AI
{
    public class ReachDestinationActionAI : PlayerActionAI
    {
        bool loop = false;

        float timer = 0;
        Vector3 destination = Vector3.zero;
        bool reached = false;
        
        protected override void Activate()
        {
            timer = 5;
            loop = true;
            Debug.Log($"REACH - Setting loop:{loop}");
        }

        protected override bool CheckConditions()
        {
            return true;
        }

        protected override void Loop()
        {
            if (!loop)
                return;

            Debug.Log("REACH - Looping");
            //timer -= Time.deltaTime;
            // Reach destination
            Vector3 dir = destination - PlayerAI.transform.position;
            dir.y = 0;
            if(dir.magnitude < 1.5)
                reached = true;
            else
                PlayerAI.transform.position += dir*3*Time.deltaTime;
            
        }

        public override bool IsCompleted(out bool succeeded)
        {
            succeeded = true;
            //loop = false;
            return reached;
        }

        public override void Initialize(object[] parameters = null)
        {
            base.Initialize(parameters);
            destination = (Vector3)parameters[0];
            destination.y = 0;
        }

    }

}
