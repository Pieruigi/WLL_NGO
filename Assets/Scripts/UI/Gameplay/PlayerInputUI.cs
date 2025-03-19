using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using WLL_NGO.Netcode;

namespace WLL_NGO.UI
{
    public class PlayerInputUI : Singleton<PlayerInputUI>
    {
        [SerializeField]
        CanvasGroup canvasGroup;

        [SerializeField]
        GameButton button1, button2;

        TeamController teamController;

        [SerializeField]
        Joystick joystick;


        // Start is called before the first frame update
        void Start()
        {
            canvasGroup.alpha = 0;
            Show();    
        }

        // Update is called once per frame
        void Update()
        {
            CheckInput();
         
        }

        void OnEnable()
        {
            PlayerInfo.OnReadyChanged += HandleOnPlayerReadyChanged;
        }


        void OnDisable()
        {
            PlayerInfo.OnReadyChanged -= HandleOnPlayerReadyChanged;
        }

        private void HandleOnPlayerReadyChanged(PlayerInfo playerInfo)
        {
            if (!playerInfo.IsLocal && !playerInfo.Bot)
                return;
            teamController = playerInfo.Home ? TeamController.HomeTeam : TeamController.AwayTeam;
        }

        void CheckInput()
        {
            if (!MatchController.Instance || !MatchController.Instance.IsSpawned) return;

            if (MatchController.Instance.MatchState != MatchState.Playing && MatchController.Instance.MatchState != MatchState.KickOff) return;

            if (!teamController) return;

            if (!teamController.SelectedPlayer) return;

            var ih = teamController.SelectedPlayer.GetInputHandler();

            switch (MatchController.Instance.MatchState)
            {
                case MatchState.Playing:
                    //ih.SetJoystick(new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")).normalized);
                    ih.SetJoystick(joystick.Direction);
                    //ih.SetButton1(Input.GetKey(KeyCode.Keypad1));
                    ih.SetButton1(button1.GetButton());
                    //ih.SetButton2(Input.GetKey(KeyCode.Keypad2));
                    ih.SetButton2(button2.GetButton());
                    ih.SetButton3(false);
                    break;
                case MatchState.KickOff:
                    ih.SetJoystick(Vector2.zero);
                    //ih.SetButton1(Input.GetKey(KeyCode.Keypad1));
                    if(teamController.SelectedPlayer && teamController.SelectedPlayer.HasBall)
                        ih.SetButton1(button1.GetButton());
                    else
                        ih.SetButton1(false);
                    ih.SetButton2(false);
                    ih.SetButton3(false);
                    break;
            }

           
        }

        public void Show()
        {
            canvasGroup.alpha = 1;
        }

        public void Hide()
        {
            canvasGroup.alpha = 0;
        }
    }
    
}
