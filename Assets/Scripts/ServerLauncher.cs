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
    public class ServerLauncher : MonoBehaviour
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
                ServerManager.Instance.Shutdown();
                Application.Quit();
            }
            else
            {
#if NO_MM
                // Just invoke the matchmaker payload result
                ServerManager.OnMatchmakerPayload?.Invoke(new Unity.Services.Matchmaker.Models.MatchmakingResults() { });
#else
                // Start matchmaker manager
                ServerManager.Instance.StartUp();
#endif
            }

        }

        // Update is called once per frame
        void Update()
        {

        }
    }

}