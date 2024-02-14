using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Matchmaker;
using Unity.Services.Matchmaker.Models;
using UnityEngine;
using UnityEngine.Events;

#if NO_MM

#endif
#if UNITY_EDITOR
using ParrelSync;
#endif

namespace WLL_NGO.Multiplay
{
    public class ClientMatchmaker : Singleton<ClientMatchmaker>
    {
        public static UnityAction<MultiplayAssignment> OnTicketAssigned;
        public static UnityAction OnTicketFailed;
        

        //public static ClientManager Instance { get; private set; }

        string ticketId;
        MultiplayAssignment assignment; // Holds match information

        protected override void Awake()
        {
            base.Awake();
#if !NO_MM
#pragma warning disable 4014
            MultiplayUtilities.SignInAsync(HandleOnSignedIn, HandleOnSignInFailed);
#pragma warning restore 4014
#endif
            
        }

        public void Play(GameMode gameMode)
        {
#if NO_MM
            //BoltClientManager.Instance.StartClientNoMM();
            OnTicketAssigned?.Invoke(new MultiplayAssignment("no_mm", null, MultiplayAssignment.StatusOptions.Found, "127.0.0.1", Constants.NoMatchmakingTestingPort));
#else
            string ticketName = "";
            switch (gameMode)
            {
                case GameMode.Powered:
                    ticketName = Constants.PoweredQueueName;
                    break;
                case GameMode.GoldenGoal:
                    ticketName = Constants.GoldenGoalQueueName;
                    break;
                case GameMode.Classic:
                    ticketName = Constants.ClassicQueueName;
                    break;
            }
            CreateTicket(ticketName);
#endif
        }

        public void PlayWithFriends()
        {
            CreateTicket(Constants.PlayWithFriendsQueueName);
            //MatchmakerService.Instance.GetTicketAsync();
        }

       

        async void CreateTicket(string queueName)
        {
            
            // Sign in
            await MultiplayUtilities.SignInAsync(HandleOnSignedIn, HandleOnSignInFailed);

            // Not authenticated
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                OnTicketFailed?.Invoke();
                return;
            }

            // Create ticket
            //var options = new CreateTicketOptions(queueName: "Test");
            var options = new CreateTicketOptions(queueName);
            
            List<Player> players = new List<Player>
            {
                new Player(GetInternalPlayerId(),new MatchmakingPlayerData(GetExternalPlayerId())),
                
            };

            var ticketResponse = await MatchmakerService.Instance.CreateTicketAsync(players, options);
            ticketId = ticketResponse.Id;
            Debug.Log($"Received ticked, id:{ticketId}");
            if (string.IsNullOrEmpty(ticketId))
            {
                Shutdown();
                OnTicketFailed?.Invoke();
            }
            else
            {
                Task pollTicketStatus = PollTicketStatus();// Task.Delay(Constants.ClientMatchmakingTimeout);
                if (await Task.WhenAny(pollTicketStatus, Task.Delay(Constants.ClientMatchmakingTimeout)) != pollTicketStatus)
                {
                    
                    if(assignment != null && assignment.Status != MultiplayAssignment.StatusOptions.Found)
                    {
                        Debug.Log("No match found, starting single player...");
                        Shutdown();
                        OnTicketFailed?.Invoke();
                    }
                        
                }
               
            }
                
        }

        public void Shutdown()
        {
            Debug.Log("Shutdown client");
            if (!string.IsNullOrEmpty(ticketId))
                MatchmakerService.Instance.DeleteTicketAsync(ticketId);

            ticketId = null;
        }


        /// <summary>
        /// Poll ticket status 
        /// </summary>
        /// <returns></returns>
        async Task PollTicketStatus()
        {
            Debug.Log("Poll ticket status");
            assignment = null;
            bool gotAssignment = false;
            float delay = 1f;
            do
            {
                await Task.Delay(System.TimeSpan.FromSeconds(delay));

                Debug.Log("Check ticket status");

                //if (string.IsNullOrEmpty(ticketId))
                //    return;

                var ticketStatus = await MatchmakerService.Instance.GetTicketAsync(ticketId);
                if (ticketStatus == null || ticketStatus.Type != typeof(MultiplayAssignment))
                    continue;


                assignment = ticketStatus.Value as MultiplayAssignment;
                
                switch (assignment.Status)
                {
                    case MultiplayAssignment.StatusOptions.Found:
                        gotAssignment = true;
                        OnTicketAssigned?.Invoke(assignment);
                        Debug.LogWarning($"Match allocated:{assignment.MatchId}");
                        break;
                    case MultiplayAssignment.StatusOptions.InProgress:
                        break;
                    case MultiplayAssignment.StatusOptions.Timeout:
                        gotAssignment = true;
                        Shutdown();
                        OnTicketFailed?.Invoke();
                        Debug.LogWarning($"Getting assignment timed out:{assignment.Message}");
                        break;
                    case MultiplayAssignment.StatusOptions.Failed:
                        gotAssignment = true;
                        Shutdown();
                        OnTicketFailed?.Invoke();
                        Debug.LogError($"Error getting assignment:{assignment.Message}");
                        break;
                    default:
                        Shutdown();
                        OnTicketFailed?.Invoke();
                        throw new InvalidOperationException();

                }
            }
            while (!gotAssignment && !string.IsNullOrEmpty(ticketId));
            
            
        }

        /// <summary>
        /// The player id from multiplay
        /// </summary>
        /// <returns></returns>
        string GetInternalPlayerId()
        {
            return AuthenticationService.Instance.PlayerId;
        }

        /// <summary>
        /// An external source player id, for example playfab, if any, otherwise the internal player id
        /// </summary>
        /// <returns></returns>
        string GetExternalPlayerId()
        {
            // Try to get the player id from the authentication manager ( ex playfab )
            var authManager = new List<Transform>(FindObjectsOfType<Transform>()).Find(t => t.GetComponent<Interfaces.IExternalAuthenticator>() != null);
            Debug.Log($"AuthManager:{authManager}");
            if (authManager)
                return (authManager.GetComponent<Interfaces.IExternalAuthenticator>()).GetPlayerId();
            else
                return GetInternalPlayerId();
        }

        void HandleOnSignInFailed(RequestFailedException rfe)
        {
            AuthenticationService.Instance.SignInFailed -= HandleOnSignInFailed;
            AuthenticationService.Instance.SignedIn -= HandleOnSignedIn;
            Debug.LogException(rfe);
        }

        void HandleOnSignedIn()
        {
            AuthenticationService.Instance.SignInFailed -= HandleOnSignInFailed;
            AuthenticationService.Instance.SignedIn -= HandleOnSignedIn;
            Debug.Log("Player signed in");
        }

    }

}
