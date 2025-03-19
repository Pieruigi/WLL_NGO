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
            canvasGroup = GetComponentInChildren<CanvasGroup>();
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
            MatchController.OnStateChanged += HandleOnMatchStateChanged;
        }

        void OnDisable()
        {
            MatchController.OnStateChanged -= HandleOnMatchStateChanged;
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
            canvasGroup.blocksRaycasts = true;
        }

        void Hide()
        {
            canvasGroup.alpha = 0;
            canvasGroup.blocksRaycasts = false;
        }
    }
    
}
