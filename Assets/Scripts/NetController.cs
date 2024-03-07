using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WLL_NGO.Netcode;

namespace WLL_NGO
{
    public class NetController : MonoBehaviour
    {
        public static NetController HomeNetController;
        public static NetController AwayNetController;

        [SerializeField]
        bool home;

        [SerializeField]
        GameObject trigger;

        //[SerializeField]
        //List<Transform> targets;

        float width, height;

        private void Awake()
        {
            if (home)
                HomeNetController = this;
            else
                AwayNetController = this;

            width = trigger.transform.lossyScale.z;
            height = trigger.transform.lossyScale.y;
            Debug.Log($"Width:{width}");
            Debug.Log($"Height:{height}");
        }

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        public static NetController GetOpponentTeamNetController(TeamController team)
        {
            return team.Home ? AwayNetController : HomeNetController;
        }

        //public Transform GetLeftTarget()
        //{
        //    return targets[2];
        //}

        //public Transform GetRightTarget()
        //{
        //    return targets[0];
        //}

        //public Transform GetCenterTarget()
        //{
        //    return targets[1];
        //}

        public Vector3 GetRandomTarget(bool left)
        {
            float w = Random.Range(width / 2f, 0f);
            if ((!left && home) || (left && !home))
                w *= -1;

            float h = Random.Range(0f, height);

            return new Vector3(transform.position.x, h, w);
        }
    }

}
