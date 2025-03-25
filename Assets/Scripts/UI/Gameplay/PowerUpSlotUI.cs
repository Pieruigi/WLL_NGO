using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WLL_NGO.UI
{
    public class PowerUpSlotUI : MonoBehaviour
    {
        [SerializeField]
        Image emptyImage;

        [SerializeField]
        Image powerImage;

        [SerializeField]
        Image glowImage;

        void Awake()
        {
            SetEmpty();
        }

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        public void SetEmpty()
        {
            emptyImage.gameObject.SetActive(true);
            powerImage.gameObject.SetActive(false);
            //glowImage.gameObject.SetActive(false);
        }

        public void SetPower(Sprite sprite)
        {
            powerImage.sprite = sprite;
            emptyImage.gameObject.SetActive(false);
            //glowImage.gameObject.SetActive(false);
            powerImage.gameObject.SetActive(true);
        }
    }
    
}
