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
    /// Before we can start the matchmaking listening routine we might need to load some data from extarnal sources ( for example the catalog from PlayFab ).
    /// </summary>
    public class ServerMatchmakerLauncher : MonoBehaviour
    {
        void Start()
        {
            Launch();
        }

        async void Launch()
        {
            // We start loading data and wait for all the loading tasks to complete before we launch the matchmaking linstening routine.
            List<Task<FetchDataResponse<string>>> tasks = new List<Task<FetchDataResponse<string>>>();
            tasks.Add(GameServerDataFetcher.FetchAll());
            await Task.WhenAll(tasks.ToArray());
            bool failed = false;
            foreach (var t in tasks)
            {
                if (!t.Result.Succeeded)
                {
                    // We failed for some reason.
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
                // Something goes wrong, just quit the application and release the cloud server.
                Application.Quit();
            }
            else
            {
                // Ok, we loaded all we need, just start the matchmaking routine.
#if NO_MM
                // We are testing connecting directly to a local server so we just simulate a matchmaking response
                ServerMatchmaker.OnMatchmakerPayload?.Invoke(new Unity.Services.Matchmaker.Models.MatchmakingResults() { });
#else
                // Start the matchmaker handler
                ServerMatchmaker.Instance.StartUp();
#endif
            }

        }

  
    }

}
