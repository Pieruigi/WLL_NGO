using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace WLL_NGO
{
#if OLD
    public class NetworkTimer
    {
        //static NetworkTimer instance;
        //public static NetworkTimer Instance
        //{
        //    get
        //    {
        //        if (instance == null)
        //            instance = new NetworkTimer();
        //        return instance;
        //    }
        //}

        float timer;
        public float DeltaTick { get; private set; }

        public int CurrentTick { get; private set; }

        public NetworkTimer()
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
#else
    public class NetworkTimer: MonoBehaviour
    {
        public UnityAction OnTimeToTick;

        public static NetworkTimer Instance { get; private set; }
        

        float timer;
        public float DeltaTick { get; private set; }

        public int CurrentTick { get; private set; }

        bool playing = false;

        //public NetworkTimer()
        //{
        //    DeltaTick = 1f / Constants.ServerTickRate;
        //}

        private void Awake()
        {
            if(!Instance)
            {
                Instance = this;
                timer = 0f;
                CurrentTick = 0;
                DeltaTick = 1f / Constants.ServerTickRate;
            }
            else
            {
                Destroy(gameObject);
            }
        }
        

        public void Update()
        {
            if (!playing) 
                return; 
            timer += Time.deltaTime;
        }

        void FixedUpdate()
        {
            if (TimeToTick())
            {
                OnTimeToTick?.Invoke();
            }
        }

        

        bool TimeToTick()
        {
            if (timer >= DeltaTick)
            {
                timer -= DeltaTick;
                CurrentTick++;
                return true;

            }
            return false;
        }

        public void StartTimer()
        {
            playing = true;
        }
    }
#endif
}
