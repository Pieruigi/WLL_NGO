#if !UNITY_SERVER
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEditor.Rendering;
using UnityEngine;
using WLL_NGO.Netcode;

namespace WLL_NGO.UI
{
    public class PowerUpUI : MonoBehaviour
    {
        [SerializeField]
        List<PowerUpSlotUI> localSlots;

        [SerializeField]
        List<PowerUpSlotUI> remoteSlots;



        void OnEnable()
        {
            if (MatchRuler.Instance.GameMode != GameMode.Powered)
            {
                gameObject.SetActive(false);
            }
            else
            {

                PowerUpManager.OnPowerUpPushed += HandleOnPowerUpPushed;
                PowerUpManager.OnPowerUpPopped += HandleOnPowerUpPopped;
            }

            // MatchRuler.OnSpawned += HandleOnMatchRulerSpawned;   
            // MatchRuler.OnDespawned += HandleOnMatchRulerDespawned;   
        }


        void OnDisable()
        {
            if (MatchRuler.Instance.GameMode == GameMode.Powered)
            {
                PowerUpManager.OnPowerUpPushed -= HandleOnPowerUpPushed;
                PowerUpManager.OnPowerUpPopped -= HandleOnPowerUpPopped;
            }
            // MatchRuler.OnSpawned -= HandleOnMatchRulerSpawned;
            // MatchRuler.OnDespawned -= HandleOnMatchRulerDespawned;   
        }

        // private void HandleOnMatchRulerSpawned()
        // {
            
        //     if (MatchRuler.Instance.GameMode != GameMode.Powered)
        //     {
        //         gameObject.SetActive(false);
        //     }
        //     else
        //     {

        //         PowerUpManager.OnPowerUpPushed += HandleOnPowerUpPushed;
        //         PowerUpManager.OnPowerUpPopped += HandleOnPowerUpPopped;
        //     }
        // }

        // private void HandleOnMatchRulerDespawned()
        // {
        //     if (MatchRuler.Instance.GameMode == GameMode.Powered)
        //     {
        //         Debug.Log("RRRRRRRRRRRRRRRRRRRRRRRRRRRRRRR");
        //         PowerUpManager.OnPowerUpPushed -= HandleOnPowerUpPushed;
        //         PowerUpManager.OnPowerUpPopped -= HandleOnPowerUpPopped;
        //     }
           
        // }

        private void HandleOnPowerUpPopped(TeamController team, string powerUpName)
        {
            Refresh(team);
        }

        private void HandleOnPowerUpPushed(TeamController team, string powerUpName)
        {
            Refresh(team);
        }

        void Refresh(TeamController team)
        {
            var powers = team.Home ? PowerUpManager.Instance.HomeTeamPowerUps : PowerUpManager.Instance.AwayTeamPowerUps;
            var slots = team.Home ? localSlots : remoteSlots;

           
            foreach (var slot in slots)
            {
                // Set empty
                slot.SetEmpty();
            }


            for (int i = 0; i < powers.Count; i++)
            {
                var asset = PowerUpManager.Instance.GetPowerUpAssetByName(powers[i].ToString());
           
                // Init slot image
                slots[i].SetPower(asset.Icon);
            }

            
        }
    }
    
}
#endif