using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using WLL_NGO.Netcode;

namespace WLL_NGO
{
    /// <summary>
    /// Load and stream all the scene objects in the client game scene and report the server when the client is ready.
    /// </summary>
    public class ClientGameSceneLoader : MonoBehaviour
    {
        private void Awake()
        {
            if(Utility.IsDedicatedServer())
            {
                Destroy(gameObject);
            }
            else
            {
                // Load and stream data
                Initialize();
            }
        }

        async void Initialize()
        {
            await Task.Delay(3000); // Just for test

            PlayerInfoManager.Instance.SetPlayerReadyServerRpc(NetworkManager.Singleton.LocalClientId);
        }
    }

}
