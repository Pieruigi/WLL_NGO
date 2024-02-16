using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
#if NO_MM && UNITY_EDITOR
using ParrelSync;
#endif

namespace WLL_NGO
{
    /// <summary>
    /// The main launcher simply reads arguments to check whether the app is a client or the server.
    /// </summary>
    public class Launcher : MonoBehaviour
    {
       
        //public bool IsServer { get; private set; } = false;
        

        private void Start()
        {
            if(Utility.IsDedicatedServer())
            {
                // Server
                SceneManager.LoadScene(Constants.ServerMainScene);
            }
            else
            {
                // Client
                SceneManager.LoadScene(Constants.ClientMainScene);
            }
        }

       
    }

}
