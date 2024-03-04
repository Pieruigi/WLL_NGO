using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WLL_NGO.Netcode;

namespace WLL_NGO.UI
{
    public class BallPassageTimingBar : MonoBehaviour
    {
        BallController ball;

        private void Awake()
        {
            ball = GetComponent<BallController>();
        }

        private void OnEnable()
        {
            BallController.OnShoot += HandleOnShoot;
        }

        private void OnDisable()
        {
            BallController.OnShoot -= HandleOnShoot;
        }

        void HandleOnShoot()
        {

        }
    }

}
