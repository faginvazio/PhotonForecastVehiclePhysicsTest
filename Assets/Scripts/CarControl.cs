using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

public class CarControl : NetworkBehaviour
{
    [Header("Car Properties")]
    public float motorTorque = 2000f;
    public float brakeTorque = 2000f;
    public float maxSpeed = 20f;
    public float steeringRange = 30f;
    public float steeringRangeAtMaxSpeed = 10f;
    public float centreOfGravityOffset = -1f;

    [Networked] private float vInput { get; set; }
    [Networked] private float hInput { get; set; }



    private WheelControl[] wheels;
    private Rigidbody rigidbody;

    [Header("Input Actions")]
    public InputActionReference moveAction;

    private void OnEnable()
    {
        moveAction.action.Enable();
    }

    private void OnDisable()
    {
        moveAction.action.Disable();
    }

    private void Start()
    {
        rigidbody = GetComponent<Rigidbody>();

        // Adjust center of mass to impove stability and prevent rolling
        Vector3 centerOfMass = rigidbody.centerOfMass;
        centerOfMass.y += centreOfGravityOffset;
        rigidbody.centerOfMass = centerOfMass;

        // Get all wheel components attatched to the car
        wheels = GetComponentsInChildren<WheelControl>();
    }

    private void FixedUpdate()
    {
        if (Object.HasStateAuthority)
        {
            //vInput = Input.GetAxis("Vertical");
            //hInput = Input.GetAxis("Horizontal");

            vInput = moveAction.action.ReadValue<Vector2>().y;  // Forward/Backward input
            hInput = moveAction.action.ReadValue<Vector2>().x;  // Steering input
        }

        //// Get player input for acceleration and steering
        //float vInput = moveAction.action.ReadValue<Vector2>().y;  // Forward/Backward input
        //float hInput = moveAction.action.ReadValue<Vector2>().x;  // Steering input

        // Calculate current speed along the car's forward axis
        float forwardSpeed = Vector3.Dot(transform.forward, rigidbody.linearVelocity);
        float speedFactor = Mathf.InverseLerp(0, maxSpeed, Mathf.Abs(forwardSpeed));  // Normalized speed factor

        // Reduce motor torque and steering at high speeds for better handling
        float currentMotorTorque = Mathf.Lerp(motorTorque, 0, speedFactor);
        float currentSteerRange = Mathf.Lerp(steeringRange, steeringRangeAtMaxSpeed, speedFactor);

        // Determine if the player is accelerating or trying to reverse
        bool isAccelerating = Mathf.Sign(vInput) == Mathf.Sign(forwardSpeed);

        foreach (var wheel in wheels)
        {
            if (!wheel.WheelCollider)
            {
                continue;
            }


            // Apply steering to wheels that support steering
            if (wheel.steerable)
            {
                wheel.WheelCollider.steerAngle = hInput * currentSteerRange;
            }

            if (isAccelerating)
            {
                // Apply torque to motorized wheels
                if (wheel.motorized)
                {
                    wheel.WheelCollider.motorTorque = vInput * currentMotorTorque;
                }

                // Release brakes when accelerating
                wheel.WheelCollider.brakeTorque = 0f;
            }
            else
            {
                // Apply brakes when reversing direction
                wheel.WheelCollider.motorTorque = 0f;
                wheel.WheelCollider.brakeTorque = Mathf.Abs(vInput) * brakeTorque;
            }
        }
    }
}
