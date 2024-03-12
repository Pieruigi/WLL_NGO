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

        float keepPositionTollerance = 1.5f;
        float keepPositionDistance = 3;
        //float[] keepPositionCenter;

        Vector3 netCenter;
        float netWidth, netHeight;
        Bounds areaBounds;
        
        
        float keepPositionTolleranceDefault;
        bool superShot = false;
        BallController ball;

        private void Awake()
        {
            player = GetComponent<PlayerController>();
            keepPositionTolleranceDefault = keepPositionTollerance;
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
            netCenter.y = 0f;
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
            Vector3 direction = ball.Position - netCenter;
            direction.y = 0;

            // Compute the goalkeeper target position
            Vector3 targetPosition = netCenter + direction.normalized * keepPositionDistance;
            if(Vector3.Distance(player.Position, targetPosition) > keepPositionTollerance)
            {
                keepPositionTollerance = .25f;
                Vector3 worldDirecton = targetPosition - player.Position;
                worldDirecton.y = 0;
            
                ((NotHumanInputHandler)player.GetInputHandler()).SetJoystick(InputData.ToInputDirection(worldDirecton));
            }
            else
            {
                keepPositionTollerance = keepPositionTolleranceDefault;
                ((NotHumanInputHandler)player.GetInputHandler()).SetJoystick(Vector3.zero);
            }
        }
       
    }

}
