using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using WLL_NGO.Netcode;

namespace WLL_NGO
{
    public class ExplosiveCat : NetworkBehaviour
    {
#if !UNITY_SERVER
        [SerializeField] GameObject modelPrefab;

        GameObject model;

        Animator animator;
#endif

        [SerializeField]
        float maxSpeed = 8;

        [SerializeField]
        float rotationSpeed = 3;

        [SerializeField]
        float force = 5;



        Rigidbody rb;

        PlayerController target;

        float targetTime = 1;

        float targetElapsed = 0;

        PlayerController user;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        void FixedUpdate()
        {
            if (target)
            {
                var dir = Vector3.ProjectOnPlane(target.Position - rb.position, Vector3.up);
                dir = Vector3.MoveTowards(dir, target.Position, rotationSpeed * Time.fixedDeltaTime);
                rb.AddForce(dir * force, ForceMode.Acceleration);
            }
            else
            {
                targetElapsed += Time.fixedDeltaTime;
                if (targetElapsed > targetTime)
                {
                    ChooseTarget();
                }

                // Move forward
                var dir = transform.forward;
                rb.AddForce(transform.forward * force, ForceMode.Acceleration);

            }

            // Clamp speed
            if (rb.velocity.magnitude > maxSpeed)
            {
                rb.velocity = rb.velocity.normalized * maxSpeed;
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {

            }

#if !UNITY_SERVER
            if (IsClient)
            {
                // Create model
                model = Instantiate(modelPrefab, transform);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;

                // Get animator
                animator = GetComponentInChildren<Animator>();
            }
#endif

        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
        }

        void ChooseTarget()
        {
            // Get opponent team players
            var opponentTeam = TeamController.GetOpponentTeam(user);

            // Choose the closest player
            float minDist = 0;
            var players = opponentTeam.GetPlayers();
            foreach (var player in players)
            {
                var dist = Vector3.Distance(player.Position, rb.position);
                if (!target || dist < minDist)
                {
                    target = player;
                    minDist = dist;
                }
            }

            

        }

        public void SetUser(PlayerController user)
        {
            this.user = user;
        }
    }
    
}
