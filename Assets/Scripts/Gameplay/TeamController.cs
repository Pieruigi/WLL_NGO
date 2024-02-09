using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using WLL_NGO.Services;

namespace WLL_NGO.Gameplay
{
    public class TeamController : NetworkBehaviour
    {
        static TeamController homeTeamController, awayTeamController;
        public static TeamController HomeTeam
        {
            get { return homeTeamController; }
        }
        public static TeamController AwayTeam
        {
            get { return awayTeamController; }
        }

        //private NetworkVariable<bool> home = new NetworkVariable<bool>();
        [SerializeField]
        bool home;

        private void Awake()
        {
            if (home)
                homeTeamController = this;
            else
                awayTeamController = this;
        }

       

        
    }

}
