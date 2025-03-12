using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Services.Matchmaker.Models;
using Unity.VisualScripting;
using UnityEngine;
using WLL_NGO.AI.Test;

namespace WLL_NGO.AI
{
    public class FormationHelperTrigger : MonoBehaviour
    {
      
        [SerializeField]
        int ballOwnerIndex = -1;
        public int BallOwnerIndex
        {
            get{ return ballOwnerIndex; }
        }

        [SerializeField]
        Transform positionGroup;

        List<Transform> positions = new List<Transform>();
        public IList<Transform> Positions
        {
            get{ return positions.AsReadOnly(); }
        }

        [SerializeField]
        Transform pivot;
        public Transform Pivot
        {
            get{ return pivot; }
        }

        FormationHelper rootHelper;

        void Awake()
        {
            rootHelper = transform.root.GetComponent<FormationHelper>();
        }

        // Start is called before the first frame update
        void Start()
        {
            for (int i = 0; i < positionGroup.childCount; i++)
                positions.Add(positionGroup.GetChild(i));


        }


        // void OnTriggerStay(Collider other)
        // {
        //     if (ballOwnerIndex < 0)
        //         return;
                

        //     PlayerAI player = other.GetComponent<PlayerAI>();

        //     if (!player)
        //         return;

        //     if (player.TeamAI.Home != rootHelper.Home)
        //         return;

        //     // Check index
        //     if (ballOwnerIndex >= 0)
        //     {
        //         int index = player.TeamAI.Players.ToList().FindIndex(p => p == player);
        //         if (index != ballOwnerIndex)
        //             return;
        //         // Is the right player
        //         if (player.HasBall)
        //             rootHelper.AddTrigger(this);
        //         else
        //             rootHelper.RemoveTrigger(this);
        //     }
        

        // }

        void OnTriggerEnter(Collider other)
        {

            if (ballOwnerIndex < 0)
            {
#if TEST_AI
                TestBallController ballCtrl = other.GetComponent<TestBallController>();
#else
            BallController ballCtrl = other.GetComponent<BallController>();
#endif

                if (ballCtrl)
                    rootHelper.AddTrigger(this);
            }
            else
            {
                PlayerAI player = other.GetComponent<PlayerAI>();

                if (!player)
                    return;

                if (player.TeamAI.Home != rootHelper.Home)
                    return;

                int index = player.TeamAI.Players.ToList().FindIndex(p => p == player);
                if (index != ballOwnerIndex)
                    return;

                rootHelper.AddTrigger(this);
            }

        }

        void OnTriggerExit(Collider other)
        {
            if (ballOwnerIndex < 0)
            {
#if TEST_AI
                TestBallController ballCtrl = other.GetComponent<TestBallController>();
#else
                BallController ballCtrl = other.GetComponent<BallController>();
#endif

                if (ballCtrl)
                    rootHelper.RemoveTrigger(this);
            }
            else
            {
                PlayerAI player = other.GetComponent<PlayerAI>();

                if (!player)
                    return;

                if (player.TeamAI.Home != rootHelper.Home)
                    return;

                if (ballOwnerIndex >= 0)
                {
                    int index = player.TeamAI.Players.ToList().FindIndex(p => p == player);
                    if (index != ballOwnerIndex)
                        return;
                    // Right player
                    rootHelper.RemoveTrigger(this);
                }    
            }
            
            

        }
    }
    
}
