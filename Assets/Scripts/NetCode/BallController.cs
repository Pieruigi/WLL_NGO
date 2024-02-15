using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace WLL_NGO.Netcode
{
    public class BallController : SingletonNetwork<BallController>
    {
        /// <summary>
        /// Stored value is the PlayerController.NetworkObjectId
        /// </summary>
        NetworkVariable<ulong> ownerNetObjId = new NetworkVariable<ulong>(0);

        PlayerController owner = null;

        Rigidbody rb;

        protected override void Awake()
        {
            base.Awake();
            rb = GetComponent<Rigidbody>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Q))
            {

                if(ownerNetObjId.Value == 0)
                {
                    PlayerController pc = FindObjectsOfType<PlayerController>().Where(p=>p.OwnerClientId == NetworkManager.Singleton.LocalClientId).First();
                    Debug.Log($"Ball - pc:{pc.PlayerInfo}");
                    ownerNetObjId.Value = pc.NetworkObjectId;
                }
                else
                {

                    ownerNetObjId.Value = 0;
                }
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            // Owner change event handler
            ownerNetObjId.OnValueChanged += HandleOnOwnerChanged;
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            // Owner change event handler
            ownerNetObjId.OnValueChanged -= HandleOnOwnerChanged;
        }
        
        void HandleOnOwnerChanged(ulong oldValue, ulong newValue)
        {
            Debug.Log($"Owner changed:{oldValue}->{newValue}");
            owner = newValue > 0 ? PlayerControllerManager.Instance.GetPlayerCotrollerByNetworkObjectId(newValue) : null;
        }

        public void Shoot(PlayerController player, Vector3 force)
        {
            // You can not shoot the ball it's controlled by another player
            if (owner != null && owner != player) return;
            
            // Shoot
            rb.AddForce(force, ForceMode.VelocityChange);
        }
    }

}
