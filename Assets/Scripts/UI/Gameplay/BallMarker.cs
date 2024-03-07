using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using WLL_NGO.Netcode;

namespace WLL_NGO.UI
{
    public class BallMarker : MonoBehaviour
    {
        [SerializeField]
        SpriteRenderer backgroundRenderer;

        [SerializeField]
        SpriteRenderer cursorRenderer;

        [SerializeField]
        SpriteRenderer perfectRenderer;

        BallController ball;

        bool hasReceiver = false;
        bool hasOwner = false;
        bool ballSpawned = false;

        bool backgroundActive = true;
        bool cursorActive = true;
        bool perfectActive = true;

        private void Start()
        {
            // Destroy the object on the dedicated server
            if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsHost)
            {
                Destroy(gameObject);
                return;
            }

            RegisterCallbacks();
            HideAll();
          
        }

        private void Update()
        {
            if (!ballSpawned)
                return;

            if (!hasReceiver && !hasOwner)
            {
                HideAll();
                return;
            }
            
            if(hasReceiver && !hasOwner)
            {
                
                //Debug.Log($"UI - Timing:{value}");
                if (!hasOwner)
                {
                    Vector3 initialPosition = BallController.Instance.GetShootingDataInitialPosition();
                    Vector3 targetPosition = BallController.Instance.GetShootingDataTargetPosition();
                    Vector3 ballPosition = BallController.Instance.Position;
                    float value = InputTimingUtility.GetOnTheFlyNormalizedTimingValue(initialPosition, targetPosition, ballPosition);
                    if (value >= 1)
                    {
                        HideAll();
                    }
                    else
                    {
                        
                        UpdateCursor(value);
                        UpdatePerfect(value);
                    }
                        
                }
                
            }
        }

        private void LateUpdate()
        {
            if (!ballSpawned)
                return;

            // Set position
            Vector3 pos  = BallController.Instance.Position;
            pos.y = 0;
            transform.position = pos;
            
        }

        private void OnDestroy()
        {
            UnregisterCallbacks();
        }

        void RegisterCallbacks()
        {
            BallController.OnBallSpawned += HandleOnBallSpawned;
            BallController.OnShoot += HandleOnShoot;
            BallController.OnOwnerChanged += HandleOnOwnerChanged;
        }

        void UnregisterCallbacks()
        {
            BallController.OnBallSpawned -= HandleOnBallSpawned;
            BallController.OnShoot -= HandleOnShoot;
            BallController.OnOwnerChanged -= HandleOnOwnerChanged;
        }

        void UpdateCursor(float value)
        {
            value = Mathf.Clamp01(value);   
            if(value < .5f)
                cursorRenderer.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, value / .5f);
            else
                cursorRenderer.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, (1f-value) / .5f );
        }

        void UpdatePerfect(float value)
        {

        }

        void HandleOnShoot()
        {
            hasReceiver = BallController.Instance.GetShootingDataReceiver();

            if (hasReceiver)
            {
                ActivateBackground(true);
                ActivateCursor(true);
                ActivatePerfect(false);
            }

        }

        void HandleOnOwnerChanged()
        {
            //Debug.Log($"Owner changed:{BallController.Instance.Owner}");

            hasOwner = BallController.Instance.Owner != null;

            if(hasOwner)
            {
                ActivateBackground(true);
                ActivateCursor(false);
                ActivatePerfect(false);
            }
        }

        void HandleOnBallSpawned()
        {
            ballSpawned = true;
        }

        void HideAll()
        {
            ActivateBackground(false);
            ActivateCursor(false);
            ActivatePerfect(false);
        }
        
        async void ActivateBackground(bool value)
        {
            if (backgroundActive == value)
                return;
            backgroundActive = value;
            
            // To avoid background flickering in and out during on the fly combos
            if (!value)
            {
                await Task.Delay(TimeSpan.FromSeconds(.5f));
                
            }
            if (value == backgroundActive)
                backgroundRenderer.enabled = value;

        }
        void ActivateCursor(bool value)
        {
            if (cursorActive == value)
                return;
            cursorActive = value;
            cursorRenderer.enabled = value;
            cursorRenderer.transform.localScale = Vector3.zero;
            
        }
        void ActivatePerfect(bool value)
        {
            if (perfectActive == value)
                return;
            perfectActive = value;
            perfectRenderer.enabled = value;
        }
    }

}
