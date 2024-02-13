using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using WLL_NGO.Multiplay;
using WLL_NGO.Netcode;
using WLL_NGO.Services;

namespace WLL_NGO
{
    /// <summary>
    /// Load data from external sources ( for example the whole catalog from playfab ) and then launch the matchmaker service on server.
    /// </summary>
    public class ServerMatchmakerLauncher : MonoBehaviour
    {
        void Start()
        {
            Launch();
        }

        async void Launch()
        {
            // Prefetch data ( for example the full catalog )
            // We load all needed data before we enter the lobby room
            List<Task<FetchDataResponse<string>>> tasks = new List<Task<FetchDataResponse<string>>>();
            tasks.Add(GameServerDataFetcher.FetchAll());
            await Task.WhenAll(tasks.ToArray());
            bool failed = false;
            foreach (var t in tasks)
            {
                if (!t.Result.Succeeded)
                {
                    failed = true;
                    break;
                }
                else
                {
                    Debug.Log($"Prefetch task result:{t.Result.Data}");
                }
            }

            if (failed)
            {
                // Something goes wrong, shutdown
                //ServerMatchmaker.Instance.Shutdown();
                Application.Quit();
            }
            else
            {
#if NO_MM
                // Just invoke the matchmaker payload result
                ServerMatchmaker.OnMatchmakerPayload?.Invoke(new Unity.Services.Matchmaker.Models.MatchmakingResults() { });
#else
                // Start matchmaker manager
                ServerMatchmaker.Instance.StartUp();
#endif
            }

        }

  
    }

}
