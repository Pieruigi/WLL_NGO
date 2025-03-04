#if !UNITY_SERVER
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WLL_NGO.UI
{
    public class GameUISpawner : MonoBehaviour
    {
        [SerializeField]
        GameObject playerInputUIPrefab;



        // Start is called before the first frame update
        void Start()
        {
            Instantiate(playerInputUIPrefab, Vector3.zero, Quaternion.identity);
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
    
}
#endif