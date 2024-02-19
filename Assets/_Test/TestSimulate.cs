using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestSimulate : MonoBehaviour
{
    Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //Physics.simulationMode = SimulationMode.Script;
        //for (int i = 0; i < 100; i++)
        {
            
        }
        //rb.velocity += transform.forward * 10 * Time.fixedDeltaTime;
    }

    private void FixedUpdate()
    {
        Physics.simulationMode = SimulationMode.Script;
        rb.velocity += transform.forward * 10 * Time.fixedDeltaTime;
        Physics.Simulate(Time.fixedDeltaTime);
        //Physics.simulationMode = SimulationMode.FixedUpdate;
    }
}
