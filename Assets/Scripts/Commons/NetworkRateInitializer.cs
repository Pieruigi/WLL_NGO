using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace WLL_NGO.Netcode
{
    /// <summary>
    /// We set the network tick rate exactly equal to the physics tick rate.
    /// </summary>
    public class NetworkRateInitializer : MonoBehaviour
    {
        private void Awake()
        {
            
        }

        // Start is called before the first frame update
        void Start()
        {
            Time.fixedDeltaTime = 1f / Constants.ServerTickRate;
            //NetworkManager.Singleton.NetworkConfig.TickRate = Constants.ServerTickRate;
            Application.targetFrameRate = Constants.ServerTickRate;
            

            //Physics.simulationMode = SimulationMode.Script;
        }

        // Update is called once per frame
        void Update()
        {

        }
    }

}
