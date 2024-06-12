#if UNITY_EDITOR
using ParrelSync;
#endif
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WLL_NGO.Netcode;

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


        //public static Vector2 ToInputDirection(Vector3 worldDirection)
        //{
        //    return new Vector2(worldDirection.x, worldDirection.z).normalized;
        //}

        //public static InputData ToInputData(PlayerController.InputPayload inputPayload)
        //{
        //    return new InputData() { joystick = inputPayload.inputVector, button1 = inputPayload.button1, button2 = inputPayload.button2, button3 = inputPayload.button3 };
        //}

        //public static PlayerController.InputPayload ToInputPayload(InputData inputData, int tick)
        //{
        //    return new PlayerController.InputPayload() { inputVector = inputData.joystick, button1 = inputData.button1, button2 = inputData.button2, button3 = inputData.button3, tick = tick };
        //}


    }


    

}
