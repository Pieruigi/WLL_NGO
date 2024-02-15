using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using WLL_NGO.Interfaces;
using WLL_NGO.Netcode;

namespace WLL_NGO
{
    [System.Serializable]
    public struct InputData
    {
        [SerializeField]
        public Vector2 joystick;
        [SerializeField]
        public bool button1;
        [SerializeField]
        public bool button2;
        [SerializeField]
        public bool button3;

        public override string ToString()
        {
            return $"[Input joystick:{joystick}, button1:{button1}, button2:{button2}, button3:{button3}]";
        }
    }

    [System.Serializable]
    public class HumanInputHandler : IInputHandler
    {
        public InputData GetInput()
        {
            return new InputData()
            {
                joystick = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")).normalized,
                button1 = Input.GetKey(KeyCode.Keypad1),
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
            if(humanInputHandler == null)
            {
                
                if (NetworkManager.Singleton.IsClient)
                {
                    // Create a human input handler
                    humanInputHandler = new HumanInputHandler();
                }
            }
            
            pc.SetInputHandler(humanInputHandler);
        }
    }

}
