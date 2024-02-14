using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using WLL_NGO.Interfaces;
using WLL_NGO.Netcode;

namespace WLL_NGO
{

    public struct InputData
    {
        public Vector2 joystick;
        public bool button1;
        public bool button2;
        public bool button3;
    }

    [System.Serializable]
    public class HumanInputHandler : IInputHandler
    {
        public InputData GetInput()
        {
            return new InputData()
            {
                joystick = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")).normalized,
                button1 = false,
                button2 = false,
                button3 = false
            };
        }
    }


    public class PlayerInputManager: Singleton<PlayerInputManager>
    {
        IInputHandler humanInputHandler;

        protected override void Awake()
        {
            base.Awake();
            if (NetworkManager.Singleton.IsClient)
            {
                Debug.Log("Creating the human input handler");
                // Create a human input handler
                humanInputHandler = new HumanInputHandler();
            }
        }

        private void OnEnable()
        {
            PlayerController.OnSpawned += HandleOnPlayerControllerSpawned;
        }

        /// <summary>
        /// TO REMOVE. We need to check the team for the selected player
        /// </summary>
        /// <param name="pc"></param>
        void HandleOnPlayerControllerSpawned(PlayerController pc)
        {
            
            pc.SetInputHandler(humanInputHandler);
        }
    }

}
