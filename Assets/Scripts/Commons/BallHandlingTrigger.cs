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

        private void OnTriggerEnter(Collider other)
        {
            // It's the ball
            if (other.CompareTag(Tags.Ball))
                OnBallEnter?.Invoke();

        }

        private void OnTriggerExit(Collider other)
        {
            // It's the ball
            if (other.CompareTag(Tags.Ball))
            {
                OnBallExit?.Invoke();
            }
        }
    }

}
