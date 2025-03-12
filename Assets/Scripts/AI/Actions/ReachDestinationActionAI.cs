using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace WLL_NGO.AI
{
    public class ReachDestinationActionAI : PlayerActionAI
    {
        bool loop = false;

        float timer = 0;
        Vector3 destination = Vector3.zero;
        bool reached = false;

        float tollerance = 2f;

        protected override void Activate()
        {
            timer = 5;
            loop = true;
        }



        protected override void Loop()
        {
            if (!loop)
                return;

#if TEST_AI
            // Trying to simulate the joystick behaviour as much as possible
            var dir = Vector3.ProjectOnPlane(destination - PlayerAI.transform.position, Vector3.up);
            if (dir.magnitude > 0.01f)
            {
                // Rotate 
                Quaternion targetRotation = Quaternion.LookRotation(dir.normalized);
                PlayerAI.transform.rotation = Quaternion.RotateTowards(PlayerAI.transform.rotation, targetRotation, PlayerAI.RotationSpeed * Time.deltaTime);

                // Move along fwd
                PlayerAI.transform.position += PlayerAI.transform.forward * Time.deltaTime * PlayerAI.Speed;

            }

#else
            // Using input handler
#endif
        }

        public override bool IsCompleted(out bool succeeded)
        {
            var dir = Vector3.ProjectOnPlane(destination - PlayerAI.transform.position, Vector3.up);
            if (dir.magnitude < tollerance)
            {
                succeeded = true;
                return true;
            }
            else
            {
                succeeded = false;
                return false;
            }

        }

        /// <summary>
        /// float: destination
        /// float: tollerance
        /// </summary>
        /// <param name="parameters"></param>
        public override void Initialize(ActionParams parameters = default)
        {
            base.Initialize(parameters);
            // if (parameters == null)
            // {
            //     OnActionCompleted?.Invoke(this, false);
            //     return;
            // }
            // Cast params
            ReachDestinationActionParams p = (ReachDestinationActionParams)parameters;

            // Destination
            destination = (Vector3)p.Destination;
            destination.y = 0;
            // Tollerance
            tollerance = p.Tollerance;

        }

        // public override void Initialize(object[] parameters = null)
        // {
        //     base.Initialize(parameters);
        //     if (parameters == null)
        //     {
        //         OnActionCompleted?.Invoke(this, false);
        //         return;
        //     }

        //     // Destination
        //     destination = (Vector3)parameters[0];
        //     destination.y = 0;
        //     // Tollerance
        //     tollerance = .75f;

        // }

    }

    public class ReachDestinationActionParams : ActionParams
    {
        public Vector3 Destination;
        public float Tollerance = .75f;
    }

}
