#if !UNITY_SERVER
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using WLL_NGO.Netcode;

namespace WLL_NGO.UI
{
    public class StaminaBar : MonoBehaviour
    {
        [SerializeField]
        GameObject filler;

        SpriteRenderer fillerRenderer;

        PlayerMarker marker;

        float fillerMaxWidth;

        float baseHeight;

        void Awake()
        {
            fillerRenderer = filler.GetComponent<SpriteRenderer>();
            marker = GetComponentInParent<PlayerMarker>();
            fillerMaxWidth = fillerRenderer.size.x;
            baseHeight = transform.localPosition.y;
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
            if (!marker.PlayerInfo.IsSpawned) return;

            if (!marker.TeamController.IsSpawned) return;

            var selected = marker.TeamController.SelectedPlayer;

            if (!selected) return;

            // Only adjust height offset
            Vector3 localPosition = transform.localPosition;
            localPosition.y = baseHeight + selected.transform.position.y;
            transform.localPosition = localPosition;
            

            // Adjust filler width
            float ratio = selected.CurrentStamina / selected.MaxStamina;
            var newSize = fillerRenderer.size;
            newSize.x = Mathf.Lerp(fillerMaxWidth, 0, ratio);
            fillerRenderer.size = newSize;
        }
    }
    
}
#endif