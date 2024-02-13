using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WLL_NGO.Multiplay;
using WLL_NGO.Netcode;

namespace WLL_NGO.UI
{
    public class NetworkLauncherUI : MonoBehaviour
    {
        public void StartClient()
        {
#if NO_MM

            NetworkLauncher.Instance.StartClient();

#else
            ClientMatchmaker.Instance.Play(GameMode.Classic);
#endif
        }

        public void StartHost()
        {
            NetworkLauncher.Instance.StartHost();
        }
    }

}
