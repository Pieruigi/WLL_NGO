using System.Collections;
using System.Collections.Generic;
using Unity.Networking.Transport;
using UnityEngine;

namespace WLL_NGO
{
    public class InputTimingUtility
    {
        public const float GoodTimingChargeLimit = 0.5f;

        public const float PerfectTimingChargeLimit = .7f;

       
        public static ShotTiming GetOnTheFlyTiming(float value)
        {

            if (value < 0.25f)
                return ShotTiming.Normal;
            else if (value < 0.375f)
                return ShotTiming.Good;
            else if (value < .625f)
                return ShotTiming.Perfect;
            else if (value < .75f)
                return ShotTiming.Good;
            else
                return ShotTiming.Bad;


        }

        public static float GetOnTheFlyNormalizedTimingValue(Vector3 initialPosition, Vector3 targetPosition, Vector3 ballPosition)
        {
            targetPosition.y = 0;
            initialPosition.y = 0;
            ballPosition.y = 0;
            float lerp = (ballPosition - initialPosition).magnitude / (targetPosition - initialPosition).magnitude;
            return lerp;
        }

        public static ShotTiming GetShotTimingByCharge(float charge)
        {
            if (charge <= GoodTimingChargeLimit)
                return ShotTiming.Good;
            else if (charge <= PerfectTimingChargeLimit)
                return ShotTiming.Perfect;
            else
                return ShotTiming.Bad;
                
        }

        
        
    }

}
