using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using WLL_NGO.Netcode;
using static UnityEngine.GraphicsBuffer;

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
        Vector3 diveTarget;
        Vector3 diveOffset;
        float diveTime;
        bool diving = false;
        float diveMaxSpeed = 8;

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
                if(IsBallMovingThisDirection())
                {
                    KeepPosition();
                }
                else // Is coming
                {
                    // How much time it will take for the ball to reach the net line ( computing velocity along the X axis )
                    //Debug.Log($"GK - ball vel:{ball.GetVelocityWithoutEffect()}");
                    //Vector3 xVel = Vector3.ProjectOnPlane(ballVelNoEffect, Vector3.right);
                    //Vector3 xDir = Vector3.ProjectOnPlane(ballDir, Vector3.right);
                    //float time = xDir.magnitude / xVel.magnitude;
                    //// Get the future position of the ball ( maybe the ball is not reaching the net )
                    //Vector3 bPos = ball.Position + (ballVelNoEffect * time) + (0.5f * Physics.gravity.y * Mathf.Pow(time, 2)) * Vector3.up;

                    //// How much time it will take the ball to reach the player ( means the plane through the player right axis )
                    //Vector3 pDirFwd = Vector3.Project(ball.Position - player.Position, transform.forward);//Vector3.ProjectOnPlane(ball.Position - player.Position, Vector3.up);
                    //Debug.Log($"GK - pDirFwd:{pDirFwd}, pPos:{player.Position}, bPos:{ball.Position}");

                    ////Vector3 ballVelNoEffectH = Vector3.ProjectOnPlane(ballVelNoEffect, Vector3.up);
                    //float pTime = pDirFwd.magnitude / Vector3.Project(ballVelNoEffect, transform.forward).magnitude;
                    //Debug.Log($"GK - pTime:{pTime}");
                    //// At which height
                    //float vPos = ball.Position.y + ballVelNoEffect.y * pTime - .5f * Mathf.Abs(Physics.gravity.y) * Mathf.Pow(pTime, 2);

                    //// We must compute distance along player right axis
                    //Vector3 rgtV = Vector3.Project(ballVelNoEffect, transform.right);
                    //Vector3 rgtP = Vector3.Project(ball.Position - player.Position, transform.right);
                    //float rgtDist = Mathf.Sign(Vector3.Dot(rgtP, transform.right)) * rgtP.magnitude;
                    //rgtDist += Mathf.Sign(Vector3.Dot(rgtV, transform.right)) * rgtV.magnitude * pTime;  //  Mathf.Abs(ball.Position.x - player.Position.x);
                    //Debug.Log($"GK - rgtDist:{rgtDist}");
                    //float t = pTime;//xDist / ballVelNoEffect.magnitude;

                    //float hOffset = player.PlayerHeight * .7f;
                    //float vOffset = player.PlayerHeight * .7f;
                    //if(vPos - vOffset > 0)
                    //{
                    //    diveTime = Mathf.Sqrt(2f * (vPos - vOffset) / Mathf.Abs(Physics.gravity.y));
                    //    Debug.Log($"GK - A DiveTime:{diveTime}, vPos:{vPos}, pTime:{pTime}, {gameObject.name}");
                    //}
                    //else
                    //{
                    //    diveTime = pTime;
                    //    Debug.Log($"GK - B DiveTime:{diveTime}, vPos:{vPos}, pTime:{pTime}, {gameObject.name}");
                    //}


                    //if (Mathf.Abs(bPos.z) > (netWidth / 2f) + .5f || Mathf.Abs(bPos.z) > netHeight + .5f || diveTime < pTime - .1f || gameObject.name.EndsWith("_true")) // Out of goal
                    if (!IsBallReachingTheNet() || gameObject.name.EndsWith("_true") || !IsTimeToDive()) // Out of goal
                    {
                        // Keep position or try to get the ball if it's in area
                        KeepPosition();
                    }
                    //else // Ok, it's coming, really
                    //{
                        


                    //    //float tolleranceTime = 1f;
                        
                    //    ////if (t < tolleranceTime)
                    //    //{
                    //    //    // Get the target position
                    //    //    Vector3 targetPos = (ballVelNoEffect * t) + (.5f * Physics.gravity.y * Mathf.Pow(t, 2)) * Vector3.up;

                    //    //    // The distance along the Z axis when the ball reaches the player line
                    //    //    //float zDist = Mathf.Abs(player.Position.z - (ball.Position.z + ballVelNoEffect.z * t));

                    //    //    Debug.Log($"GK - target position:{targetPos}");

                            
                    //    //    float cooldown = 6.5f;

                    //    //    if(Mathf.Abs(rgtDist) < .25f)
                    //    //    {
                    //    //        // Center shot
                    //    //        Debug.Log($"GK - Dive center");
                    //    //        diveTarget = targetPos;
                    //    //        diveOffset = -Vector3.up * vOffset;
                    //    //        player.SetPlayerStateInfo((byte)PlayerState.Diving, (byte)DiveType.Center, (byte)DiveDetail.Middle, cooldown);
                    //    //    }
                    //    //    else
                    //    //    {
                    //    //        //float angle = Vector3.SignedAngle(Vector3.ProjectOnPlane(ballVelNoEffect, Vector3.up).normalized, Vector3.ProjectOnPlane(player.Position - ball.Position, Vector3.up), Vector3.up);
                    //    //        if (rgtDist > 0f)
                    //    //        {
                    //    //            Debug.Log($"GK - Dive right, dist:{rgtDist}");
                    //    //            diveTarget = targetPos;
                    //    //            diveOffset = -transform.right * hOffset - Vector3.up * vOffset;
                    //    //            player.SetPlayerStateInfo((byte)PlayerState.Diving, (byte)DiveType.Right, (byte)DiveDetail.Middle, cooldown);
                    //    //        }
                    //    //        else
                    //    //        {
                    //    //            Debug.Log($"GK - Dive left, dist:{rgtDist}");
                    //    //            diveTarget = targetPos;
                    //    //            diveOffset = transform.right * hOffset - Vector3.up * vOffset;
                    //    //            player.SetPlayerStateInfo((byte)PlayerState.Diving, (byte)DiveType.Left, (byte)DiveDetail.Middle, cooldown);
                    //    //        }
                    //    //    }

                    //    //    Time.timeScale = 0;

                    //    //}
                    //}
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

        bool IsTimeToDive()
        {
            Vector3 ballVelNoEffect = ball.GetVelocityWithoutEffect();
            Vector3 pDirFwd = Vector3.Project(ball.Position - player.Position, transform.forward);//Vector3.ProjectOnPlane(ball.Position - player.Position, Vector3.up);
            Debug.Log($"GK - pDirFwd:{pDirFwd}, pPos:{player.Position}, bPos:{ball.Position}");

            //Vector3 ballVelNoEffectH = Vector3.ProjectOnPlane(ballVelNoEffect, Vector3.up);
            float pTime = pDirFwd.magnitude / Vector3.Project(ballVelNoEffect, transform.forward).magnitude;
            Debug.Log($"GK - pTime:{pTime}");
            // At which height
            float vPos = ball.Position.y + ballVelNoEffect.y * pTime - .5f * Mathf.Abs(Physics.gravity.y) * Mathf.Pow(pTime, 2);



            // We must compute distance along player right axis
            //Vector3 rgtV = Vector3.Project(ballVelNoEffect, transform.right);

            //Debug.Log($"GK - velRgt-Proj:{rgtV}");
            //Vector3 rgtP = Vector3.Project(ball.Position - player.Position, transform.right);
            float rgtDist = 0;// Mathf.Sign(Vector3.Dot(rgtP, transform.right)) * rgtP.magnitude;

            

            //Debug.Log($"GK - rgtDist-1:{rgtDist}");
            //Debug.Log($"GK - velRgt:{Mathf.Sign(Vector3.Dot(rgtV, transform.right)) * rgtV.magnitude}");
            //rgtDist += Mathf.Sign(Vector3.Dot(rgtV, transform.right)) * rgtV.magnitude * pTime;  //  Mathf.Abs(ball.Position.x - player.Position.x);
            //Debug.Log($"GK - rgtDist-2:{rgtDist}");

            Debug.Log($"GK - vel:{ballVelNoEffect}");
            Vector3 hPos = Vector3.ProjectOnPlane(ball.Position, Vector3.up) + Vector3.ProjectOnPlane(ballVelNoEffect, Vector3.up) * pTime;
            Debug.Log($"GK - hPos:{hPos}");
            Vector3 rgtDistV = (Vector3.ProjectOnPlane(player.Position, Vector3.up) - hPos);
            Debug.Log($"GK - rgtDist.mag:{rgtDistV.magnitude}");
            rgtDist = rgtDistV.magnitude * Mathf.Sign(Vector3.Dot(rgtDistV, transform.right));
            Debug.Log($"GK - rgtDist:{rgtDist}");

            float t = pTime;//xDist / ballVelNoEffect.magnitude;

            float hOffset = Mathf.Sign(rgtDist) * player.PlayerHeight * .7f;
            float vOffset = player.PlayerHeight * .7f;

            Vector3 targetPos = player.Position + transform.right * (rgtDist-hOffset);
            if (vPos - vOffset > 0)
            {
                diveTime = Mathf.Sqrt(2f * (vPos - vOffset) / Mathf.Abs(Physics.gravity.y));
                Debug.Log($"GK - A DiveTime:{diveTime}, vPos:{vPos}, pTime:{pTime}, {gameObject.name}");
                targetPos += (vPos - vOffset) * Vector3.up;

            }
            else
            {
                diveTime = pTime;
                Debug.Log($"GK - B DiveTime:{diveTime}, vPos:{vPos}, pTime:{pTime}, {gameObject.name}");
            }

            

            if (diveTime < pTime - .1f)
                return false;

            //Vector3 targetPos = (ballVelNoEffect * t) + (.5f * Physics.gravity.y * Mathf.Pow(t, 2)) * Vector3.up;

            // The distance along the Z axis when the ball reaches the player line
            //float zDist = Mathf.Abs(player.Position.z - (ball.Position.z + ballVelNoEffect.z * t));

            Debug.Log($"GK - target position:{targetPos}");


            float cooldown = 6.5f;

            if (Mathf.Abs(rgtDist) < .25f)
            {
                // Center shot
                Debug.Log($"GK - Dive center");
                diveTarget = targetPos;
                //diveOffset = -Vector3.up * vOffset;
                player.SetPlayerStateInfo((byte)PlayerState.Diving, (byte)DiveType.Center, (byte)DiveDetail.Middle, cooldown);
            }
            else
            {
                //float angle = Vector3.SignedAngle(Vector3.ProjectOnPlane(ballVelNoEffect, Vector3.up).normalized, Vector3.ProjectOnPlane(player.Position - ball.Position, Vector3.up), Vector3.up);
                if (rgtDist > 0f)
                {
                    Debug.Log($"GK - Dive right, dist:{rgtDist}");
                    diveTarget = targetPos;
                    //diveOffset = -transform.right * hOffset - Vector3.up * vOffset;
                    player.SetPlayerStateInfo((byte)PlayerState.Diving, (byte)DiveType.Right, (byte)DiveDetail.Middle, cooldown);
                }
                else
                {
                    Debug.Log($"GK - Dive left, dist:{rgtDist}");
                    diveTarget = targetPos;
                    //diveOffset = transform.right * hOffset - Vector3.up * vOffset;
                    player.SetPlayerStateInfo((byte)PlayerState.Diving, (byte)DiveType.Left, (byte)DiveDetail.Middle, cooldown);
                }
            }

            return true;
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

                keepPositionTollerance = .25f;
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
       
        public async void UpdateDive()
        {

            if (!diving)
            {
                // Get the time it will take for the player to reach that position
                Vector3 target = diveTarget;
                float jumpVelV = Mathf.Abs(Physics.gravity.y) * diveTime;
                float jumpVelH = Vector3.Distance(Vector3.ProjectOnPlane(player.Position, Vector3.up), Vector3.ProjectOnPlane(target, Vector3.up)) / diveTime;

                Debug.Log($"GK - divetime, Update dive, targetPos:{diveTarget}, diveTime:{diveTime}, jumpVelV:{jumpVelV}");

                player.Velocity = jumpVelV * Vector3.up + jumpVelH * transform.right;
                diving = true;
            }
            else
            {

            }
            
        }
    }

}
