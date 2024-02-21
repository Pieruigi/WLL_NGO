using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestAnim : MonoBehaviour
{
    float elapsed = 0;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        elapsed += Time.deltaTime;
        if(elapsed > 1 )
        {
            Animator animator = GetComponent<Animator>();
            elapsed = 0;
           
        }
    }
}
