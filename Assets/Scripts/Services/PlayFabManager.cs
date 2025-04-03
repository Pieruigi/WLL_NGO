using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace WLL_NGO.Services
{
    public class PlayFabManager : SingletonPersistent<PlayFabManager>
    {

        public static UnityAction OnLogInSucceeded;
        public static UnityAction</*Error code*/int> OnLogInFailed;

        const string customIdKey = "PlayFabCustomId";

        LoginResult loginResult;

        void Start()
        {
            AnonymousLogIn();
        }

        string GetCustomId()
        {
            // Check player prefs
            string customId = "";
            if (PlayerPrefs.HasKey(customIdKey))
            {
                customId = PlayerPrefs.GetString(customIdKey);

                Debug.Log($"[PlayFabManager - Found custom id:{customId}]");
            }
            else
            {
                customId = $"WLL-{Guid.NewGuid()}";

                Debug.Log($"[PlayFabManager - New custom id created:{customId}]");
                PlayerPrefs.SetString(customIdKey, customId);
                PlayerPrefs.Save();
            }
            return customId;
        }

        void AnonymousLogIn()
        {

            LoginWithCustomIDRequest request = new LoginWithCustomIDRequest()
            {
                CustomId = GetCustomId(),
                CreateAccount = true,
                InfoRequestParameters = new GetPlayerCombinedInfoRequestParams
                {
                    GetTitleData = true,
                    UserDataKeys = null,
                    GetUserData = true,
                    GetUserAccountInfo = true,
                    TitleDataKeys = null,
                    GetUserReadOnlyData = true,
                    GetPlayerProfile = true,
                    ProfileConstraints = new PlayerProfileViewConstraints { ShowLocations = true }
                }
            };

            PlayFabClientAPI.LoginWithCustomID(request, OnLoginSucceeded, OnLoginFailed);

            OnLogInSucceeded?.Invoke();

        }

        void OnLoginSucceeded(LoginResult result)
        {
            Debug.Log($"[Playfab - Login succeeded:{result.PlayFabId}]");

            // Store login result
            loginResult = result;

            // Initialize catalog and inventory
            InitializeCatalogAndInventory();
        }

        void OnLoginFailed(PlayFabError result)
        {
            Debug.Log($"[Playfab - Login failed:{result.ErrorMessage}]");
        }

        void LoadCatalog()
        {

        }

        void InitializeCatalogAndInventory()
        {
            PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest { FunctionName = "checkInventoryAndReturnNew" },
            (result) =>
            
            {
                string resString = result.FunctionResult.ToString();
                  //           Debug.LogError("Persistent Data Path: " + Application.persistentDataPath);
                Debug.Log($"[PlayFabManager - Received json:{resString}]");


                  // System.IO.File.WriteAllText(Path.Combine(Application.persistentDataPath, "playercatalog.json"), resString);
                 //var response = JsonConvert.DeserializeObject<CheckInventoryAndReturnCloudResponse>(resString);
//                   if (response.Success)
//                   {
//                       GetNFTCatalog();

//                       //Debug.Log("TEST - Setting up user inventory");

//                       SetupUserInventory(response.data, result.InfoResultPayload.UserData);
//                       //Debug.Log("TEST - Setting up after login");
//                       SetupAfterLogin(result, completed);
//                       //Debug.Log("TEST - Completed");
//                       //TODO: fill super powers struct from inventory
//                       InitSuperPowersFromInventory();
//                       InitSuperPowerSlotsFromInventory();

// #if !UNITY_SERVER
//                       DTDAnalytics.CustomEvent("SETUP_INVENTORY_COMPLETED");
// #endif
//                   }
//                   else
//                   {
//                       Debug.LogError("Error while setting up inventory: " + response.Code);
// #if !UNITY_SERVER
//                       var customParam = new DTDCustomEventParameters();
//                       customParam.Add("ERROR_SETUP_INVENTORY", response.Code);

//                       DTDAnalytics.CustomEvent("ERROR_SETUP_INVENTORY", customParam);
// #endif
//                       OnLoginSuccess(result); // EDITED BY FRANCESCO - WHY LOGIN AGAIN IF ONLY THE CLOUDSCRIPT FAILED!?
//
//                  }
            },
            (error) =>
            {
                Debug.Log($"[PlayFabManager - An error occurred while retrieving catalog and inventory:{error.ErrorMessage}]");       
            });




        // if (CurrentLoginResult != null && string.IsNullOrEmpty(CurrentLoginResult.InfoResultPayload.AccountInfo.TitleInfo.AvatarUrl) == false)
        // {
        //     TexturesCacheManager.GetTextureOtherAddressables(
        //   CurrentLoginResult.InfoResultPayload.AccountInfo.TitleInfo.AvatarUrl,
        //   (texture) => { },
        //   () => { });
        // }
        }

    }
    
}
