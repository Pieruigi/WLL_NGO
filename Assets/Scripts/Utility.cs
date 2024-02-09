using ParrelSync;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WLL_NGO
{
    public class Utility
    {
        public static bool IsDedicatedServer()
        {
#if NO_MM
#if UNITY_EDITOR
            if (ClonesManager.GetArgument().Contains(Constants.DedicatedServerArg))
                return true;
#else
            if (new List<string>(System.Environment.GetCommandLineArgs()).Contains(Constants.DedicatedServerArg))
                return true;
#endif
#else
            if (new List<string>(System.Environment.GetCommandLineArgs()).Contains(Constants.DedicatedServerArg))
                return true;
#endif
            else
                return false;
        }
    }

}
