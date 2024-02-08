using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WLL_NGO.Multiplay
{
    public class MatchmakingPlayerData
    {
        public string PlayFabId { get; }

        public MatchmakingPlayerData(string playFabId)
        {
            PlayFabId = playFabId;
        }
    }

}
