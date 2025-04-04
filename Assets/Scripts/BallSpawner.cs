using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace WLL_NGO
{
    public class BallSpawner : Singleton<BallSpawner>
    {

        [SerializeField] NetworkPrefabsList ballPrefabList;

        public void SpawnBall()
        {
            GameObject ball = Instantiate(ballPrefabList.PrefabList[0].Prefab);
            //SphereCollider coll = ball.GetComponent<SphereCollider>();
            //ball.transform.position = Vector3.up * coll.radius;
            ball.transform.position = GetKickOffBallPosition();
            ball.transform.rotation = Quaternion.identity;
            ball.GetComponent<NetworkObject>().Spawn();

        }

        public Vector3 GetKickOffBallPosition()
        {
            return Vector3.up * Netcode.BallController.Instance.gameObject.GetComponent<SphereCollider>().radius;
        }
    }

}
