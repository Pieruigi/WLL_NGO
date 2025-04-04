
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Threading.Tasks;
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

        public static UnityAction OnPlayFabSucceeded;
        public static UnityAction OnPlayFabFailed;

        public const string TeamInfoClass = "TeamInfo";
        public const string UserTeamKey = "UserTeam";

        const string customIdKey = "PlayFabCustomId";

        public const string PRESTIGE = "Prestige";
        public const string TROPHYROAD = "TrophyRoad";
        public const string EXPERIENCE = "XP";
        public const string LEVEL = "Level";

        public const string USER_TEAM = "UserTeam";
        public const string HEROPATH = "HeroPathV2";



        readonly string[] nftCatalogs = new string[] { "NFT_Season_2022" };

        public List<CatalogItem> NftCatalogItems { get; private set; } = new List<CatalogItem>();

        public List<CatalogItem> CatalogItems { get; private set; } = new List<CatalogItem>();

        public List<ItemInstance> Inventory { get; private set; } = new List<ItemInstance>();

        public TagStatus TagStatus { get; private set; } = TagStatus.noRelatedTag;

        public List<CatalogItem> Teams { get; private set; } = new List<CatalogItem>();

        public List<CatalogItem> OwnedTeams { get; private set; } = new List<CatalogItem>();

        public LoginResult LoginResult { get; private set; }

        public CountryCode? CountryCode { get; private set; }

        public int CurrentPrestige { get; private set; }

        public int Experience { get; private set; }

        public int Level { get; private set; }

        public int HeroPath { get; private set; }

        public bool TeamAuthRequested { get; private set; }

        public string DateOfBirth { get; private set; }

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



        }

        async void OnLoginSucceeded(LoginResult result)
        {
            Debug.Log($"[Playfab - Login succeeded:{result.PlayFabId}]");

            // Store login result
            LoginResult = result;

            // Set last user name
            SetLastUserName();

            // Set country code
            SetCountryCode();

            // Set up inventory and catalog
            bool[] results = await Task.WhenAll(InitializeNftCatalogs(), InitializeCatalogAndInventory());

            foreach (var res in results)
            {
                if (!res)
                {
                    Debug.LogError($"[PlayFabManager - Failed to initialize catalog and inventory]");
                    ClearAll();
                    return;
                }
            }

        }

        void OnLoginFailed(PlayFabError result)
        {
            Debug.Log($"[Playfab - Login failed:{result.ErrorMessage}]");
        }

        void SetLastUserName()
        {
            PlayerPrefs.SetString("SLL.LastUsername", LoginResult.InfoResultPayload.AccountInfo.TitleInfo.DisplayName);
        }

        void SetCountryCode()
        {
            if (LoginResult.InfoResultPayload.PlayerProfile != null && LoginResult.InfoResultPayload.PlayerProfile.Locations != null && LoginResult.InfoResultPayload.PlayerProfile.Locations.Count > 0)
            {
                Debug.Log($"[PlayFabManager - Country ISO found:{LoginResult.InfoResultPayload.PlayerProfile.Locations[0].CountryCode}]");

                // PIERLUIGI; HERE IS THE COUNTRY ISO ("EN" "IT" etc)
                CountryCode = LoginResult.InfoResultPayload.PlayerProfile.Locations[0].CountryCode;

            }
        }

        async Task<bool> InitializeCatalogAndInventory()
        {
            var tcs = new TaskCompletionSource<bool>();
            PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest { FunctionName = "checkInventoryAndReturnNew" },
            result =>
            {
                string resString = result.FunctionResult.ToString();
                //           Debug.LogError("Persistent Data Path: " + Application.persistentDataPath);
                Debug.Log($"[PlayFabManager - Received json:{resString}]");


                // System.IO.File.WriteAllText(Path.Combine(Application.persistentDataPath, "playercatalog.json"), resString);
                var response = JsonConvert.DeserializeObject<CheckInventoryAndReturnCloudResponse>(resString);
                if (response.success)
                {

                    Debug.Log($"[PlayFabManager - Cloud script response success:{response.success}]");

                    SetUpInventory(response.data);

                    tcs.SetResult(true);

                }
                else
                {
                    Debug.LogError($"[PlayFabManager - Cloud script response error:{response.message}]");
                    tcs.SetResult(false);
                }

            },
            error =>
            {
                Debug.Log($"[PlayFabManager - An error occurred while retrieving catalog and inventory:{error.ErrorMessage}]");
                tcs.SetResult(false);
            });

            return await tcs.Task;


            // if (CurrentLoginResult != null && string.IsNullOrEmpty(CurrentLoginResult.InfoResultPayload.AccountInfo.TitleInfo.AvatarUrl) == false)
            // {
            //     TexturesCacheManager.GetTextureOtherAddressables(
            //   CurrentLoginResult.InfoResultPayload.AccountInfo.TitleInfo.AvatarUrl,
            //   (texture) => { },
            //   () => { });
            // }
        }

        void SetUpInventory(LoginInventoryResponse data)
        {

            TagStatus = data.tagStatus;
            Inventory = data.inventory;
            CatalogItems = data.catalog;

            // Set up teams
            SetUpTeams();

            // Set up teams in global game manager
            GlobalGameManager.Instance.SetUpTeams();

            // Initialize team roster
            GlobalGameManager.Instance.InitializeTeamRoster(data.teamRoster);

            Debug.Log($"[PlayFabManager - Current team roster:{GlobalGameManager.Instance.CurrentTeamRoster}]");

            // Initialize purchasing system
            //iAPManager.Instance.InitializePurchasing(); // TODO: Purchasing system has to be configured

            // Initialize user data
        }

        void RefreshTeamUserInventory(List<ItemInstance> inventory, Action OnCompleted)
        {
            // Set new inventory
            this.Inventory = inventory;

            // Set up teams
            SetUpTeams();

            // Set up teams in global game manager
            GlobalGameManager.Instance.SetUpTeams();

            OnCompleted?.Invoke();
        }

        async Task<bool> InitializeNftCatalogs()
        {
            var tcs = new TaskCompletionSource<bool>();
            foreach (var catalog in nftCatalogs)
            {
                PlayFabClientAPI.GetCatalogItems(new GetCatalogItemsRequest { CatalogVersion = catalog }, result =>
                {
                    foreach (var item in result.Catalog)
                    {
                        Debug.Log($"[PlayFabManager - New NFT item found:{item.DisplayName}]");
                    }
                    NftCatalogItems.AddRange(result.Catalog);
                    // Succeeded
                    tcs.SetResult(true);
                },
                error =>
                {
                    NftCatalogItems.Clear();
                    // Failed
                    tcs.SetResult(false);
                });

            }
            return await tcs.Task;
        }



        void SetUpTeams()
        {
            ClearTeams();

            // Set up teams
            Teams = CatalogItems.Where(a => a.ItemClass == TeamInfoClass).ToList();

            // Sort by team id
            Teams.Sort((a, b) => GetTeamId(a).CompareTo(GetTeamId(b)));

            // Setup owned Teams
            foreach (var item in Teams)
            {
                var searchResult = Inventory.Find((x) =>
                {
                    return x.ItemId == item.ItemId;
                });
                if (searchResult != null)
                {
                    OwnedTeams.Add(item);
                }
            }



            foreach (var team in OwnedTeams)
            {
                Debug.Log($"[PlayFabManager - Owned team found:{team.DisplayName}]");
            }
        }




        int GetTeamId(CatalogItem catalogItem)
        {
            // Team1, Team2, Team11 etc.
            string teamName = catalogItem.ItemId;
            string teamId = teamName.Substring(4);
            return int.Parse(teamId);
        }

        void ClearAll()
        {
            NftCatalogItems.Clear();
            Inventory.Clear();
            CatalogItems.Clear();
            TagStatus = TagStatus.noRelatedTag;
            Teams.Clear();
            OwnedTeams.Clear();
        }

        void ClearTeams()
        {
            Teams.Clear();
            OwnedTeams.Clear();
        }

        public void UpdatePlayerTeamRoster(List<ItemInstance> cards, Action OnSuccessCallback = null, Action OnErrorCallback = null)
        {
            List<string> toSend = new List<string>();
            foreach (var item in cards)
            {
                toSend.Add(item.ItemInstanceId);
            }
            PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
            {
                FunctionName = "updateTeam",
                FunctionParameter = new
                {
                    TeamMembers = toSend.ToArray()
                }
            },
            result =>
            {
                Debug.Log("Team updated!");
                OnSuccessCallback?.Invoke();

                OnPlayFabSucceeded?.Invoke();
            },
            error =>
            {
                OnErrorCallback?.Invoke();

                OnPlayFabFailed?.Invoke();
            });
        }

        public void SetUpUserData(List<string> keys = null, System.Action<GetUserDataResult> onSuccess = null, Action onError = null)
        {
            PlayFab.ClientModels.GetUserDataRequest request = new GetUserDataRequest()
            {
                PlayFabId = LoginResult.PlayFabId,

            };

            if (keys != null && keys.Count > 0)
                request.Keys = keys;


            PlayFabClientAPI.GetUserData(request, result =>
            {


                if (result.Data.ContainsKey(PRESTIGE))
                {
                    CurrentPrestige = Convert.ToInt32(result.Data[PRESTIGE].Value.ToString());
                }
                /*  else
                  {
                      currentPrestige = 0;
                  }*/

                //Debug.Log("Prestige value is: " + currentPrestige);

                if (result.Data.ContainsKey(TROPHYROAD))
                {
                    // TODO: remove comment
                    //localTrophyRoad = JsonConvert.DeserializeObject<TrophyRoad>(result.Data[TROPHYROAD].Value.ToString());
                }

                if (request.Keys == null || request.Keys.Contains(EXPERIENCE))
                {
                    if (result.Data.ContainsKey(EXPERIENCE))
                    {
                        Experience = Convert.ToInt32(result.Data[EXPERIENCE].Value.ToString());
                        Debug.Log("Experience " + Experience);
                    }
                    else
                    {
                        SetUserData(new Dictionary<string, string>(){
                   {EXPERIENCE, "0"}},
                          UserDataPermission.Public,
                          false,
                          result => { Debug.Log("Reset value for " + EXPERIENCE); });
                    }
                }

                if (request.Keys == null || request.Keys.Contains(LEVEL))
                {
                    if (result.Data.ContainsKey(LEVEL))
                    {
                        Level = Convert.ToInt32(result.Data[LEVEL].Value.ToString());
                        Debug.Log("Level " + Level);
                    }
                    else
                    {

                        Level = 0;
                        SetUserData(new Dictionary<string, string>(){
                        {LEVEL, "0"}},
                            UserDataPermission.Public,
                            false,
                            result => { Debug.Log("Reset value for " + LEVEL); }
                        );

                        Debug.LogError("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
                        List<string> items = new List<string>();
                        for (int i = 0; i < 10; i++)
                        {
                            items.Add("InGameHealth");
                            if (i < 5)
                            {
                                items.Add("SuperShot");
                            }
                        }
                        GrantTFItems(items, () =>
                        {
                            RefreshInventory(() => { InitSuperPowersFromInventory(); });
                            Debug.Log("Granting InGameHealth succeeded");
                        });



                    }
                }

                if (request.Keys == null || request.Keys.Contains(USER_TEAM))
                {
                    if (result.Data.ContainsKey(USER_TEAM))
                    {

                        Debug.Log("USER_TEAM " + USER_TEAM);
                    }
                    else
                    {


                        //TODO! THIS IS REALLY HARDCODED... if we change the initial team to another one it may arise issue

                        SetUserData(new Dictionary<string, string>(){
                         {USER_TEAM, JsonConvert.SerializeObject(new { teamName = "Worchester United", selectedTeam = 0 })}
                        },
                        UserDataPermission.Public,
                       false,
                       result => { Debug.Log("Reset value for " + USER_TEAM); });




                    }
                }


                if (request.Keys == null || request.Keys.Contains(HEROPATH))
                {
                    if (result.Data.ContainsKey(HEROPATH))
                    {
                        HeroPath = Convert.ToInt32(result.Data[HEROPATH].Value.ToString());
                        Debug.Log("HeroPath " + HeroPath);
                    }
                    else
                    {
                        HeroPath = 0;
                        SetUserData(new Dictionary<string, string>(){
               {HEROPATH, "0"}},
                    UserDataPermission.Public,
                    false,
                    result => { Debug.Log("Reset value for " + HEROPATH); });
                    }
                }

                TeamAuthRequested = result.Data.ContainsKey("ShareConsent") || result.Data.ContainsKey("ReceiveConsent");
                if (result.Data.ContainsKey("dateOfBirth"))
                {
                    DateOfBirth = result.Data["dateOfBirth"].Value;
                }

                onSuccess?.Invoke(result);
                OnGetUserData?.Invoke(result);
                // if (InventoryRes == null)
                //     RefreshInventory();

            }, e => { if (onError != null) onError?.Invoke(); });
        }

        public void GrantTFItems(List<string> items, Action onSuccess = null, Action onError = null)
        {
            PlayFabServerAPI.GrantItemsToUser(new PlayFab.ServerModels.GrantItemsToUserRequest { ItemIds = items, PlayFabId = LoginResult.PlayFabId }, x => { onSuccess?.Invoke(); }, e => { onError?.Invoke(); });


        }


        public void SetUserData(Dictionary<string, string> data,
                UserDataPermission permission = UserDataPermission.Private,
                bool saveToPublisher = false,
                System.Action<PlayFab.ClientModels.UpdateUserDataResult> onSuccess = null,
                System.Action<PlayFabError> onError = null)
        {
            PlayFabClientAPI.UpdateUserData(new PlayFab.ClientModels.UpdateUserDataRequest()
            {
                Data = data,
                Permission = permission
            }, result => { onSuccess?.Invoke(result); }, error =>
            {
                onError?.Invoke(error);
                //OnPlayFabError(error);
            });

            if (!saveToPublisher) return;

            PlayFabClientAPI.UpdateUserPublisherData(new PlayFab.ClientModels.UpdateUserDataRequest()
            {
                Data = data,
                Permission = UserDataPermission.Private,
            }, resultCallback => { }, onError);
        }

        public void RefreshInventory(Action onRefresh = null, bool requestTeamRosterUpdate = false)
            {
                PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(), (result) =>
                {
                    InventoryRes = result;
                    this.Inventory = result.Inventory;
                    ResetAndReloadAllCards();

                    GameEventsManager.Instance.OnCurrencyValueChanged?.Invoke(CurrencyType.COIN, result.VirtualCurrency["GD"]);
                    GameEventsManager.Instance.OnCurrencyValueChanged?.Invoke(CurrencyType.GEMS, result.VirtualCurrency["GE"]);
                    if (requestTeamRosterUpdate)
                        RefreshTeamUserInventory(this.Inventory, onRefresh);
                    else
                    {
                        onRefresh?.Invoke();
                    }
                    RefreshSuperPowers();

                }, (error) => Debug.Log("Error while refreshing inventory"));
            }

        // async Task<bool> UpdateAccountInfo(UserAccountInfo accountInfo = null)
        // {
        //     var tcs = new TaskCompletionSource<bool>();


        //     if (accountInfo == null)
        //     {
        //         GetAccountInfoRequest request = new GetAccountInfoRequest()
        //         {
        //             PlayFabId = accountInfo.PlayFabId,
        //         };
        //         PlayFabClientAPI.GetAccountInfo(request,
        //         result =>
        //         {
        //             tcs.SetResult(true);
        //             UserAccountInfo = result.AccountInfo;
        //             PlayerPrefs.SetString("SLL.LastUsername", UserAccountInfo.TitleInfo.DisplayName);

        //         },
        //         error =>
        //         {
        //             tcs.SetResult(false);
        //             Debug.Log($"[PlayFabManager - An error occurred while retrieving account info:{error.ErrorMessage}]");
        //             OnPlayFabFailed?.Invoke();
        //         }
        //         );
        //     }
        //     // else
        //     // {
        //     //     tcs.SetResult(true);
        //     //     UserAccountInfo = accountInfo;
        //     //     PlayerPrefs.SetString("SLL.LastUsername", UserAccountInfo.TitleInfo.DisplayName);

        //     // }

        //     return await tcs.Task;
        // }

    }


    #region helper classes
    [System.Serializable]
    public class DefaultCloudResponse
    {
        [SerializeField] public bool success;
        [SerializeField] public string data;
        [SerializeField] public int code;
        [SerializeField] public string message;

        public bool Success => success;
        public string Data => data;
        public int Code => code;
        public string Message => message;
    }

    [System.Serializable]
    public class DataWithMessageCloudResponse
    {
        public string message;
    }

    [System.Serializable]
    public class CheckInventoryAndReturnCloudResponse : DefaultCloudResponse
    {
        public new LoginInventoryResponse data;
        public new LoginInventoryResponse Data => data;
    }


    [System.Serializable]
    public class LoginInventoryResponse
    {
        public List<ItemInstance> teamRoster;
        public List<ItemInstance> inventory;
        public UserAccountInfo accountInfo;
        public List<CatalogItem> catalog;
        public string[] tags;
        public TagStatus tagStatus;
    }

    public enum TagStatus
    {
        accepted,
        haventAccepted,
        needToAsk,
        noRelatedTag
    }

    #endregion
}
