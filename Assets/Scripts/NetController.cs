using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Services.Matchmaker.Models;
using UnityEngine;
using UnityEngine.Events;
using WLL_NGO.Netcode;

namespace WLL_NGO
{
    public class NetController : MonoBehaviour
    {
        public static UnityAction<TeamController> OnGoalScored;

        public static NetController HomeNetController;
        public static NetController AwayNetController;

        [SerializeField]
        bool home;

  
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

            width = GetComponent<BoxCollider>().size.z;// trigger.transform.lossyScale.z;
            height = GetComponent<BoxCollider>().size.y;// trigger.transform.lossyScale.y;
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
                case (int)MatchState.KickOff:
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
            if (!NetworkManager.Singleton.IsServer) return;

            Debug.Log($"TEST - Net trigger, is not client");

            if (!MatchController.Instance || !MatchController.Instance.IsSpawned || MatchController.Instance.MatchState != MatchState.Playing) return;

            Debug.Log($"TEST - Net trigger, MatchController initialized");

            if (other.gameObject != BallController.Instance.gameObject) return;

            Debug.Log($"TEST - Net trigger, this is the ball");

            if (!triggered)
            {

                triggered = true;
                var scorer = home ? TeamController.AwayTeam : TeamController.HomeTeam;
                Debug.Log($"TEST - Net trigger, scorer:{scorer.gameObject.name}");
                OnGoalScored?.Invoke(scorer);
            }
        }

        
    }

}
