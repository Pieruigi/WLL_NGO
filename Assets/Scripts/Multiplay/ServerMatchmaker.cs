#if UNITY_SERVER || UNITY_EDITOR || UNITY_STANDALONE || UNITY_STANDALONE_WIN
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Threading.Tasks;
using Unity.Services.Core;
using Unity.Services.Matchmaker.Models;
using Unity.Services.Multiplay;
using UnityEngine;
using UnityEngine.Events;

namespace WLL_NGO.Multiplay
{
    public class ServerMatchmaker : Singleton<ServerMatchmaker>
    {
        public static UnityAction<MatchmakingResults> OnMatchmakerPayload;
        public static UnityAction OnMatchmakerFailed;

        int maxPlayers = 2;
        
        private MatchmakingResults matchmakerPayload;
        private string allocationId;
        private MultiplayEventCallbacks serverCallbacks;
        private IServerEvents serverEvents;
        int allocationWaitingDelay = 1000; // In millis

        IServerQueryHandler serverQueryHandler;
        string serverName;


#if !NO_MM
        public async void StartUp()
        {
            await StartServices();
        }
#endif

        /// <summary>
        /// Sugg: move this to a dedicated exception manager class
        /// </summary>
        public void Shutdown()
        {
            Debug.Log("Server shut down");
            serverCallbacks.Allocate -= HandleOnMultiplayAllocation;
            serverEvents?.UnsubscribeAsync();
            matchmakerPayload = null;
        }

        /// <summary>
        /// Starts multiplay services
        /// </summary>
        /// <returns></returns>
        async Task StartServices()
        {
            // Initialize
            await UnityServices.InitializeAsync();
            try
            {
                serverName = Guid.NewGuid().ToString();
                serverQueryHandler = await MultiplayService.Instance.StartServerQueryHandlerAsync((ushort)maxPlayers, serverName, "n/a", "0", "n/a");
            }
            catch (Exception e)
            {
                Debug.Log($"Error while trying to set up the SQP service.\n{e}");
                Shutdown(); // We could also throw the exception to a dedicated class
                return;
            }

            // Try to get the matchmaker payload
            try
            {
                matchmakerPayload = null; // Clear
                Debug.Log("Waiting for matchmaking payload...");
                matchmakerPayload = await GetMatchmakerPayloadAsync(Constants.MatchmakerTimeout);

                if (matchmakerPayload != null)
                {
                    Debug.Log($"Got matchmaker payload:{matchmakerPayload}");

                    await MultiplayService.Instance.ReadyServerForPlayersAsync();
                    StartCoroutine(QueryServer());
                    OnMatchmakerPayload?.Invoke(matchmakerPayload);

                }
                else
                {
                    Debug.LogError($"Matchmaker payload is null");
                    OnMatchmakerFailed?.Invoke();
                    Shutdown();
                    return;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error while trying to set up the allocation & backfill.\n{e}");
                OnMatchmakerFailed?.Invoke();
                Shutdown();
                return;
            }

        }

        IEnumerator QueryServer()
        {
            serverQueryHandler.CurrentPlayers = 2;
            while (true)
            {
                serverQueryHandler.UpdateServerCheck();
                yield return new WaitForSeconds(1f);
            }
        }

        /// <summary>
        /// Try to get the matchmaker payload
        /// </summary>
        /// <param name="timeout"></param>
        /// <returns></returns>
        async Task<MatchmakingResults> GetMatchmakerPayloadAsync(int timeout)
        {
            Debug.Log("Trying to get payload...");
            var payloadTask = SubscribeAndAwaitMatchmakerAllocationAsync();
            if (await Task.WhenAny(payloadTask, Task.Delay(timeout)) == payloadTask)
            {
                return payloadTask.Result;
            }
            
            return null;
        }

        /// <summary>
        /// Subscribe matchmaking events and wait for the payload
        /// </summary>
        /// <returns></returns>
        async Task<MatchmakingResults> SubscribeAndAwaitMatchmakerAllocationAsync()
        {
            Debug.Log("Subscribe and await matchmaker allocation");
            allocationId = null; // Just reset
            serverCallbacks = new MultiplayEventCallbacks();
            serverCallbacks.Allocate += HandleOnMultiplayAllocation;
            serverEvents = await MultiplayService.Instance.SubscribeToServerEventsAsync(serverCallbacks);
            Debug.Log("Awaiting allocation...");
            await AwaitAllocationIdAsync(); 
            Debug.Log("Allocated, allocation id:" + allocationId);
            var mmPayload = await GetMatchmakerPayloadAllocationAsync();
            return mmPayload;
        }

        /// <summary>
        /// Get the actual payload and deserialize it into a model
        /// </summary>
        /// <returns></returns>
        async Task<MatchmakingResults> GetMatchmakerPayloadAllocationAsync()
        {
            try
            {
                Debug.Log("Get matchmaker allocation from json");
                var payloadAllocation = await MultiplayService.Instance.GetPayloadAllocationFromJsonAs<MatchmakingResults>();
                Debug.Log("payload received, match id:" + payloadAllocation.MatchId);
                var modelToJson = JsonConvert.SerializeObject(payloadAllocation, Formatting.Indented);
                Debug.Log($"{nameof(GetMatchmakerPayloadAllocationAsync)}:{modelToJson}");
                return payloadAllocation;
            }
            catch (Exception e)
            {
                Debug.Log($"Error while trying to get the matchmaking payload.\n{e}");
                return null;
            }
        }

        async Task AwaitAllocationIdAsync()
        {
            while (string.IsNullOrEmpty(allocationId))
            {
                var config = MultiplayService.Instance.ServerConfig;
                if (!string.IsNullOrEmpty(config.AllocationId) && string.IsNullOrEmpty(allocationId))
                {
                    allocationId = config.AllocationId;
                    break;
                }
                await Task.Delay(allocationWaitingDelay);
            }
        }

        void HandleOnMultiplayAllocation(MultiplayAllocation allocation)
        {
            serverCallbacks.Allocate -= HandleOnMultiplayAllocation;
            serverEvents?.UnsubscribeAsync();
            Debug.Log($"On multiplay allocation, allocation id:{allocation.AllocationId}");
            allocationId = allocation.AllocationId;
        }

        
    }

}
#else
using UnityEngine;
namespace WLL.Multiplay
{
    public class ServerManager : MonoBehaviour
    {
        private void Awake()
        {
            if (!Instance)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }
        public static ServerManager Instance { get; private set; }
    }
}


#endif