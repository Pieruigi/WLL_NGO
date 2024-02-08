using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WLL_NGO
{
    public class Singleton<T> : MonoBehaviour where T : Component
    {
        public static T Instance { get; private set; }


        [SerializeField]
        bool persistent;

        protected virtual void Awake()
        {
            if (Instance == null)
            {
                Instance = gameObject.GetComponent<T>();
                if (persistent)
                    DontDestroyOnLoad(gameObject);
            }
            else
            {
                DestroyImmediate(gameObject);
            }


        }


    }

}
