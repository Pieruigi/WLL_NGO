using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WLL_NGO.Netcode;

namespace WLL_NGO.UI
{
    public class BlackScreenUI : MonoBehaviour
    {
        CanvasGroup canvasGroup;

        void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 1;
        }

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        void OnEnable()
        {
            MatchController.Instance.OnStateChanged += HandleOnMatchStateChanged;
        }

        void OnDisable()
        {
            MatchController.Instance.OnStateChanged -= HandleOnMatchStateChanged;
        }

        private void HandleOnMatchStateChanged(int oldState, int newState)
        {
            switch (newState)
            {
                case (int)MatchState.StartingMatch:
                    Hide();
                    break;
            }
        }

        void Show()
        {
            canvasGroup.alpha = 1;
        }

        void Hide()
        {
            canvasGroup.alpha = 0;
        }
    }
    
}
