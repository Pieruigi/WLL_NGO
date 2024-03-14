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
        float takeTheBallRange = 2f;

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
            

            
            if(ball.Owner != null) // The ball is controller by someone 
            {
                if(ball.Owner == this)// Goalkeeper is controlling the ball
                {
                    player.ResetLookDirection();
                }
                else // Someone else is controlling the ball
                {
                    if (areaBounds.Contains(ball.Position))
                    {
                        // The ball is inside the goalkeeper area...
                        if (!player.IsTeammate(ball.Owner))
                        {
                            // ... and controlled by an opponent player, so lets try to get it
                            TakeBallFromOpponent(ball.Owner);

                        }
                    }
                    else
                    {
                        // Ball is out the goalkeeper area, just keep position
                        KeepPosition();
                    }
                }
                
            }
            else // The ball is not controlled at all
            {
                Vector3 ballVel = ball.GetVelocityWithoutEffect();
                Vector3 ballDir = netCenter - ball.Position;
                float dot = Vector3.Dot(Vector3.ProjectOnPlane(ball.Velocity, Vector3.up), Vector3.ProjectOnPlane(ballDir, Vector3.up));
                if(dot < 0) // The ball is going to the opponent goal line
                {
                    KeepPosition();
                }
                else // Is coming
                {
                    // How much time it will take for the ball to reach the net line
                    Vector3 xVel = Vector3.ProjectOnPlane(ballVel, Vector3.right);
                    Vector3 xDir = Vector3.ProjectOnPlane(ballDir, Vector3.right);
                    float time = xDir.magnitude / xVel.magnitude;
                    // Get the future position of the ball ( maybe the ball is not reaching the net )
                    Vector3 bPos = ball.Position + ballVel * time;
                    if (Mathf.Abs(bPos.z) > (netWidth / 2f) + .5f || Mathf.Abs(bPos.z) > netHeight + .5f) // Out of goal
                    {
                        // Keep position or try to get the ball if it's in area
                        KeepPosition();
                    }
                    else // Ok, it's coming, really
                    {

                    }
                }

            }

            //if (areaBounds.Contains(ball.Position))
            //{
            //    // If an opponent controls the ball try to take it
            //    if(ball.Owner != null && ball.Owner != this)
            //    {
            //        if (!player.IsTeammate(ball.Owner)) 
            //        {
            //            // The player who controls the ball is an opponent, so we try to get the ball from them
            //            TakeBallFromOpponent(ball.Owner);

            //        }
            //    }


            //}
            //else
            //{
            //    if(ball.Owner != null && ball.Owner != this)
            //    {
                    
            //    }
            //    else
            //    {
            //        if(ball.Owner == this)
            //        {
            //            player.ResetLookDirection();
            //        }
            //    }
            //}
        }

        private void TakeBallFromOpponent(PlayerController ballOwner)
        {

            if (ballOwner == null || player.IsTeammate(ballOwner))
                return;

         
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
                
                //((NotHumanInputHandler)player.GetInputHandler()).SetJoystick(Vector2.zero);
                // Hit the opponent
                //if(player.sta)
                player.GiveSlap();
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
