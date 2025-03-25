using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using DG.Tweening;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;

namespace WLL_NGO.Netcode
{
    public class SportBag : NetworkBehaviour
    {
    #if !UNITY_SERVER
        [SerializeField]
        GameObject sportBagMeshPrefab;

        [SerializeField]
        Material localMaterial;

        [SerializeField]
        Material remoteMaterial;

        [SerializeField]
        Material bothMaterial;

        [SerializeField]
        int materialIndex = 0;

        GameObject sportBagMesh;
    #endif

        NetworkVariable<byte> type = new NetworkVariable<byte>(default);

        NetworkVariable<byte> powerUpType = new NetworkVariable<byte>(default);

        NetworkVariable<byte> state = new NetworkVariable<byte>((byte)SportBagState.NotReady);

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

#if !UNITY_SERVER
            // Only client has mesh
            if (IsClient)
            {
                sportBagMesh = Instantiate(sportBagMeshPrefab, transform);
                sportBagMesh.transform.localPosition = Vector3.zero;

                if (PlayerInfoManager.Instance.GetLocalPlayerInfo().Home)
                    sportBagMesh.transform.localRotation = Quaternion.Euler(0, 0, 0);
                else
                    sportBagMesh.transform.localRotation = Quaternion.Euler(0, 180, 0);

                //TODO: check rotation for away client

                var local = PlayerInfoManager.Instance.GetLocalPlayerInfo();
                if (type.Value == (int)SportBagType.Both)
                {
                    SetMaterial(bothMaterial);
                }
                else
                {
                    if ((local.Home && type.Value == (byte)SportBagType.Home) || (!local.Home && type.Value == (byte)SportBagType.Away))
                        SetMaterial(localMaterial);
                    else
                        SetMaterial(remoteMaterial);

                }


            }
#endif
            // State callback
            state.OnValueChanged += HandleOnStateChanged;

            GetComponent<Collider>().enabled = false; // No trigger while falling down
            

            if (IsServer)
            {
                transform.DOMoveY(0, 3f, false).onComplete += () => { state.Value = (byte)SportBagState.Ready; };
            }
        }

        private void HandleOnStateChanged(byte previousValue, byte newValue)
        {
            switch (state.Value)
            {
                case (byte)SportBagState.Ready:
                    GetComponent<Collider>().enabled = true;
                    break;
                case (byte)SportBagState.PickedUp:
                    GetComponent<Collider>().enabled = false;
                    if (IsServer) Despawn();
                    if (IsClient) DoPickedUpFx();
                    break;
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
        }

        void OnTriggerStay(Collider other)
        {
            if (state.Value != (byte)SportBagState.Ready) return;

            if (!other.CompareTag(Tags.Player)) return;

            PlayerController player = other.GetComponent<PlayerController>();

            TryPickUp(player);
            
        }

        void TryPickUp(PlayerController player)
        {
            if (state.Value != (byte)SportBagState.Ready) return;

            bool succeeded = IsRightPicker(player);

            if (IsServer) // Do logic
            {
                if(succeeded) DoPickUp(player);
            }

            if (IsClient) // Do fx
            {
                if (succeeded) DoPickedUpFx();
                else DoWrongPickerFx();
            }
        }

        bool IsRightPicker(PlayerController player)
        {
            if (type.Value == (byte)SportBagType.Both) return true;

            TeamController team = TeamController.GetPlayerTeam(player);

            if (PowerUpManager.Instance.HasReachedMaxPowerUps(team)) return false;

            if (type.Value == (byte)SportBagType.Home && team.Home) return true;

            if (type.Value == (byte)SportBagType.Away && !team.Home) return true;

            return false;
        }

        void DoPickUp(PlayerController player)
        {
            var team = TeamController.GetPlayerTeam(player);
            
            Despawn();
        }

        async void Despawn()
        {
            if (!IsServer) return;
            await Task.Delay(TimeSpan.FromSeconds(3f));
            GetComponent<NetworkObject>().Despawn();
        }

        void DoPickedUpFx()
        {
            if (!IsClient) return;    
        }

        void DoWrongPickerFx()
        {
            if (!IsClient) return;
        }

#if !UNITY_SERVER
        void SetMaterial(Material material)
        {
            var rend = sportBagMesh.GetComponentInChildren<MeshRenderer>();
            var mats = rend.materials;
            mats[materialIndex] = material;
            rend.materials = mats;
        }
    #endif

        public void Initialize(SportBagType type, PowerUpType powerUpType)
        {
            this.type.Value = (byte)type;
            this.powerUpType.Value = (byte)powerUpType;

        }


    }
    
}
