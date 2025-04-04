using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;
using PlayFab.ClientModels;
using PlayFab.MultiplayerModels;
using Unity.Services.Matchmaker.Models;
using UnityEngine;
using WLL_NGO.Services;

namespace WLL_NGO
{
    public class GlobalGameManager : SingletonPersistent<GlobalGameManager>
    {

        float maxBallSpeed = 35;
        public float MaxBallSpeed
        {
            get { return maxBallSpeed; }
        }


        public List<TeamInfo> OwnedTeams { get; private set; } = new List<TeamInfo>();
        public List<TeamInfo> Teams { get; private set; } = new List<TeamInfo>();

        public UserTeam CurrentTeamInfo { get; private set; }

        public TeamRoster CurrentTeamRoster { get; private set; }

        public const string TeamIdKey = "TeamID";



        void ClearTeams()
        {
            OwnedTeams.Clear();
            Teams.Clear();
            CurrentTeamInfo = null;
            CurrentTeamRoster = null;
        }

        void AddTeam(TeamInfo teamInfo)
        {
            if (teamInfo != null && !Teams.Contains(teamInfo))
            {
                Debug.Log($"[GlobalGameManager - Adding team: {teamInfo}]");
                Teams.Add(teamInfo);
                teamInfo.InitTeam();
            }
        }

        void AddOwnedTeam(TeamInfo teamInfo)
        {
            if (teamInfo != null && !OwnedTeams.Contains(teamInfo))
            {
                Debug.Log($"[GlobalGameManager - Adding owned team: {teamInfo}]");
                OwnedTeams.Add(teamInfo);
                teamInfo.InitTeam();
            }
        }

        void SortOwnedTeams()
        {
            OwnedTeams = OwnedTeams.OrderBy(y => y.teamID).ToList();

        }

        void SortTeams()
        {
            Teams = Teams.OrderBy(y => y.teamID).ToList();
        }

        void SetCurrentUserTeam(UserTeam userTeam = null)
        {
            Debug.Log("TEST - OwnedTeams.Count" + OwnedTeams.Count);

            if (userTeam != null)
                CurrentTeamInfo = userTeam;
            else
                CurrentTeamInfo = new UserTeam() { selectedTeam = OwnedTeams.FindIndex(0, OwnedTeams.Count, (x => x.name == "Worchester United")) % OwnedTeams.Count };

            var team = OwnedTeams[CurrentTeamInfo.selectedTeam % OwnedTeams.Count];
            CurrentTeamInfo.teamName = team.name;
            Debug.Log($"[GLOBAL_GAME_MANAGER - Setting team:{CurrentTeamInfo}]");
            PlayerPrefs.SetInt(TeamIdKey, CurrentTeamInfo.selectedTeam % OwnedTeams.Count);

        }

        public void InitializeTeamRoster(List<ItemInstance> teamCards, Action OnSuccessCallback = null, Action OnErrorCallback = null)
        {
            CurrentTeamRoster = new TeamRoster(teamCards, OnSuccessCallback, OnErrorCallback);

        }

        public void SetUpTeams()
        {
            ClearTeams();

            foreach (var team in PlayFabManager.Instance.Teams)
            {
                Debug.Log($"[PlayFabManager - Team found:{team.DisplayName}]");

                // Create a new TeamInfo object and set its properties
                TeamInfo newteam = new TeamInfo(team);

                // Add team to global game manager data
                AddTeam(newteam);

                if (PlayFabManager.Instance.Inventory.Exists(t => t.ItemId == team.ItemId))
                {
                    AddOwnedTeam(newteam);
                }
                else
                {
                    Debug.Log($"[PlayFabManager - Team not owned:{team.DisplayName}]");
                }
            }

            SortTeams();


            SortOwnedTeams();

            // Set up current team
            var userData = PlayFabManager.Instance.LoginResult.InfoResultPayload.UserData;
            if (userData != null && userData.ContainsKey(PlayFabManager.UserTeamKey))
            {
                var json = userData[PlayFabManager.UserTeamKey].Value;

                var team = JsonConvert.DeserializeObject<UserTeam>(json);
                if (team != null)
                {
                    Debug.Log($"[PlayFabManager - User team found:{team.teamName}, selectedTeam:{team.selectedTeam}]");
                    SetCurrentUserTeam(team);
                }
                else
                {
                    Debug.Log($"[PlayFabManager - User team not found, setting default team]");
                    SetCurrentUserTeam();
                }
            }
            else
            {
                Debug.Log($"[PlayFabManager - User data not found, setting default team]");
                SetCurrentUserTeam();
            }
        }

    }






    [Serializable]
    public class TeamInfo
    {
        public string name;

        public PlayFab.ClientModels.CatalogItem Team;

        public List<PlayFab.ClientModels.CatalogItem> TeamMembers;

        public int teamID;

        public int teamuniformID;
        public int teamuniform2ID;
        public string FirstUniform;
        public string SecondUniform;
        public string TeamBanner;
        public string TeamFlag;
        public int teamKeeperID;
        public int teamKeeper2ID;
        Action<bool, Sprite> onSuccess;
        public Sprite teamflag;
        public Texture2D Keeper;
        public Texture2D Defender;
        public Texture2D Support;
        public Texture2D Sniper;

        public TeamInfo(CatalogItem team)
        {
            Team = team;
            name = team.DisplayName;
            TeamMembers = new List<CatalogItem>();

            var dict = JsonConvert.DeserializeObject<UniformSet>(team.CustomData);
            FirstUniform = dict.firstUniform;
            SecondUniform = dict.secondUniform;

            teamID = dict.TeamID;
            teamuniformID = dict.Team1ID;
            teamuniform2ID = dict.Team2ID;
            teamKeeper2ID = dict.TeamKeeper2ID;
            teamKeeperID = dict.TeamKeeperID;
            TeamFlag = team.ItemImageUrl;

            // Add team members
            foreach (var p in team.Bundle.BundledItems)
            {
                var teamMember = PlayFabManager.Instance.CatalogItems.Find(k => k.ItemId == p);
                string example = teamMember.CustomData;
                TeamMembers.Add(teamMember);
            }
        }

        // TODO: Remove comments 
        public void InitTeam()
        {

            var kp = TeamMembers[(int)PlayerRole.GK];
            //TexturesCacheManager.GetTextureAddressable(string.Format(GlobalGameManager.PlayersUrlFormat, kp.ItemId), (texture) => Keeper = texture);

            var dp = TeamMembers[(int)PlayerRole.DF];
            //TexturesCacheManager.GetTextureAddressable(string.Format(GlobalGameManager.PlayersUrlFormat, dp.ItemId), (texture) => Defender = texture);

            var sp = TeamMembers[(int)PlayerRole.MD];
            //TexturesCacheManager.GetTextureAddressable(string.Format(GlobalGameManager.PlayersUrlFormat, sp.ItemId), (texture) => Support = texture);

            var sn = TeamMembers[(int)PlayerRole.AT];
            //TexturesCacheManager.GetTextureAddressable(string.Format(GlobalGameManager.PlayersUrlFormat, sn.ItemId), (texture) => Sniper = texture);


            // TexturesCacheManager.GetTextureAddressable(TeamFlag, (sprite) =>
            // {
            //     teamflag = Sprite.Create(sprite, new Rect(0, 0, sprite.width, sprite.height), Vector2.one / 2);
            //     onSuccess?.Invoke();
            // });


        }



        override public string ToString()
        {
            return $"TeamInfo: {name} - {teamID} - {teamuniformID} - {teamuniform2ID} - {FirstUniform} - {SecondUniform} - {TeamBanner} - {TeamFlag} - {teamKeeperID} - {teamKeeper2ID}";
        }
    }

    public class UniformSet
    {
        public string firstUniform;
        public string secondUniform;
        public int TeamID;

        public int Team1ID;
        public int TeamKeeperID;

        public int Team2ID;
        public int TeamKeeper2ID;
    }

    public class UserTeam
    {
        public string teamName;

        public int selectedTeam; // This is the team id

        override public string ToString()
        {
            return $"UserTeam: {teamName} - {selectedTeam}";
        }
    }

    public class TeamRoster
    {
        public List<ItemInstance> ItemInstances { get; private set; } = new List<ItemInstance>();

        public TeamRoster(List<ItemInstance> teamCards, Action OnSuccessCallback = null, Action OnErrorCallback = null)
        {
            UpdateTeam(teamCards, false, OnSuccessCallback, OnErrorCallback);
        }


        public void UpdateTeam(List<ItemInstance> newCards, bool sendToPlayfab = false, Action OnSuccess = null, Action OnError = null)
        {
            ItemInstances.Clear();

            //todo handle nft cards
            // Next step is done to fill out remaining data about card in 
            foreach (var card in newCards)
            {
                ItemInstance inventoryItem = PlayFabManager.Instance.Inventory.Where(i => i.ItemInstanceId == card.ItemInstanceId).FirstOrDefault();
                ItemInstances.Add(inventoryItem);
            }


            // sendToPlayfab should be used only when exiting team roster UI, not after every change
            if (sendToPlayfab)
            {
                PlayFabManager.Instance.UpdatePlayerTeamRoster(ItemInstances, OnSuccess, OnError);
            }
            else
            {
                OnSuccess?.Invoke();
            }
        }

        override public string ToString()
        {
            return $"TeamRoster: {string.Join(", ", ItemInstances.Select(i => i.ItemInstanceId))}";
        }
    }

}
