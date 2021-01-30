using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DriveController : MonoBehaviour
{
    [System.Serializable]
    public class Wheel
    {
        public WheelInfo info;
        public WheelCollider collider;
        public Transform model;
    }

    [System.Serializable]
    public struct WheelInfo
    {
        public float rpm;
        public Vector3 position;
	    public Quaternion rotation;
        public bool isFrontWheel;
        public bool isRightWheel;
    }

    [System.Serializable]
    public struct DriveSettings {
        public float forwardAcceleration;
        public float reverseAcceleration;
        public float brakingDeceleration;
        public float decelerationFromDrag;
        public float forwardMaxRpm;
        public float reverseMaxRpm;
        public float gearRatio;
        public float maxTurnAngle;
        public float stabilizingForce;
        public Vector3 centerOfGravityOffset;
        public float powerslideJumpForce;
        public FrictionSettings frontDefaultForwardFriction;
        public FrictionSettings frontDefaultSidewaysFriction;
        public FrictionSettings frontPowerslideForwardFriction;
        public FrictionSettings frontPowerslideSidewaysFriction;
        public FrictionSettings rearDefaultForwardFriction;
        public FrictionSettings rearDefaultSidewaysFriction;
        public FrictionSettings rearPowerslideForwardFriction;
        public FrictionSettings rearPowerslideSidewaysFriction;
    }

    [System.Serializable]
    public struct FrictionSettings
    {
        public float extremumSlip;
        public float extremumValue;
        public float asymptoteSlip;
        public float asymptoteValue;
        public float stiffness;
    }

    public DriveSettings _driveSettings;
    public List<Wheel> _wheels;
    private Rigidbody _rigidbody;
    private float _gasAmount;
    private float _brakeAmount;
    private float _turnAmount;
    private bool _powerslidePressed;
    private bool _powerslideReleased;

    // Start is called before the first frame update
    void Start()
    {
        _rigidbody = this.GetComponent<Rigidbody>();
        _rigidbody.centerOfMass += _driveSettings.centerOfGravityOffset;
        foreach(var wheel in _wheels)
        {
            wheel.collider.ConfigureVehicleSubsteps(1.0f, 120, 120);
            ApplyDefaultPhysicsSettings(wheel);
        }
    }

    // Calling the inputs on every frame that's run so that we don't eat player
    // inputs by only checking during FixedUpdate()
    void Update()
    {
        _gasAmount = Input.GetKey(KeyCode.Space) || Input.GetButton("Gas") ? 1 : 0;
        _brakeAmount = Input.GetKey(KeyCode.F) || Input.GetButton("Brake") ? 1 : 0;
        _turnAmount = Input.GetAxis("Horizontal");
        _powerslidePressed = Input.GetKeyDown(KeyCode.A) || Input.GetButtonDown("Powerslide");
        _powerslideReleased = Input.GetKeyUp(KeyCode.A) || Input.GetButtonUp("Powerslide");
    }

    // Running physics adjustments to operate the wheel during FixedUpdate() to
    // ensure consistent acceleration and during regardless of the player frame
    // rate. (Don't want 120 FPS to get twice the accel of 60 FPS)
    void FixedUpdate()
    {
        foreach(var wheel in _wheels)
        {
            ApplyGasAcceleration(wheel);
            ApplyBraking(wheel);
            ApplyDecelerationFromDrag(wheel);
            AdjustTurnAngles(wheel);
            if (_powerslidePressed)
            {
                ApplyPowerslidePhysicsSettings(wheel);
            }
            if (_powerslideReleased)
            {
                Debug.Log("Powerslide released!  Restoring to defaults...");
                ApplyDefaultPhysicsSettings(wheel);
            }
            //ApplyStabilizationForce(wheel);
            
            UpdateWheelPose(wheel);
            wheel.info.rpm = wheel.collider.rpm;
        } 
    }

    private void ApplyGasAcceleration(Wheel wheel)
    {
        if(wheel.collider.rpm < _driveSettings.forwardMaxRpm)
        {
            wheel.collider.brakeTorque = 0;
            wheel.collider.motorTorque = _driveSettings.forwardAcceleration * _gasAmount;
        }
    }

    private void ApplyBraking(Wheel wheel)
    {
        if(_brakeAmount > 0)
        {
            if(wheel.collider.rpm <= 0)
            {
                wheel.collider.brakeTorque = 0;
                ApplyReverseAcceleration(wheel);
            }
            else
            {
                wheel.collider.brakeTorque = _driveSettings.brakingDeceleration;
            }
        }
    }

    private void ApplyReverseAcceleration(Wheel wheel)
    {
        if(wheel.collider.rpm > _driveSettings.reverseMaxRpm)
        {
            wheel.collider.motorTorque = _driveSettings.reverseAcceleration;
        }
    }

    private void ApplyDecelerationFromDrag(Wheel wheel)
    {
        if(wheel.collider.rpm > 0)
        {
            wheel.collider.motorTorque -=  _rigidbody.drag * _driveSettings.decelerationFromDrag;
        }
    }

    private void AdjustTurnAngles(Wheel wheel)
    {
        wheel.collider.steerAngle = _turnAmount * 
            _driveSettings.maxTurnAngle * 
            (wheel.info.isFrontWheel ? 1 : -1);
    }

    private void ApplyStabilizationForce(Wheel wheel)
    {
        Wheel oppositeWheel;
        WheelHit wheelHit, oppositeWheelHit;
        float wheelTravelDistance, oppositeWheelTravelDistance;
        oppositeWheel = GetOppositeWheelOnAxle(wheel);
        bool wheelGrounded = wheel.collider.GetGroundHit(out wheelHit);
        bool oppositeWheelGrounded = oppositeWheel.collider.GetGroundHit(out oppositeWheelHit);
        wheelTravelDistance = wheelGrounded ? 
            (wheel.collider.transform.InverseTransformPoint(wheelHit.point).y - wheel.collider.radius) / wheel.collider.suspensionDistance :
            1.0f;
        oppositeWheelTravelDistance = oppositeWheelGrounded ? 
            (wheel.collider.transform.InverseTransformPoint(oppositeWheelHit.point).y - oppositeWheel.collider.radius) / oppositeWheel.collider.suspensionDistance :
            1.0f;
        float antiRollForce = (wheelTravelDistance - oppositeWheelTravelDistance) * 
            _driveSettings.stabilizingForce *
            (wheel.info.isRightWheel ? -1 : 1);

        if(wheelGrounded)
        {
            _rigidbody.AddForceAtPosition(wheel.collider.transform.up * -antiRollForce,
                wheel.collider.transform.position);
        }
        if(oppositeWheelGrounded)
        {
            _rigidbody.AddForceAtPosition(oppositeWheel.collider.transform.up * antiRollForce,
                oppositeWheel.collider.transform.position);
        }

        var forwardFrictionLimit = wheel.collider.forwardFriction.extremumSlip;
        var sidewaysFrictionLimit = wheel.collider.sidewaysFriction.extremumSlip;
        if(wheelHit.forwardSlip > forwardFrictionLimit || wheelHit.sidewaysSlip > forwardFrictionLimit)
        {
            //Debug.Log($"Slipping! Forward: {wheelHit.forwardSlip}, Sideways: {wheelHit.sidewaysSlip}");
        }
    }

    private void ApplyDefaultPhysicsSettings(Wheel wheel)
    {
        var defaultForwardFrictionSettings = wheel.info.isFrontWheel ? 
            _driveSettings.frontDefaultForwardFriction : _driveSettings.rearDefaultForwardFriction;
        var defaultSidewaysFrictionSettings = wheel.info.isFrontWheel ? 
            _driveSettings.frontDefaultSidewaysFriction : _driveSettings.rearDefaultSidewaysFriction;
        
        wheel.collider.forwardFriction = GetFrictionCurveFromSettings(defaultForwardFrictionSettings);
        wheel.collider.sidewaysFriction = GetFrictionCurveFromSettings(defaultSidewaysFrictionSettings);
    }

    private void ApplyPowerslidePhysicsSettings(Wheel wheel)
    {
        var powerslideForwardFrictionSettings = wheel.info.isFrontWheel ? 
            _driveSettings.frontPowerslideForwardFriction : _driveSettings.rearPowerslideForwardFriction;
        var powerslideSidewaysFrictionSettings = wheel.info.isFrontWheel ? 
            _driveSettings.frontPowerslideSidewaysFriction : _driveSettings.rearPowerslideSidewaysFriction;
        
        wheel.collider.forwardFriction = GetFrictionCurveFromSettings(powerslideForwardFrictionSettings);
        wheel.collider.sidewaysFriction = GetFrictionCurveFromSettings(powerslideSidewaysFrictionSettings);
        
        _rigidbody.AddForce(_rigidbody.transform.up * _driveSettings.powerslideJumpForce, ForceMode.Impulse);
        //_rigidbody.transform.rotation = new Quaternion(0, 30 * _turnAmount >= 0 ? 1 : -1, 0, 0);
    }

    private WheelFrictionCurve GetFrictionCurveFromSettings(FrictionSettings settings)
    {
        return new WheelFrictionCurve {
            extremumSlip = settings.extremumSlip,
            extremumValue = settings.extremumValue,
            asymptoteSlip = settings.asymptoteSlip,
            asymptoteValue = settings.asymptoteValue,
            stiffness = settings.stiffness
        };
    }

    private Wheel GetOppositeWheelOnAxle(Wheel wheel)
    {
        return _wheels.Where(opp => 
            opp.info.isFrontWheel == wheel.info.isFrontWheel && 
            opp.info.isRightWheel != wheel.info.isRightWheel).FirstOrDefault();
    }

    private void UpdateWheelPose(Wheel wheel)
    {
        wheel.collider.GetWorldPose(out wheel.info.position, out wheel.info.rotation);
        //wheel.model.position = wheel.info.position;
        wheel.model.rotation = wheel.info.rotation;
    }
}
