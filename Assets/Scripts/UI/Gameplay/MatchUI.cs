using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using WLL_NGO.Netcode;

namespace WLL_NGO.UI
{
    public class MatchUI : MonoBehaviour
    {
        [SerializeField]
        TMP_Text timerField;

        [SerializeField]
        TMP_Text localScoreField;

        [SerializeField]
        TMP_Text remoteScoreField;

        TeamController localTeam, remoteTeam;


        // Start is called before the first frame update
        void Start()
        {
            localTeam = PlayerInfoManager.Instance.GetLocalPlayerInfo(false).Home ? TeamController.HomeTeam : TeamController.AwayTeam;
            remoteTeam = localTeam.Home ? TeamController.AwayTeam : TeamController.HomeTeam;
   
        }

        // Update is called once per frame
        void Update()
        {
            if (!MatchController.Instance || !MatchController.Instance.IsSpawned) return;

            // Show timer
            ShowTimer();

            // Update score
            UpdateScore();
        }

        void ShowTimer()
        {
            TimeSpan ts = TimeSpan.FromSeconds(MatchRuler.Instance.Timer);
            timerField.text = $"{(int)ts.TotalMinutes:D2}:{(int)ts.Seconds:D2}";
        }

        

        private void UpdateScore()
        {
            localScoreField.text = localTeam.Score.ToString();
            remoteScoreField.text = remoteTeam.Score.ToString();


        }
    }
    
}
