using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
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
        public float Width
        {
            get { return width; }
        }
        public float Height
        {
            get { return height; }
        }

        public Vector3 Position
        {
            get { return transform.position; }
        }

        bool triggered = false;
        public bool Triggered
        {
            get { return triggered; }
        }

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

        void OnEnable()
        {
            MatchController.OnStateChanged += HandleOnMatchStateChanged;
        }

        void OnDisable()
        {
            MatchController.OnStateChanged -= HandleOnMatchStateChanged;
        }

        private void HandleOnMatchStateChanged(int oldState, int newState)
        {
            switch (newState)
            {
                case (int)MatchState.Playing:
                    triggered = false;
                    break;
            }
        }

        public static NetController GetOpponentTeamNetController(TeamController team)
        {
            return team.Home ? AwayNetController : HomeNetController;
        }

        public static NetController GetTeamNetController(TeamController team)
        {
            return team.Home ? HomeNetController : AwayNetController;
        }


        public Vector3 GetRandomTarget(bool left)
        {
            float w = Random.Range(width / 2f, 0f);
            if ((!left && home) || (left && !home))
                w *= -1;

            float h = Random.Range(0f, height);

            return new Vector3(transform.position.x, h, w);
        }

        void OnTriggerEnter(Collider other)
        {
            if (NetworkManager.Singleton.IsClient) return;

            if (!MatchController.Instance || !MatchController.Instance.IsSpawned || MatchController.Instance.MatchState != MatchState.Playing) return;

            if (other != BallController.Instance) return;

            if (!trigger)
            {
                triggered = true;
                MatchController.Instance.SetMatchState(MatchState.Goal);
            }
        }

        
    }

}
