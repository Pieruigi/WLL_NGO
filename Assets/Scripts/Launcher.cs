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
       
        //public bool IsServer { get; private set; } = false;
        

        private void Awake()
        {
            if(Utility.IsDedicatedServer())
            {
                SceneManager.LoadScene(Constants.ServerMainScene);
            }
            else
            {
                SceneManager.LoadScene(Constants.ClientMainScene);
            }
        }

       
    }

}
