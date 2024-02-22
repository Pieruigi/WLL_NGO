using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WLL_NGO
{
    public class NetworkTimer
    {
        static NetworkTimer instance;
        public static NetworkTimer Instance
        {
            get
            { 
                if(instance == null) 
                    instance = new NetworkTimer(); 
                return instance;
            }
        }

        float timer;
        public float DeltaTick { get; private set; }

        public int CurrentTick { get; private set; }

        private NetworkTimer()
        {
            DeltaTick = 1f / Constants.ServerTickRate;
        }

        //public NetworkTimer(float serverTickRate)
        //{
        //    DeltaTick = 1f / serverTickRate;
        //}

        public void Reset()
        {
            timer = 0;
            CurrentTick = 0;
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
