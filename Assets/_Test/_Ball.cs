using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public class _Ball : MonoBehaviour
{
    [SerializeField]
    float maxSpeed = 10f;

    [SerializeField]
    Transform target;

    [SerializeField]
    Slider velSlider;

    [SerializeField]
    TMPro.TMP_Text velText;

    Rigidbody rb;

    [SerializeField]
    float speed = 1.0f;

    /// <summary>
    /// Formula: currentEffectSpeed = effectSpeed * ( 1 - 2t ), where t is normalized between 0 and 1
    /// </summary>
    Vector3 effectVelocity, currentEffectVelocity;
    float effectTime, currentEffectTime;
    


    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        Debug.Log($"sin(30):{math.sin(math.radians(30))}");
        velSlider.onValueChanged.AddListener((v) => { speed = v; velText.text = speed.ToString(); });
        speed = velSlider.value;
        velText.text = speed.ToString();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            
            Vector3 vel = ComputeVelocity(rb.position, target.position, speed, maxSpeed,  1f, 8f);
            Debug.Log($"Velocity:{vel}");
            rb.velocity = vel;
        }
            
    }

    private void FixedUpdate()
    {
        if(effectTime > 0) 
        {
            // Remove the old effect velocity
            rb.velocity -= currentEffectVelocity;
            currentEffectVelocity = effectVelocity * (1f - 2f * currentEffectTime / effectTime);
            currentEffectTime += Time.fixedDeltaTime;
            // Add the updated effect velocity
            rb.velocity += currentEffectVelocity;
            if(currentEffectTime >= effectTime )
                effectTime = 0;
        }
    }

    public Vector3 ComputeVelocity(Vector3 ballPosition, Vector3 targetPosition, float speed, float maxSpeed, float effect, float maxEffectSpeed)
    {

        // If we want to reach a target T we must aim to T1 higher than T because of the gravity.
        // We apply the formula V(y) = (1/2 * g * t) +/- (d*sin(b))/t ( sign depending whether the ball is higher or lower than the target )
        // - V(y) is the vertical velocity taking into account the gravity ( towards T1 )
        // - g is the gravity acceleration
        // - t is the time it will take to reach the original target T depending on the speed
        // - b is the angle between the original direction ( towards T ) and the horizontal plane
        // Basically our vertical velocity will be made by two components: one to reach the target and anothersla to contrast the gravity 

        Vector3 direction = targetPosition - ballPosition;
        Vector3 dirOnFloor = Vector3.ProjectOnPlane(direction, Vector3.up);
        float b = Vector3.Angle(direction, dirOnFloor);

        // Get the time it takes to reach the target 
        float t = direction.magnitude / speed;


        Vector3 velE = Vector3.zero;
        // We apply some effect to the ball.
        if (effect != 0)
        {
            float eSpeed = math.lerp(0f, maxEffectSpeed, effect);
            //eSpeed = eSpeed * (1f - 2f * t);
            Vector3 eDir = Vector3.Cross(direction, dirOnFloor).normalized;
            velE = eDir * eSpeed;

            effectTime = t;
            currentEffectTime = 0;
            effectVelocity = velE;
            currentEffectVelocity = Vector3.zero;
        }



        // Invert the sign of the first component of the velocity if the ball is higher than the target ( we must move down )
        float sign = ballPosition.y > targetPosition.y ? -1 : 1;

        // Compute horizontal and vertical components
        Vector3 velY = (sign * (direction.magnitude * math.sin(math.radians(b)) / t) + (.5f * math.abs(Physics.gravity.y) * t)) * Vector3.up;
        Vector3 velH = dirOnFloor.normalized * (dirOnFloor.magnitude / t);


        Vector3 vel = velY + velH;
        if (vel.magnitude > maxSpeed)
            vel = vel.normalized * maxSpeed;

        return vel;


    }

    public Vector3 _ComputeVelocity(Vector3 ballPosition, Vector3 targetPosition, float speed, float maxSpeed, float effect, float maxEffectSpeed)
    {

        // If we want to reach a target T we must aim to T1 higher than T because of the gravity.
        // We apply the formula V(y) = (1/2 * g * t) +/- (d*sin(b))/t ( sign depending whether the ball is higher or lower than the target )
        // - V(y) is the vertical velocity taking into account the gravity ( towards T1 )
        // - g is the gravity acceleration
        // - t is the time it will take to reach the original target T depending on the speed
        // - b is the angle between the original direction ( towards T ) and the horizontal plane
        // Basically our vertical velocity will be made by two components: one to reach the target and anothersla to contrast the gravity 

        Vector3 direction = targetPosition - ballPosition;
        Vector3 dirOnFloor = Vector3.ProjectOnPlane(direction, Vector3.up);
        float b = Vector3.Angle(direction, dirOnFloor);
       
        // Get the time it takes to reach the target 
        float t = direction.magnitude / speed;


       

        // Invert the sign of the first component of the velocity if the ball is higher than the target ( we must move down )
        float sign = ballPosition.y > targetPosition.y ? -1 : 1;
        
        // Compute horizontal and vertical components
        Vector3 velY = (sign * (direction.magnitude * math.sin(math.radians(b)) / t) + (.5f * math.abs(Physics.gravity.y) * t)) * Vector3.up;
        Vector3 velH = dirOnFloor.normalized * (dirOnFloor.magnitude / t);
        

        Vector3 vel = velY + velH;
        if(vel.magnitude > maxSpeed)
            vel = vel.normalized * maxSpeed;
       
        return vel;
        
        
    }
}
