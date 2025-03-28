#if !UNITY_SERVER
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Services.Authentication;
using UnityEngine;
using WLL_NGO.Netcode;

namespace WLL_NGO.UI
{
    public class GameUISpawner : MonoBehaviour
    {
        [SerializeField]
        GameObject playerInputUIPrefab;

        [SerializeField]
        GameObject matchUIPrefab;

        [SerializeField]
        GameObject blackScreenUIPrefab;

        [SerializeField]
        GameObject playerMarkerPrefab;

        // Start is called before the first frame update
        void Start()
        {
            Instantiate(playerInputUIPrefab, Vector3.zero, Quaternion.identity);
            Instantiate(matchUIPrefab, Vector3.zero, Quaternion.identity);
            Instantiate(blackScreenUIPrefab, Vector3.zero, Quaternion.identity);

            // Instantiate markers

        }

        // Update is called once per frame
        void Update()
        {

        }

        void OnEnable()
        {
            Netcode.PlayerInfo.OnInitializedChanged += HandleOnPlayerInitialized;
        }

        void OnDisable()
        {
            Netcode.PlayerInfo.OnInitializedChanged -= HandleOnPlayerInitialized;
        }

        private void HandleOnPlayerInitialized(Netcode.PlayerInfo player)
        {
            GameObject pm = Instantiate(playerMarkerPrefab);
            pm.transform.localPosition = Vector3.zero;
            pm.transform.localRotation = Quaternion.identity;
            pm.GetComponent<PlayerMarker>().Init(player);
        }
    }
    
}
#endif