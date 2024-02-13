using System.Collections;
using System.Collections.Generic;
using UnityEditor.Build;
using UnityEngine;

namespace WLL_NGO
{
    public class NetworkTimer
    {
        float timer;
        public float MinTimerBetweenTicks { get; private set; }

        public int CurrentTick { get; private set; }

        public NetworkTimer(float serverTickRate)
        {
            MinTimerBetweenTicks = 1f / serverTickRate;
        }

        public void Update(float deltaTime)
        {
            timer += deltaTime;
        }

        public bool TimeToTick()
        {
            if(timer >= MinTimerBetweenTicks)
            {
                timer -= MinTimerBetweenTicks;
                CurrentTick++;
                return true;

            }
            return false;
        }
    }

}
