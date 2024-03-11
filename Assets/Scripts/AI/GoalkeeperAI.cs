using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using WLL_NGO.Netcode;

namespace WLL_NGO.AI
{
    public class GoalkeeperAI : MonoBehaviour
    {
        PlayerController player;

        //float keepPositionTollerance = 2;
        //float[] keepPositionCenter;

        Vector3 netCenter;
        float netWidth, netHeight;
        Bounds areaBounds;
        
        float keepPositionDistance = 0;

        bool superShot = false;
        BallController ball;

        private void Awake()
        {
            player = GetComponent<PlayerController>();
        }

        private void Start()
        {
            Initialize();
        }

        // Update is called once per frame
        void Update()
        {
            if (!NetworkManager.Singleton.IsServer || !MatchController.Instance.IsPlaying())
                return;

            if (superShot)
                UpdateSuperShot();
            else
                UpdateNormal();
        }

        private void OnEnable()
        {
            //PlayerController.OnSpawned += HandleOnPlayerSpawned;
            BallController.OnBallSpawned += HandleOnBallSpawned;
        }

        private void OnDisable()
        {
            //PlayerController.OnSpawned -= HandleOnPlayerSpawned;
            BallController.OnBallSpawned -= HandleOnBallSpawned;
        }

        void HandleOnBallSpawned()
        {
            //Debug.Log($"Ball spawned:{BallController.Instance}");
            ball = BallController.Instance;
        }

        void Initialize()
        {
            TeamController team = TeamController.GetPlayerTeam(player);
            NetController net = NetController.GetTeamNetController(team);
            netCenter = net.Position;
            netWidth = net.Width;
            netHeight = net.Height;
            
            float areaHeight = 10f;
            areaBounds.center = new Vector3(net.Position.x + (team.Home ? 1f : -1f) * GameFieldInfo.GetAreaLength() / 2f, areaHeight / 2f, 0f);
            areaBounds.extents = new Vector3(GameFieldInfo.GetAreaLength()/2f, areaHeight/2f, GameFieldInfo.GetAreaWidth()/2f);

            keepPositionDistance = GameFieldInfo.GetAreaLength() / 2f;

        }

        void UpdateSuperShot()
        {

        }

        void UpdateNormal()
        {
            if (areaBounds.Contains(ball.Position))
            {
                Debug.Log("Ball is in area");
            }
            else
            {
                Debug.Log("Ball is not in area");
                if(ball.Owner != null)
                {
                    // Move the goalkeeper in the best position
                    KeepPosition();
                }
            }
        }


        void KeepPosition()
        {
            // Get ball direction
            Vector3 direction = ball.Position - player.Position;
            direction.y = 0;


        }
       
    }

}
