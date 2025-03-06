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

        [SerializeField]
        List<Transform> kickOffHomeList = new List<Transform>();

        [SerializeField]
        List<Transform> kickOffAwayList = new List<Transform>();



        public Transform GetHomeSpawnPoint(int index)
        {
            return homeList[index];
        }

        public Transform GetAwaySpawnPoint(int index)
        {
            return awayList[index];
        }

        public Transform GetKickOffHomeSpawnPoint(int index)
        {
            return kickOffHomeList[index];
        }

        public Transform GetKickOffAwaySpawnPoint(int index)
        {
            return kickOffAwayList[index];
        }

        public List<Transform> GetHomeSpawnPoints()
        {
            return homeList;
        }

        public List<Transform> GetKickOffHomeSpawnPoints()
        {
            return kickOffHomeList;
        }

        public List<Transform> GetAwaySpawnPoints()
        {
            return awayList;
        }

        public List<Transform> GetKickOffAwaySpawnPoints()
        {
            return kickOffAwayList;
        }
    }

}
