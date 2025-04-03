#if !UNITY_SERVER
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WLL_NGO.Netcode;

namespace WLL_NGO.UI
{
    public class PlayerMarker : MonoBehaviour
    {
        [SerializeField]
        GameObject root;

        [SerializeField]
        GameObject bottom;


        [SerializeField]
        Color localColor, remoteColor;

        PlayerInfo playerInfo;
        public PlayerInfo PlayerInfo
        {
            get{ return playerInfo; }
        }

        TeamController teamController;
        public TeamController TeamController
        {
            get{ return teamController; }
        }

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        void LateUpdate()
        {
            if (!teamController.IsSpawned) return;

            var selectedPlayer = teamController.SelectedPlayer;

            if (selectedPlayer)
            {
                if (!root.activeSelf)
                    root.SetActive(true);

                //  Adjust position
                Vector3 position = selectedPlayer.Position;
                position.y = 0;
                transform.position = position;

                // Adjust orientation
                Vector3 targetV = Vector3.forward * (playerInfo.IsLocal ? 1f : -1);
                transform.right = targetV;

                
            }
            else
            {
                if (root.activeSelf)
                    root.SetActive(false);
            }
            
        }


        public void Init(PlayerInfo playerInfo)
        {
            this.playerInfo = playerInfo;

            teamController = playerInfo.Home ? TeamController.HomeTeam : TeamController.AwayTeam;

            Color color = localColor;
            if (!playerInfo.IsLocal || playerInfo.Bot)
                color = remoteColor;
            
            bottom.GetComponent<SpriteRenderer>().color = color;
        }
    }
    
}
#endif