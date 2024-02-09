using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WLL_NGO.Netcode;

namespace WLL_NGO.UI
{
    public class NetworkLauncherUI : MonoBehaviour
    {
        public void StartClient()
        {
            NetworkLauncher.Instance.StartClient();
        }

        public void StartHost()
        {
            NetworkLauncher.Instance.StartHost();
        }
    }

}
