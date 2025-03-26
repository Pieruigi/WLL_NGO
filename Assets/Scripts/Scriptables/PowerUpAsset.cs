using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WLL_NGO.Scriptables
{
    public class PowerUpAsset : ScriptableObject
    {
        public const string ResourceFolder = "PowerUp";

#if !UNITY_SERVER
        [SerializeField]
        Sprite icon;
        public Sprite Icon
        {
            get { return icon; }
        }

        [SerializeField]
        AudioClip pickUpClip;
        public AudioClip PickUpClip
        {
            get { return pickUpClip; }
        }
#endif

        [SerializeField]
        PowerUpController controllerPrefab;
        public PowerUpController ControllerPrefab
        {
            get { return controllerPrefab; }
        }
    }
        
}
