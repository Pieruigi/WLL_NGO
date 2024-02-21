using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WLL_NGO
{
    public class NetworkTimer
    {
        float timer;
        public float DeltaTick { get; private set; }

        public int CurrentTick { get; private set; }

        public NetworkTimer(float serverTickRate)
        {
            DeltaTick = 1f / serverTickRate;
        }

        public void Update(float deltaTime)
        {
            timer += deltaTime;
        }

        public bool TimeToTick()
        {
            if(timer >= DeltaTick)
            {
                timer -= DeltaTick;
                CurrentTick++;
                return true;

            }
            return false;
        }
    }

}
