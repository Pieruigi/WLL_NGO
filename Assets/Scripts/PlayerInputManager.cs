using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Markup;
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
        public bool button3; // Power up
        [SerializeField]
        public bool button4; // Tap screen

        public InputData(PlayerController.InputPayload inputPayload)
        {
            joystick = inputPayload.inputVector;
            button1 = inputPayload.button1;
            button2 = inputPayload.button2;
            button3 = inputPayload.button3;
            button4 = inputPayload.button4;
        }

        public static Vector2 ToInputDirection(Vector3 worldDirection)
        {
            return new Vector2(worldDirection.x, worldDirection.z).normalized;
        }

        public override string ToString()
        {
            return $"[Input joystick:{joystick}, button1:{button1}, button2:{button2}, button3:{button3}]";
        }
    }

    //TODO: we can keep just one handler 
    [System.Serializable]
    public class InputHandler : IInputHandler
    {
        Vector2 joystick;
        public Vector2 Joystick{ get{ return joystick; }}
       
        bool button1, button2, button3, button4;
        public bool Button1{ get{ return button1; }}
        public bool Button2{ get{ return button2; }}
        public bool Button3{ get{ return button3; }}
        public bool Button4{ get{ return button4; }}

        public InputData GetInput()
        {

            return new InputData()
            {
                // joystick = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")).normalized,
                // button1 = Input.GetKey(KeyCode.Keypad1),
                // button2 = Input.GetKey(KeyCode.Keypad2),
                // button3 = false
                joystick = this.joystick,
                button1 = this.button1,
                button2 = this.button2,
                button3 = this.button3,
                button4 = this.button4
            };
        }

        public void SetButton1(bool value)
        {
            button1 = value;
        }

        public void SetButton2(bool value)
        {
            button2 = value;
        }

        public void SetButton3(bool value)
        {
            button3 = value;
        }

        public void SetButton4(bool value)
        {
            button4 = value;
        }

        public void SetJoystick(Vector2 value)
        {
            joystick = value;
        }

        public void ResetInput()
        {
            joystick = Vector2.zero;
            button1 = button2 = button3 = button4 = false;
        }
    }

    
    public class PlayerInputManager: Singleton<PlayerInputManager>
    {
       
        private void OnEnable()
        {
          
            PlayerController.OnSpawned += HandleOnPlayerControllerSpawned;
            //TeamController.OnSelectedPlayerChanged += HandleOnSelectedPlayerChanged;
        }

        private void OnDisable()
        {
            PlayerController.OnSpawned -= HandleOnPlayerControllerSpawned;
            //TeamController.OnSelectedPlayerChanged -= HandleOnSelectedPlayerChanged;
        }

       

        /// <summary>
        /// TO REMOVE. We need to check the team for the selected player
        /// </summary>
        /// <param name="pc"></param>
        void HandleOnPlayerControllerSpawned(PlayerController pc)
        {
    
            pc.SetInputHandler(new InputHandler());
        }

       
    }

}
