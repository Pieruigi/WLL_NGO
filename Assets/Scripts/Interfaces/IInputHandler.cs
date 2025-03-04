using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WLL_NGO.Interfaces
{
    public interface IInputHandler
    {
        public InputData GetInput();

        public void SetJoystick(Vector2 value);

        public void SetButton1(bool value);

        public void SetButton2(bool value);

        public void SetButton3(bool value);
        
    }

}
