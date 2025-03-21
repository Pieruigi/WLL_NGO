using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace WLL_NGO.AI
{
    public class ReachDestinationActionAI : PlayerActionAI
    {
        bool loop = false;

        float timer = 0;
        Vector3 destination = Vector3.zero;
        bool reached = false;

        float tollerance = .75f;

        bool isAvoiding = false; // Flag per sapere se siamo in fase di schivata
        float avoidDuration = 2f; // Tempo di schivata prima di tornare alla direzione originale
        float avoidTimer = 0f;

        float avoidDetectionDistance = 5;

        float avoidSphereRadius = 3f;

        Vector3 avoidDirection;

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

                // Avoider
                RaycastHit hit;
                var origin = PlayerAI.Position + Vector3.up;
                var mask = LayerMask.GetMask(new string[] { Tags.Player });
                if(Physics.SphereCast(origin, avoidSphereRadius, dir.normalized, out hit, avoidDetectionDistance, mask))
                //if (Physics.Raycast(origin, dir.normalized, out hit, avoidRange, mask))
                {
                    //  if (PlayerAI.gameObject.name.EndsWith("_Red"))
                    //      Debug.Log($"TEST - Player {PlayerAI.gameObject} is going to hit {hit.collider.gameObject}");

                    if (hit.collider.CompareTag(Tags.Player) && PlayerAI.IsTeammate(hit.collider.GetComponent<PlayerAI>()))
                    {
                        Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;
                        Vector3 left = -right;

                        bool canGoRight = !Physics.Raycast(PlayerAI.transform.position, right, 2f, mask);
                        bool canGoLeft = !Physics.Raycast(PlayerAI.transform.position, left, 2f, mask);

                        if (canGoRight || canGoLeft)
                        {
                            float angleRight = Vector3.Angle(dir, right);
                            float angleLeft = Vector3.Angle(dir, left);

                            var avoidValue = dir.magnitude / 2f;

                            if (canGoRight && (!canGoLeft || angleRight < angleLeft))
                            {
                                avoidDirection = right * avoidValue;
                            }
                            else if (canGoLeft)
                            {
                                avoidDirection = left * avoidValue;
                            }

                            isAvoiding = true;
                            avoidTimer = 0f;
                        }


                    }
                }

                // Se stiamo ancora schivando, incrementiamo il timer
                if (isAvoiding)
                {
                     if(PlayerAI.gameObject.name.EndsWith("_4_Red"))
                         Debug.Log($"TEST - Player {PlayerAI.gameObject} is avoiding; avoid direction:{avoidDirection}");

                    dir += avoidDirection;
                    avoidTimer += Time.deltaTime;
                    if (avoidTimer >= avoidDuration)
                    {
                        isAvoiding = false; // Dopo un po', torniamo alla direzione originale
                    }
                }


                // Rotate 
                Quaternion targetRotation = Quaternion.LookRotation(dir.normalized);
                PlayerAI.transform.rotation = Quaternion.RotateTowards(PlayerAI.transform.rotation, targetRotation, PlayerAI.RotationSpeed * Time.deltaTime);

                // Move along fwd
                PlayerAI.transform.position += PlayerAI.transform.forward * Time.deltaTime * PlayerAI.Speed;

            }
#else
            // Using input handler
            var inputHandler = PlayerAI.Controller.GetInputHandler();
            var dir = Vector3.ProjectOnPlane(destination - PlayerAI.transform.position, Vector3.up);
            if (dir.magnitude > tollerance)
            {
                inputHandler.SetJoystick(new Vector2(dir.x, dir.z).normalized);
            }
            else
            {
                //if(dir.magnitude < tollerance * .5f)
                inputHandler.SetJoystick(Vector2.zero);
            }
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
        public float Tollerance = 2f;
    }

}
