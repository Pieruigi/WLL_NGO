using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WLL_NGO
{
    public class PlayerSpawnPointManager : Singleton<PlayerSpawnPointManager>
    {
        
        [SerializeField]
        List<Transform> homeList = new List<Transform>();
        [SerializeField]
        List<Transform> awayList = new List<Transform>();


        public Transform GetHomeSpawnPoint(int index)
        {
            return homeList[index];
        }

        public Transform GetAwaySpawnPoint(int index)
        {
            return awayList[index];
        }
    }

}
