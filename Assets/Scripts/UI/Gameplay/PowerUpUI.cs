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

        void Start()
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
        }

        
        void OnDestroy()
        {
            if (MatchRuler.Instance.GameMode == GameMode.Powered)
            {
                PowerUpManager.OnPowerUpPushed -= HandleOnPowerUpPushed;
                PowerUpManager.OnPowerUpPopped -= HandleOnPowerUpPopped;
            }

        }

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
