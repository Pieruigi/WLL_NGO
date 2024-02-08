#if UNITY_EDITOR
using ParrelSync;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.Events;

namespace WLL_NGO.Multiplay
{
    public class MultiplayUtilities
    {
        public static async Task SignInAsync(Action OnSignedIn = null, Action<RequestFailedException> OnFailed = null)
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
                await Initialize("TestProfileService");

            // If not authenticated then try to authenticate
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                AuthenticationService.Instance.SignInFailed += OnFailed;
                AuthenticationService.Instance.SignedIn += OnSignedIn;
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
        }

        public static async Task Initialize(string serviceProfileName)
        {
            if (serviceProfileName != null)
            {
#if UNITY_EDITOR
                serviceProfileName = $"{serviceProfileName}{GetCloneNumberSuffix()}";
#endif
                var initOptions = new InitializationOptions();
                initOptions.SetProfile(serviceProfileName);
                await UnityServices.InitializeAsync(initOptions);
            }
            else
            {
                await UnityServices.InitializeAsync();
            }
        }

#if UNITY_EDITOR
        public static string GetCloneNumberSuffix()
        {
            int lastUnderscore = ClonesManager.GetCurrentProjectPath().LastIndexOf("_");
            string suffix = ClonesManager.GetCurrentProjectPath().Substring(lastUnderscore + 1);
            if (suffix.Length != 1)
                suffix = "";
            return suffix;
        }
#endif

    }

}
