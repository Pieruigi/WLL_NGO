using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using WLL_NGO.Netcode;

namespace WLL_NGO
{
    public class ExplosiveCat : NetworkBehaviour
    {
        public UnityAction OnCompleted;

#if !UNITY_SERVER
        [SerializeField] GameObject modelPrefab;

        GameObject model;

        Animator animator;
#endif

        //[SerializeField]
        float maxSpeed = 8;

        //[SerializeField]
        float rotationSpeed = 3;

        //[SerializeField]
        float force = 5;

        //[SerializeField]
        float explosionForce = 10f;

        //[SerializeField]
        float explosionRange = 8f;

        //[SerializeField]
        float explosionDelay = 1;

        //[SerializeField]
        Collider trigger;

        [SerializeField]
        Collider _collider;

        Rigidbody rb;

        PlayerController target;

        PlayerController user;

        

        /// <summary>
        /// 0: moving
        /// 1: exploding
        /// </summary>
        NetworkVariable<byte> state = new NetworkVariable<byte>(0);

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
            if (!IsSpawned) return;

            //if (state.Value != 0) return;

            if (target)
            {
                var dir = Vector3.ProjectOnPlane(target.Position - rb.position, Vector3.up);
                dir = Vector3.MoveTowards(dir, target.Position, rotationSpeed * Time.fixedDeltaTime);
                rb.transform.forward = dir;
                rb.AddForce(dir * force, ForceMode.Acceleration);
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
                // Disable collision with the player who's spaning it
                Physics.IgnoreCollision(user.GetComponent<CapsuleCollider>(), _collider, true);
                // Choose target
                ChooseTarget();
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

                // Disable collider on client
                _collider.enabled = false;
            }
#endif

            // Both client and server
            state.OnValueChanged += (o,n) => { Explode(); };
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

        void OnTriggerEnter(Collider other)
        {
            if (!IsServer) return; // Server only    

            if (state.Value > 0) return; // Already triggered
         
            // Is player on trigger?
            if (!other.CompareTag(Tags.Player)) return;
         
            // Get player controller
            PlayerController player = other.GetComponent<PlayerController>();

            // In case they're teammates return
            if (player.IsTeammate(user)) return;

            // It's an opponent
            state.Value = 1;
        }

        void Explode()
        {
            Debug.Log("TEST - Exploding....");

            if (IsServer)
            {
                DoPhysicalExplosion();
            }

#if !UNITY_SERVER
            if (IsClient)
            {
                PlayExplosionFx();
            }
#endif

        }

        async void DoPhysicalExplosion()
        {
            await Task.Delay(TimeSpan.FromSeconds(explosionDelay));
            // Check all players in the explosion range
            List<PlayerController> players = FindObjectsOfType<PlayerController>().ToList().FindAll(p => Vector3.Distance(p.Position, rb.position) < explosionRange);


            float forceMax = explosionForce;
            float forceMin = explosionForce * .6f;

            // Blow players up
            foreach (var player in players)
            {
                Vector3 dir = player.Position - rb.position;

                float force = Mathf.Lerp(forceMax, forceMin, dir.magnitude / explosionRange);
                Debug.Log($"TEST - apply force {force} to player {player.gameObject.name}");
                player.GetComponent<Rigidbody>().AddForce((dir.normalized+Vector3.up*2f).normalized*force, ForceMode.VelocityChange);

                bool front = Vector3.Dot(player.transform.forward, dir) < 0 ? true : false;                
                player.SetBlowUpState(front);
            }

            await Task.Delay(TimeSpan.FromSeconds(2f));

            OnCompleted?.Invoke();
        }

#if !UNITY_SERVER
        async void PlayExplosionFx()
        {
            animator.SetTrigger("Explode");

            // just wait
            await Task.Delay(TimeSpan.FromSeconds(explosionDelay));

            // Apply explosion fx here

        }

#endif

        public void SetUser(PlayerController user)
        {
            this.user = user;
        }
    }
    
}
