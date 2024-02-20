using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace WLL_NGO
{
    public class PlayerTackleTrigger : MonoBehaviour
    {

        private void OnTriggerEnter(Collider other)
        {
            
            if (!NetworkManager.Singleton.IsServer)
                return;


        }

    }

}
