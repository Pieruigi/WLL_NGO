using System.Collections;
using System.Collections.Generic;
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



        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

#if !UNITY_SERVER
            // Only client has mesh
            if (IsClient)
            {
                sportBagMesh = Instantiate(sportBagMeshPrefab, transform);
                sportBagMesh.transform.localPosition = Vector3.zero;
                sportBagMesh.transform.localRotation = Quaternion.Euler(0, 0, 0);

                //TODO: check rotation for away client

                // Check type
                switch (type.Value)
                {
                    case (byte)SportBagType.Local:
                        SetMaterial(localMaterial);
                        break;

                    case (byte)SportBagType.Remote:
                        SetMaterial(remoteMaterial);
                        break;
                    case (byte)SportBagType.Both:
                        SetMaterial(bothMaterial);
                        break;
                }
            }
#endif
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
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
