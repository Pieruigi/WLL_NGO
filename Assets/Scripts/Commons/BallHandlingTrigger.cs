using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;
using WLL_NGO.Netcode;

namespace WLL_NGO
{
    /// <summary>
    /// Server only trigger.
    /// We are using Physics.Overlap() rather than OnTriggerEnter() due to the fact that if we set the ball kinematic theexit trigger is called.
    /// </summary>
    public class BallHandlingTrigger : MonoBehaviour
    {
        public UnityAction OnBallEnter;
        public UnityAction OnBallExit;

        CapsuleCollider coll;
        bool inside = false;
        bool disabled = false;

        PlayerController player;
      
      
        private void Awake()
        {
            coll = GetComponent<CapsuleCollider>();
            player = GetComponentInParent<PlayerController>();
        }

        private void FixedUpdate()
        {
            
            if(!NetworkManager.Singleton.IsServer || disabled) return;

            Vector3 pointA = transform.position + Vector3.up * (transform.position.y + (coll.height / 2f - coll.radius));
            Vector3 pointB = transform.position - Vector3.up * ( transform.position.y - (coll.height / 2f - coll.radius));
            float radius = coll.radius;
            Collider[] colls = Physics.OverlapCapsule(pointB, pointA, radius, LayerMask.GetMask(new string[] { "Ball" }));
            if(colls.Length > 0)
            {
                if(!inside)
                {
                    inside = true;
                    OnBallEnter?.Invoke();
                }
            }
            else
            {
                if(inside)
                {
                    inside = false;
                    OnBallExit?.Invoke();
                }
            }
        }

      
        /// <summary>
        /// Set enable on/off.
        /// If you disable the trigger the player will eventually lose ball control.
        /// </summary>
        /// <param name="value"></param>
        public void SetEnable(bool value)
        {
            disabled = !value;
            if(disabled)
            {
                if (inside)
                {
                    // If inside set false and eventually release the ball is the player is the owner
                    inside = false;
                    OnBallExit.Invoke();
                }
            }
            
            //if(player.IsServer) 
            //    player.SetBallHandlingTriggerEnableClientRpc(value);

            
        }

    }

}
