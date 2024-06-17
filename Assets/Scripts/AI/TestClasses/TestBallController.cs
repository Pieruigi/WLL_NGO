using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WLL_NGO.AI.Test
{
    public class TestBallController : MonoBehaviour
    {
        public static TestBallController Instance { get; private set; }

        private void Awake()
        {
            if (!Instance)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public Vector3 Position
        {
            get { return transform.position; }
            //set { transform.position = value; }
        }
                
    }

}
