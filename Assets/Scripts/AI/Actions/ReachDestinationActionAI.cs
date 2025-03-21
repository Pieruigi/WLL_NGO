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
        float avoidDuration = 2.5f; // Tempo di schivata prima di tornare alla direzione originale
        float avoidTimer = 0f;

        float avoidDetectionDistance = 2.5f;

        float avoidSphereRadius = 3f;

        Quaternion avoidanceRotation;

        float avoidanceAngle = 60f;

        protected override void Activate()
        {
            timer = 5;
            loop = true;
        }

        void OnDestroy()
        {
            PlayerAI.Controller.GetInputHandler().SetJoystick(Vector3.zero);
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
                AdjustDirectionToAvoidOtherPlayers(ref dir);
                

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
                AdjustDirectionToAvoidOtherPlayers(ref dir);
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


    void AdjustDirectionToAvoidOtherPlayers(ref Vector3 dir)
    {
        float sphereRadius = 1.5f; // Raggio per la sfera di rilevamento
        float detectionDistance = 3f; // Raggio di rilevamento per verificare se ci sono altri giocatori
        float avoidanceAngle = 60f; // Angolo di schivata
        float safetyDistance = 2f;  // Distanza di sicurezza per evitare collisioni

        LayerMask playerLayer = LayerMask.GetMask(Tags.Player); // Layer dei giocatori (assicurati di assegnare questo Layer ai giocatori!)

        // Trova solo altri giocatori nelle vicinanze usando il LayerMask
        Collider[] colliders = Physics.OverlapSphere(PlayerAI.Position, detectionDistance, playerLayer);

        foreach (var collider in colliders)
        {
            // Ignora se l'oggetto rilevato è il proprio giocatore
            if (collider.gameObject != PlayerAI.gameObject)
            {
                // Calcoliamo la direzione verso l'altro giocatore
                Vector3 toOtherPlayer = collider.GetComponent<PlayerAI>().Position - PlayerAI.Position;
                toOtherPlayer.y = 0; // Mantieni solo la componente orizzontale (evita la deviazione su Y)

                // Verifica se la direzione di movimento è in conflitto con un altro giocatore
                float angleBetween = Vector3.Angle(dir, toOtherPlayer);
                if (angleBetween < avoidanceAngle)
                {
                    Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;
                    Vector3 left = -right;

                    // Verifica se la strada è libera a destra o a sinistra
                    bool canGoRight = Physics.OverlapSphere(PlayerAI.Position + right * safetyDistance, sphereRadius, playerLayer).Length == 0;
                    bool canGoLeft = Physics.OverlapSphere(PlayerAI.Position + left * safetyDistance, sphereRadius, playerLayer).Length == 0;

                    // Se entrambi i lati sono liberi
                    if (canGoRight || canGoLeft)
                    {
                        // Schiva a destra se possibile
                        if (canGoRight && (!canGoLeft || Vector3.Angle(dir, right) < Vector3.Angle(dir, left)))
                        {
                            dir = Quaternion.AngleAxis(avoidanceAngle, Vector3.up) * dir; // Rotazione a destra di 45°
                        }
                        else if (canGoLeft)
                        {
                            dir = Quaternion.AngleAxis(-avoidanceAngle, Vector3.up) * dir; // Rotazione a sinistra di 45°
                        }
                    }
                    else
                    {
                        // Se entrambi i lati sono bloccati, prova ad andare all'indietro
                        dir = Quaternion.AngleAxis(180f, Vector3.up) * dir;
                    }
                }
            }
        }
    }



        void __AdjustDirectionToAvoidOtherPlayers(ref Vector3 dir)
{
    float sphereRadius = 3.0f; // Raggio dello SphereCast
    float detectionDistance = 3f; // Distanza di rilevamento
    float avoidanceAngle = 60f; // Angolo di schivata
    float safetyDistance = 3f;  // Distanza di sicurezza per evitare collisioni

    // Verifica la distanza e direzione del target
    RaycastHit hit;
    var mask = LayerMask.GetMask(new string[] { Tags.Player });
   
    if (Physics.SphereCast(PlayerAI.transform.position, sphereRadius, dir.normalized, out hit, detectionDistance, mask))
            {
                if (hit.collider.CompareTag("Player"))
                {
                    Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;
                    Vector3 left = -right;

                    // Verifica se la strada è libera a destra o a sinistra
                    bool canGoRight = !Physics.SphereCast(PlayerAI.transform.position, sphereRadius, right, out _, safetyDistance, mask);
                    bool canGoLeft = !Physics.SphereCast(PlayerAI.transform.position, sphereRadius, left, out _, safetyDistance, mask);

                    // Se entrambi i lati sono liberi
                    if (canGoRight || canGoLeft)
                    {
                        // Schiva a destra se possibile
                        if (canGoRight && (!canGoLeft || Vector3.Angle(dir, right) < Vector3.Angle(dir, left)))
                        {
                            // Applichiamo la rotazione a destra di 45°
                            dir = Quaternion.AngleAxis(avoidanceAngle, Vector3.up) * dir;
                        }
                        // Schiva a sinistra se possibile
                        else if (canGoLeft)
                        {
                            // Applichiamo la rotazione a sinistra di 45°
                            dir = Quaternion.AngleAxis(-avoidanceAngle, Vector3.up) * dir;
                        }
                    }
                    else
                    {
                        // Se entrambi i lati sono bloccati, tenta di andare all'indietro (schivata indietro)
                        dir = Quaternion.AngleAxis(180f, Vector3.up) * dir;
                    }
                }
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
        public const float TolleranceDefault = 2f;

        public Vector3 Destination;
        public float Tollerance = TolleranceDefault;
    }

}
