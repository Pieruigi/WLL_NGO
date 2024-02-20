using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;
using WLL_NGO.Netcode;

namespace WLL_NGO
{
    public class BallHandlingTrigger : MonoBehaviour
    {
        public UnityAction OnBallEnter;
        public UnityAction OnBallExit;

        [SerializeField]
        bool useTrigger = false;

        CapsuleCollider coll;
        bool inside = false;

        private void Awake()
        {
            coll = GetComponent<CapsuleCollider>();
            
        }

        private void FixedUpdate()
        {
            if (useTrigger) return;

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

        private void OnTriggerEnter(Collider other)
        {
            if(!useTrigger) return;

            // It's the ball
            if (other.CompareTag(Tags.Ball))
                OnBallEnter?.Invoke();

        }

        private void OnTriggerExit(Collider other)
        {
            if (!useTrigger) return;

            // It's the ball
            if (other.CompareTag(Tags.Ball))
            {
                OnBallExit?.Invoke();
            }
        }


    }

}
