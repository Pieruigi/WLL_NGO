using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using DG.Tweening;
using Unity.Barracuda;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEditor.Rendering;
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

        Animator animator;
    #endif

        NetworkVariable<byte> type = new NetworkVariable<byte>(default);

        NetworkVariable<FixedString32Bytes> powerUpName = new NetworkVariable<FixedString32Bytes>(default);

        NetworkVariable<byte> state = new NetworkVariable<byte>((byte)SportBagState.NotReady);

        float lifeTime = 10f;

        void Update()
        {
            if (!IsSpawned) return;

            if (lifeTime > 0)
            {
                lifeTime -= Time.deltaTime;
                if (lifeTime <= 0)
                {
                    PowerUpManager.Instance.DespawnPackage(this);
                }
            }   
        }

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

                // Get animator
                animator = sportBagMesh.GetComponentInChildren<Animator>();

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

            bool succeeded = IsRightPicker(player);

            if (IsServer)
            {
                if (succeeded) PickUp(player);
            }

#if !UNITY_SERVER
            if (IsClient)
            {
                if (succeeded) PlayPickUpSucceededFx();
                else PlayPickUpFailedFx();
            }

#endif
        

            
        }

     

        bool IsRightPicker(PlayerController player)
        {
            if (player.GetState() != (int)PlayerState.Normal) return false; 

            if (type.Value == (byte)SportBagType.Both) return true;

            TeamController team = TeamController.GetPlayerTeam(player);

            if (PowerUpManager.Instance.HasReachedMaxPowerUps(team)) return false;

            if (type.Value == (byte)SportBagType.Home && team.Home) return true;

            if (type.Value == (byte)SportBagType.Away && !team.Home) return true;

            return false;
        }

        void PickUp(PlayerController player)
        {
            
            if (!IsServer) return;

            state.Value = (byte)SportBagState.PickedUp;

            // Get the player team
            var team = TeamController.GetPlayerTeam(player);

            // Add the power up to the team
            PowerUpManager.Instance.Push(team, powerUpName.Value.ToString());

            Despawn();
        }

        async void Despawn()
        {
            if (!IsServer) return;
            await Task.Delay(TimeSpan.FromSeconds(3f));
            GetComponent<NetworkObject>().Despawn();
        }

#if !UNITY_SERVER

        void PlayPickUpSucceededFx()
        {
            if (!IsClient) return;    
        }

        void PlayPickUpFailedFx()
        {
            if (!IsClient) return;
        }

        void SetMaterial(Material material)
        {
            var rend = sportBagMesh.GetComponentInChildren<MeshRenderer>();
            var mats = rend.materials;
            mats[materialIndex] = material;
            rend.materials = mats;
        }
    #endif

        public void Initialize(SportBagType type, string powerUpName)
        {
            this.type.Value = (byte)type;
            this.powerUpName.Value = (FixedString32Bytes)powerUpName;

        }


    }
    
}
