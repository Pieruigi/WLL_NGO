#if !UNITY_SERVER
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using WLL_NGO;
using WLL_NGO.Netcode;


public class LogUI : MonoBehaviour
{
    [SerializeField]
    TMP_Text log;

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
        MatchController.OnStateChanged += HandleOnStateChanged;
    }

    void OnDisable()
    {
          MatchController.OnStateChanged -= HandleOnStateChanged;
    }

    private void HandleOnStateChanged(int oldState, int newState)
    {
        switch (newState)
        {
            case (int)MatchState.Playing:
                log.text = "";
                break;
            case (int)MatchState.KickOff:
                log.text = "Kick Off";
                break;
            case (int)MatchState.Goal:
                log.text = "Celebration";
                break;
            case (int)MatchState.Replay:
                log.text = "Replay";
                break;
            case (int)MatchState.End:
                log.text = "Final Whistle";
                break;
        }
    }
}
#endif