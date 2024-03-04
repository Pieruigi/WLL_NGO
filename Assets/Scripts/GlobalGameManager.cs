using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WLL_NGO
{
    public class GlobalGameManager : SingletonPersistent<GlobalGameManager>
    {

        float maxBallSpeed = 35;
        public float MaxBallSpeed
        {
            get { return maxBallSpeed; }
        }

     
    }

}
