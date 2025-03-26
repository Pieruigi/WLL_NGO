using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using WLL_NGO.Netcode;

namespace WLL_NGO
{
    public abstract class PowerUpController : MonoBehaviour
    {
        PlayerController user;

        public PlayerController User { get { return user; } }

        public abstract void Launch();

        public void Initialize(PlayerController user)
        {
            this.user = user;
        }

        protected virtual void Awake()
        {

        }

        protected virtual void Start()
        {
            //Launch();
        }
        
    }
    
}
