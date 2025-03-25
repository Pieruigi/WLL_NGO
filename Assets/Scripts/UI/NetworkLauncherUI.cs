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
            MatchInfo.GameMode = GameMode.Powered;
#if NO_MM

            NetworkLauncher.Instance.StartClient();

#else
            ClientMatchmaker.Instance.Play();
#endif
        }

        public void StartHost()
        {
            MatchInfo.GameMode = GameMode.Powered;
            NetworkLauncher.Instance.StartHost();
        }
    }

}
