using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace WLL_NGO.Gameplay
{
    /// <summary>
    /// NotReady: waiting for all players to be ready
    /// </summary>
    public enum MatchState { NotReady }

    public class MatchController : NetworkBehaviour
    {
        //ushort numOfPlayers = 1;
        //public ushort NumberOfPlayers
        //{
        //    get { return numOfPlayers; }
        //}

        NetworkVariable<byte> matchState = new NetworkVariable<byte>((byte)MatchState.NotReady);

        private void Update()
        {
            if (IsServer)
                return;
            if (Input.GetKeyDown(KeyCode.Q))
            {
                NetworkManager.Singleton.Shutdown();
            }
        }

        private void FixedUpdate()
        {
            if (IsServer)
            {
                UpdateMatchState();
            }


        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            matchState.OnValueChanged += HandleOnMatchStateChanged;
        }

        /// <summary>
        /// Called when match state changes
        /// </summary>
        /// <param name="previousValue"></param>
        /// <param name="newValue"></param>
        private void HandleOnMatchStateChanged(byte previousValue, byte newValue)
        {
            // Do something
        }


        

        /// <summary>
        /// Called on server to process match states.
        /// </summary>
        void UpdateMatchState()
        {
            switch (matchState.Value)
            {
                case (byte)MatchState.NotReady:
                    // If all players are ready we can start the game
                    
                    break;
            }
        }

       
    }

}
