using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DriveController : MonoBehaviour
{
    public float _acceleration;
    public float _decelerationFromDrag;
    public float _maxRpm;
    public float _gearRatio;
    public float _maxTurnAngle;
    public float _frontDefaultStiffness;
    public float _frontPowerslideStiffness;
    public float _rearDefaultStiffness;
    public float _rearPowerslideStiffness;
    public bool _isFrontWheel;
    private WheelCollider _wheelCollider;
    private Rigidbody _rigidbody;
    private float _gasAmount;
    private float _turnAmount;
    private bool _powerslidePressed;
    private bool _powerslideReleased;

    public Transform _wheelModel;

    // Start is called before the first frame update
    void Start()
    {
        _wheelCollider = this.GetComponent<WheelCollider>();
        _rigidbody = this.GetComponentInParent<Rigidbody>();
        //_rigidbody.centerOfMass += new Vector3(0.0f, -1.0f, 0.0f);
        ApplyDefaultPhysicsSettings();
    }

    // Calling the inputs on every frame that's run so that we don't eat player
    // inputs by only checking during FixedUpdate()
    void Update()
    {
        _gasAmount = Input.GetKey(KeyCode.Space) ? 1 : 0;
        _turnAmount = Input.GetAxis("Horizontal");
        _powerslidePressed = Input.GetKeyDown(KeyCode.A);
        _powerslideReleased = Input.GetKeyUp(KeyCode.A);
    }

    // Running physics adjustments to operate the wheel during FixedUpdate() to
    // ensure consistent acceleration and during regardless of the player frame
    // rate. (Don't want 120 FPS to get twice the accel of 60 FPS)
    void FixedUpdate()
    {
        ApplyGasAcceleration();
        //ApplyDecelerationFromDrag();
        AdjustTurnAngles();
        if (_powerslidePressed)
        {
            ApplyPowerslidePhysicsSettings();
        }
        if (_powerslideReleased)
        {
            ApplyDefaultPhysicsSettings();
        }
        UpdateWheelPose();

        WheelHit wheelHit;
        _wheelCollider.GetGroundHit(out wheelHit);
        if(wheelHit.forwardSlip > 0 || wheelHit.sidewaysSlip > 0)
        {
            Debug.Log($"Forward: {wheelHit.forwardSlip}, Sideways: {wheelHit.sidewaysSlip}");
        }
        
    }

    private void ApplyGasAcceleration()
    {
        if(_wheelCollider.rpm < _maxRpm)
        {
            _wheelCollider.motorTorque = _acceleration * _gasAmount;
        }
    }

    private void ApplyDecelerationFromDrag()
    {
        if(_wheelCollider.rpm > 0)
        {
            _wheelCollider.brakeTorque =  _rigidbody.drag * _decelerationFromDrag;
        }
    }

    private void AdjustTurnAngles()
    {
        if (_isFrontWheel)
        {
            _wheelCollider.steerAngle = _turnAmount * _maxTurnAngle * (_isFrontWheel ? 1 : -1);
        }
    }

    private void ApplyDefaultPhysicsSettings()
    {
        var sidewaysFrictionSettings = _wheelCollider.sidewaysFriction;
        sidewaysFrictionSettings.stiffness = _isFrontWheel ? _frontDefaultStiffness : _rearDefaultStiffness;
        _wheelCollider.sidewaysFriction = sidewaysFrictionSettings;
    }

    private void ApplyPowerslidePhysicsSettings()
    {
        var sidewaysFrictionSettings = _wheelCollider.sidewaysFriction;
        sidewaysFrictionSettings.stiffness = _isFrontWheel ? _frontPowerslideStiffness : _rearPowerslideStiffness;
        _wheelCollider.sidewaysFriction = sidewaysFrictionSettings;
        
        _rigidbody.AddForce(new Vector3(0, 2000, 0), ForceMode.Impulse);
    }

    private void UpdateWheelPose()
    {
        _wheelModel.Rotate(_wheelCollider.rpm * 6 * Time.deltaTime, 0, 0);
    }
}
