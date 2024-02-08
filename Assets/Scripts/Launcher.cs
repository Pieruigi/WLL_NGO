using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
#if NO_MM && UNITY_EDITOR
using ParrelSync;
#endif

namespace WLL_NGO
{
    public class Launcher : MonoBehaviour
    {
        [SerializeField]
        string clientScene, serverScene;

        //public bool IsServer { get; private set; } = false;
        

        private void Awake()
        {
#if NO_MM
#if UNITY_EDITOR
            if (ClonesManager.GetArgument().Contains(Constants.DedicatedServerArg))
#else
            if (new List<string>(System.Environment.GetCommandLineArgs()).Contains(Constants.DedicatedServerArg))
#endif
#else
            if (new List<string>(System.Environment.GetCommandLineArgs()).Contains(Constants.DedicatedServerArg))
#endif
            {
                //IsServer = true;
                LoadScene(serverScene);
            }
            else
            {
                LoadScene(clientScene);
            }
        }

        void LoadScene(string sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }

    }

}
