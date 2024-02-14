using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WLL_NGO
{
    public class PlayerSpawnPointManager : Singleton<PlayerSpawnPointManager>
    {
        [SerializeField]
        Transform homeGroup, awayGroup;

        List<Transform> homeList = new List<Transform>();
        List<Transform> awayList = new List<Transform>();


        private void Start()
        {
            for(int i=0; i<homeGroup.childCount; i++)
            {
                homeList.Add(homeGroup.GetChild(i));
                awayList.Add(awayGroup.GetChild(i));
            }
        }

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
