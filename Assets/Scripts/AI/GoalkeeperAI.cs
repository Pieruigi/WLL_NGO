using System;
using System.Collections;
using System.Collections.Generic;
using TMPro.EditorUtilities;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using WLL_NGO.Netcode;
using static UnityEngine.GraphicsBuffer;

namespace WLL_NGO.AI
{
    public class GoalkeeperAI : NetworkBehaviour
    {
        
        [SerializeField]
        Transform handBallHook;

        //[SerializeField]
        //BallHandlingTrigger ballBlockingTrigger;

        NetworkVariable<bool> isBouncingTheBallBack = new NetworkVariable<bool>(false);
        public bool IsBouncingTheBallBack
        {
            get { return isBouncingTheBallBack.Value; }
        }



        PlayerController player;

        float keepPositionTollerance = .75f;
        float keepPositionDistance = 3;
        //float[] keepPositionCenter;

        Vector3 netCenter;
        float netWidth, netHeight;
        Bounds areaBounds;
        
        
        float keepPositionTolleranceDefault;
        bool superShot = false;
        BallController ball;
        
        
        float takeTheBallRange = 2f;
        bool diving = false;
        float diveSpeedMax = 8;
        float diveTolleranceTime = .8f;
        Vector3 diveDir = Vector3.zero;
        int diveHigh = 0; // -1: low, 0: middle, 1: high
        float diveSpeed = 5;
        float diveTime = 0;
        //bool blockTheBall = false;
        float diveCooldown;
        float ballHookLerpSpeed = 10; 
        

        private void Awake()
        {
            player = GetComponent<PlayerController>();
            player.SetDiveUpdateFunction(UpdateDive);
            keepPositionTolleranceDefault = keepPositionTollerance;

        }

        private void Start()
        {
            Initialize();
        }

       


        // Update is called once per frame
        void Update()
        {
            if (!NetworkManager.Singleton.IsServer || !MatchController.Instance.IsPlaying() || player.Role != PlayerRole.GK)
                return;

            if (superShot)
                UpdateSuperShot();
            else
                UpdateNormal();
                
        }

        private void FixedUpdate()
        {
            if (!NetworkManager.Singleton.IsServer || !MatchController.Instance.IsPlaying() || player.Role != PlayerRole.GK)
                return;

            //if(!superShot)
            //    CheckForBallBlocking();
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

            if (player.Role != PlayerRole.GK)
                return;

           
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

            if (player.GetState() != (byte)PlayerState.Normal) return;

            
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
                
                Vector3 ballVelNoEffect = ball.GetVelocityWithoutEffect();
                Vector3 ballDir = netCenter - ball.Position;
                float dot = Vector3.Dot(Vector3.ProjectOnPlane(ballVelNoEffect, Vector3.up), Vector3.ProjectOnPlane(ballDir, Vector3.up));
                //if(ballVelNoEffect.magnitude == 0 || dot < 0) // The ball is going to the opponent goal line
                if(!IsBallMovingThisDirection() || !IsBallReachingTheNet() || !IsTimeToDive())
                {
                    KeepPosition();
                }
                else // Is coming
                {
                    if (IsTimeToDive())
                        StartDiving();
                    
                }

            }

           
        }
        
        //void CheckForBallBlocking()
        //{
        //    if (!isBouncingTheBallBack.Value)
        //    {
        //        ball.Position = Vector3.MoveTowards(ball.Position, handBallHook.position, ballHookLerpSpeed * Time.fixedDeltaTime);
        //    }
        //}

        void StartDiving()
        {
            
            Vector3 ballVelNoEffect = ball.GetVelocityWithoutEffect();
            //Vector3 nextBallPos = ball.Position + ballVelNoEffect * diveTolleranceTime;

            Vector3 ballDirFwdProj = Vector3.Project(ball.Position - player.Position, transform.forward);
            Vector3 ballVelFwdProj = Vector3.Project(ballVelNoEffect, transform.forward);
            float projT = ballDirFwdProj.magnitude / ballVelFwdProj.magnitude;

            Vector3 nextBallPos = ball.Position + (ballVelNoEffect * projT) + (.5f * Physics.gravity.y * Mathf.Pow(projT, 2)) * Vector3.up;

            Debug.Log($"GK - next ball pos:{nextBallPos}");
            Vector3 nextBallDir = Vector3.ProjectOnPlane(nextBallPos-player.Position, Vector3.up);
            Debug.Log($"GK - positions:{ball.Position}, nextPos:{nextBallPos}, playerPos:{player.Position}");
            Vector3 rgtProj = Vector3.Project(nextBallDir, transform.right);
            float cooldown = 1.5f;
            diveCooldown = cooldown;
            diveHigh = 0;
            diving = false;
            diveTime = projT;
            
            //blockTheBall = true;
            //player.DisableBallHandlingTrigger();
            isBouncingTheBallBack.Value = true;


            if (rgtProj.magnitude < .25f)
            {
                Debug.Log("GK - dive center");
                // Dive center
                diveDir = Vector3.zero;
                player.SetPlayerStateInfo((byte)PlayerState.Diving, (byte)DiveType.Center, (byte)DiveDetail.Middle, cooldown);
            }
            else
            {
                
                diveSpeed = (nextBallPos - player.Position).magnitude / projT;
                diveSpeed = Mathf.Clamp(diveSpeed, 0, diveSpeedMax);
                float rgtDot = Vector3.Dot(rgtProj, transform.right);
                if(rgtDot < 0)
                {
                    // Left
                    Debug.Log("GK - dive left");
                    diveDir = nextBallDir.normalized;//-transform.right;
                    player.SetPlayerStateInfo((byte)PlayerState.Diving, (byte)DiveType.Left, (byte)DiveDetail.Middle, cooldown);
                }
                else
                {
                    // Right
                    Debug.Log("GK - dive right");
                    diveDir = nextBallDir.normalized;// transform.right;
                    player.SetPlayerStateInfo((byte)PlayerState.Diving, (byte)DiveType.Right, (byte)DiveDetail.Middle, cooldown);
                }
            }
            
        }

        bool IsTimeToDive()
        {
            Vector3 ballVelNoEffect = ball.GetVelocityWithoutEffect();
            Vector3 ballDir = Vector3.ProjectOnPlane(ball.Position - player.Position, Vector3.up);
            float dotBefore = Vector3.Dot(ballDir, transform.forward);
            //Debug.Log($"GK - Dot before:{dotBefore}");
            // Get the ball position in X secs
            float time = diveTolleranceTime;
            Vector3 nextBallPos = ball.Position + ballVelNoEffect * time;
            
            ballDir = Vector3.ProjectOnPlane(nextBallPos - player.Position, Vector3.up);
            float dotAfter = Vector3.Dot(ballDir, transform.forward);
            //Debug.Log($"GK - Dot after:{dotAfter}");
            if (Mathf.Sign(dotAfter) != Mathf.Sign(dotBefore))
                return true;
            else 
                return false;
        }

        bool IsBallReachingTheNet()
        {
            
            // How much time it will take for the ball to reach the net line ( computing velocity along the X axis )
            Vector3 ballVelNoEffect = ball.GetVelocityWithoutEffect();
            Vector3 ballDir = netCenter - ball.Position;
            Debug.Log($"GK - ball vel:{ball.GetVelocityWithoutEffect()}");
            Vector3 xVel = Vector3.ProjectOnPlane(ballVelNoEffect, Vector3.right);
            Vector3 xDir = Vector3.ProjectOnPlane(ballDir, Vector3.right);
            float time = xDir.magnitude / xVel.magnitude;
            // Get the future position of the ball ( maybe the ball is not reaching the net )
            Vector3 bPos = ball.Position + (ballVelNoEffect * time) + (0.5f * Physics.gravity.y * Mathf.Pow(time, 2)) * Vector3.up;
            return Mathf.Abs(bPos.z) < (netWidth / 2f) + .5f && Mathf.Abs(bPos.z) < netHeight + .5f;
        }

        bool IsBallMovingThisDirection()
        {
            Vector3 ballVelNoEffect = ball.GetVelocityWithoutEffect();
            Vector3 ballDir = netCenter - ball.Position;
            float dot = Vector3.Dot(Vector3.ProjectOnPlane(ballVelNoEffect, Vector3.up), Vector3.ProjectOnPlane(ballDir, Vector3.up));
            return ballVelNoEffect.magnitude > 0 && dot > 0;
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
                //Debug.Log($"GK - move {gameObject.name} from {player.Position} to target position:{targetPosition}");

                keepPositionTollerance = keepPositionTolleranceDefault / 3f;
                Vector3 worldDirecton = targetPosition - player.Position;
                worldDirecton.y = 0;
            
                ((NotHumanInputHandler)player.GetInputHandler()).SetJoystick(InputData.ToInputDirection(worldDirecton));
            }
            else
            {
                //Debug.Log($"GK - stopping {gameObject.name} to position:{player.Position}");
                keepPositionTollerance = keepPositionTolleranceDefault;
                ((NotHumanInputHandler)player.GetInputHandler()).SetJoystick(Vector3.zero);
            }
        }
       
        public void UpdateDive()
        {

            if (!diving)
            {
                Debug.Log("Dive");
                
                player.Velocity = diveDir.normalized * diveSpeed + player.Velocity.y * Vector3.up;
                diving = true;
            }
            else
            {
                if(diveTime > 0)
                {
                    diveTime -= NetworkTimer.Instance.DeltaTick;
                    if(diveTime < 0)
                    {
                        player.Velocity = Vector3.zero + player.Velocity.y * Vector3.up;
                        //diving = false;
                    }
                }
            }
            
        }

        public Transform GetBallHook()
        {
            Debug.Log("Getting ball hook");
            return handBallHook;
        }

        public void BounceTheBallBack()
        {
            Debug.Log("Bouncing the ball back...");

            //isBouncingTheBallBack.Value = false;
            ball.Velocity = -ball.Velocity;
            //ball.Position += transform.forward * 2f;
        }
    }

}
