using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace WLL_NGO.Services
{

    public class GameServerDataFetcher// : Singleton<GameServerDataFetcher>
    {
        
        /// <summary>
        /// This function returns the full game catalog as json
        /// </summary>
        /// <returns></returns>
        public static async Task<FetchDataResponse<string>> FetchFullCatalog()
        {
            await Task.Delay(System.TimeSpan.FromSeconds(1));
            return new FetchDataResponse<string>(true, "catalog-json");
        }
    }

}
