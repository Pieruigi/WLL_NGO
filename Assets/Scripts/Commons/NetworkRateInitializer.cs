using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace WLL_NGO.Netcode
{
    public class NetworkRateInitializer : MonoBehaviour
    {
        private void Awake()
        {
            
        }

        // Start is called before the first frame update
        void Start()
        {
            Time.fixedDeltaTime = 1f / Constants.ServerTickRate;
            NetworkManager.Singleton.NetworkConfig.TickRate = Constants.ServerTickRate;
        }

        // Update is called once per frame
        void Update()
        {

        }
    }

}
