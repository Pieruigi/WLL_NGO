using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using WLL_NGO.Interfaces;

namespace WLL_NGO.UI
{
    public class GameButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        // [SerializeField]
        // string _name;
        // public string Name
        // {
        //     get { return _name; }
        // }

        bool value = false;
        bool oldValue = false;



        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        public void OnPointerDown(PointerEventData eventData)
        {
            oldValue = value;
            value = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            oldValue = value;
            value = false;
        }

        public bool GetButtonDown()
        {
            return value == true && oldValue == false;
        }

        public bool GetButtonUp()
        {
            return value == false && oldValue == true;
        }
        
        public bool GetButton()
        {
            return value;
        }   
    }
}