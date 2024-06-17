using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace WLL_NGO.AI
{
    public class FieldBlock : MonoBehaviour
    {
        public static UnityAction<FieldBlock, PlayerAI> OnPlayerEnter;
        public static UnityAction<FieldBlock, PlayerAI> OnPlayerExit;

        private void OnTriggerEnter(Collider other)
        {
            if(other.CompareTag(Tags.Player))
                OnPlayerEnter?.Invoke(this, other.GetComponent<PlayerAI>());
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag(Tags.Player))
                OnPlayerExit?.Invoke(this, other.GetComponent<PlayerAI>());
        }
    }

}
