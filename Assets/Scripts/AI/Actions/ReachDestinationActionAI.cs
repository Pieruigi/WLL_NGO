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

        float tollerance = .5f;
        
        protected override void Activate()
        {
            timer = 5;
            loop = true;
            Debug.Log($"REACH - Setting loop:{loop}");
        }



        protected override void Loop()
        {
            if (!loop)
                return;

#if TEST_AI
            //timer -= Time.deltaTime;
            // Reach destination
            Vector3 dir = destination - PlayerAI.transform.position;
            dir.y = 0;
            if(dir.magnitude < tollerance)
                reached = true;
            else
                PlayerAI.transform.position += dir.normalized*PlayerAI.Speed*DeltaTime;
#endif
        }

        public override bool IsCompleted(out bool succeeded)
        {
            succeeded = true;
            return reached;
        }

        public override void Initialize(object[] parameters = null)
        {
            base.Initialize(parameters);
            if (parameters == null)
                return;
            destination = (Vector3)parameters[0];
            destination.y = 0;
        }

    }

}
