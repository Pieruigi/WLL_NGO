#define ML_ENABLE
#if ML_ENABLE
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using WLL_NGO.AI.Test;

namespace WLL_NGO.AI
{
    public class PlayerAttack : Agent
    {

        PlayerAI playerAI;

        Vector3 startPosition;
        float timer = 0;

        bool running = false;

        void Awake()
        {
            playerAI = GetComponent<PlayerAI>();
            startPosition = playerAI.Position;
        }

        void Update()
        {
            return;
            if (!running)
            {
                return;
            }

            timer += Time.deltaTime;
            if (timer > 10)
            {
                running = false;
                AddReward(-1f);
                EndEpisode();
                return;
            }

            var dist = Vector3.Distance(playerAI.Position, TestBallController.Instance.Position);
            if (dist < 4f)
            {
                Debug.Log("Goal");
                running = false;
                AddReward(1f);
                EndEpisode();
            }
          
        }

        public override void OnEpisodeBegin()
        {
            playerAI.GetComponent<Rigidbody>().position = startPosition;
            timer = 0;
            running = true;
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            base.CollectObservations(sensor);
            sensor.AddObservation(playerAI.Position);
            sensor.AddObservation(TestBallController.Instance.Position);
            
            foreach (var player in playerAI.TeamAI.Players)
            {
                if (player != playerAI)
                    sensor.AddObservation(player.Position);
            }
            
        }

        public override void OnActionReceived(ActionBuffers actions)
        {
            base.OnActionReceived(actions);
            
            var move = new Vector3(actions.ContinuousActions[0], 0, actions.ContinuousActions[1]);
            move.Normalize();

            playerAI.GetComponent<Rigidbody>().position += move * 3 * Time.deltaTime;


        }


    }
    
    
}

#endif