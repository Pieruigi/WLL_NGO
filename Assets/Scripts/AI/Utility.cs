using System.Collections.Generic;
using UnityEngine;

namespace WLL_NGO.AI
{
    public class Utility 
    {

        public static PlayerAI GetTheClosestPlayer(Vector3 position, List<PlayerAI> playerList)
        {
            float dist = 0;
            PlayerAI ret = null;
            foreach (PlayerAI player in playerList)
            {
                float d = Vector3.ProjectOnPlane(player.Position - position, Vector3.up).magnitude;
                if (!ret || d < dist)
                {
                    ret = player;
                    dist = d;
                }
            }

            return ret;
        }

    }

}
