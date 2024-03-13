using System;
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
        float diveSpeed = 5;
        float takeTheBallRange = 1f;

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
            BallController.OnOwnerChanged += HandleOnOwnerChanged;
        }

        
        private void OnDisable()
        {
            //PlayerController.OnSpawned -= HandleOnPlayerSpawned;
            BallController.OnBallSpawned -= HandleOnBallSpawned;
            BallController.OnOwnerChanged -= HandleOnOwnerChanged;
        }

        void HandleOnBallSpawned()
        {
            //Debug.Log($"Ball spawned:{BallController.Instance}");
            ball = BallController.Instance;
        }

        private void HandleOnOwnerChanged(PlayerController oldOwner, PlayerController newOwner)
        {
            Debug.Log($"Owner changed:{oldOwner}, {newOwner}");

            if (newOwner == player)
            {
                player.SetInputHandler(new HumanInputHandler());
                player.ResetLookDirection();
            }
            else if (oldOwner == player) 
                player.SetInputHandler(new NotHumanInputHandler());
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
            if (player.IsSelected())
                return;
            

            if (areaBounds.Contains(ball.Position))
            {
                // If an opponent controls the ball try to take it
                if(ball.Owner != null)
                {
                    //if (!player.IsTeammate(ball.Owner)) 
                    if (player.IsTeammate(ball.Owner)) // TEST 
                    {
                        // The player who controls the ball is an opponent, so we try to get the ball from them
                        TakeBallFromOpponent(ball.Owner);

                    }
                }

            }
            else
            {
                if(ball.Owner != null && ball.Owner != this)
                {
                    // Move the goalkeeper in the best position
                    KeepPosition();
                }
                else
                {
                    if(ball.Owner == this)
                    {
                        player.ResetLookDirection();
                    }
                }
            }
        }

        private void TakeBallFromOpponent(PlayerController ballOwner)
        {

            //if (ballOwner == null || player.IsTeammate(ballOwner))
            //    return;

            // Ball direction
            Vector3 ballDir = ball.Position - player.Position;
            Vector3 ballSpeed = ball.Velocity;

            if(ballDir.magnitude > takeTheBallRange)
            {
                player.SetLookDirection(ballDir);
                ((NotHumanInputHandler) player.GetInputHandler()).SetJoystick(InputData.ToInputDirection(ballDir));
            }
            else
            {
                ((NotHumanInputHandler)player.GetInputHandler()).SetJoystick(Vector2.zero);
            }
            // Move towards the ball
            
        }

        void KeepPosition()
        {
            // Look at the ball
            player.SetLookDirection(ball.Position - player.Position);

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
