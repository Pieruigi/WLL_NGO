using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WLL_NGO.Netcode;

namespace WLL_NGO.AI
{
    public class GoalkeeperAI : MonoBehaviour
    {
        PlayerController playerController;

        NetController netController;

        //float keepPositionTollerance = 2;
        //float[] keepPositionCenter;

        float[] netCenter = new float[2];
        float[] netSize = new float[2];
        float[] areaSize = new float[2];

        float keepPositionDistance = 0;

        private void Awake()
        {
            playerController = GetComponent<PlayerController>();
        }

        // Update is called once per frame
        void Update()
        {

        }

        private void OnEnable()
        {
            PlayerController.OnSpawned += HandleOnPlayerSpawned;
        }

        private void OnDisable()
        {
            PlayerController.OnSpawned -= HandleOnPlayerSpawned;
        }

        void HandleOnPlayerSpawned(PlayerController playerController)
        {
            if(this.playerController == playerController)
            {
                netController = NetController.GetTeamNetController(TeamController.GetPlayerTeam(playerController));
                netCenter[0] = netController.Position.x;
                netCenter[1] = netController.Position.z;
                netSize[0] = netController.Width;
                netSize[1] = netController.Height;
                areaSize[0] = FieldSizeInfo.AreaSize[0];
                areaSize[1] = FieldSizeInfo.AreaSize[1];
                keepPositionDistance = areaSize[1] * .5f;
            }
        }


    }

}
