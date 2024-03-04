#if UNITY_EDITOR
using ParrelSync;
#endif
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

        public static ushort GetPortFromCommandLineArgs()
        {
#if !UNITY_EDITOR
            string[] args = System.Environment.GetCommandLineArgs();
#else
            string[] args = ClonesManager.GetArgument().Split(" ");
#endif

            for (int i = 0; i < args.Length; i++)
            {
                if (Constants.ServerPortArg.Equals(args[i]) && args.Length > i + 1)
                {
                    return ushort.Parse(args[i + 1]); // Port number is sent via cmd by the matchmaking
                }
            }

            return 0;
        }


    }


    

}
