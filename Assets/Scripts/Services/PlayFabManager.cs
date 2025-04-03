using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace WLL_NGO.Services
{
    public class PlayFabManager : SingletonPersistent<PlayFabManager>
    {

        public static UnityAction OnLogInSucceeded;
        public static UnityAction</*Error code*/int> OnLogInFailed;



        void Start()
        {

        }

        public void LogIn(UnityAction OnSucceeded, UnityAction<int> OnFailed)
        {
            OnLogInSucceeded?.Invoke();

        }

        void OnLoginSucceeded()
        {

        }

        void OnLoginFailed(int errorCode)
        {
            Debug.Log($"[Playfab - Login failed:{errorCode}]");
        }

        void LoadCatalog()
        {
            
        }

    }
    
}
