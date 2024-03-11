using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WLL_NGO
{
    public class GameFieldInfo
    {
        static readonly float[] size = new float[] { /*width*/20f, /*length*/30f };
        static readonly float[] areaSize = new float[] { 10f, 7f };

        public static float GetFieldWidth()
        {
            return size[0];
        }

        public static float GetFieldLength()
        {
            return size[1];
        }

        public static float GetAreaWidth()
        {
            return areaSize[0];
        }

        public static float GetAreaLength()
        {
            return areaSize[1];
        }
    }

}
