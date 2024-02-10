#if USE_LOBBY_SCENE
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace WLL_NGO.Gameplay
{
    public class LobbyController : MonoBehaviour
    {
        ushort numOfPlayers = 1;
        public ushort NumberOfPlayers
        {
            get { return numOfPlayers; }
        }

        bool loadingGameScene = false;

  
        // Update is called once per frame
        void Update()
        {
            if (NetworkManager.Singleton.IsServer)
            {
                if (!loadingGameScene && MatchCanStart())
                {
                    loadingGameScene = true;
                    NetworkManager.Singleton.SceneManager.LoadScene(Constants.DefaultGameScene, UnityEngine.SceneManagement.LoadSceneMode.Single);
                }
            }
        }

        bool MatchCanStart()
        {
            if (PlayerInfoManager.Instance.PlayerCount() < numOfPlayers || !PlayerInfoManager.Instance.PlayerInitializedAll())
                return false;

            return true;
        }
      
    }

}
#endif