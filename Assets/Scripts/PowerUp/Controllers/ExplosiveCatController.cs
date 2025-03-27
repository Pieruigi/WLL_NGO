using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace WLL_NGO
{
    public class ExplosiveCatController : PowerUpController
    {
        [SerializeField] GameObject catPrefab;

        public override void Launch()
        {
            // Spawn cat
            var cat = Instantiate(catPrefab);
            cat.GetComponent<ExplosiveCat>().OnCompleted += () => { cat.GetComponent<NetworkObject>().Despawn(); Destroy(gameObject); };
            cat.GetComponent<Rigidbody>().position = User.GetComponent<Rigidbody>().position + Vector3.up * .5f;
            cat.GetComponent<Rigidbody>().rotation = User.GetComponent<Rigidbody>().rotation;
            cat.GetComponent<ExplosiveCat>().SetUser(User);
            cat.GetComponent<NetworkObject>().Spawn(true);
        }

        
    }
}
