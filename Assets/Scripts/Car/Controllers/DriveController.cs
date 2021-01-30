using System;
using UnityEngine;

namespace KartRacer.Car.Controllers
{
    public class DriveController : MonoBehaviour
    {
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
        }

        public DriveSettings _driveSettings;
        private WheelSettings _wheelSettings;
        private Rigidbody _carBody;
        private float _gasAmount;
        private float _brakeAmount;
        private float _turnAmount;

        // Start is called before the first frame update
        void Start()
        {
            _wheelSettings = this.GetComponent<WheelSettings>();
            _carBody = this.GetComponent<Rigidbody>();
            _carBody.centerOfMass += _driveSettings.centerOfGravityOffset;
            foreach(var wheel in _wheelSettings.wheels)
            {
                wheel.collider.ConfigureVehicleSubsteps(1.0f, 12, 12);
            }
        }

        // Calling the inputs on every frame that's run so that we don't eat player
        // inputs by only checking during FixedUpdate()
        void Update()
        {
            _gasAmount = Input.GetKey(KeyCode.Space) || Input.GetButton("Gas") ? 1 : 0;
            _brakeAmount = Input.GetKey(KeyCode.F) || Input.GetButton("Brake") ? 1 : 0;
            _turnAmount = Input.GetAxis("Horizontal");
        }

        // Running physics adjustments to operate the wheel during FixedUpdate() to
        // ensure consistent acceleration and during regardless of the player frame
        // rate. (Don't want 120 FPS to get twice the accel of 60 FPS)
        void FixedUpdate()
        {
            foreach(var wheel in _wheelSettings.wheels)
            {
                ApplyGasAcceleration(wheel);
                ApplyBraking(wheel);
                ApplyDecelerationFromDrag(wheel);
                AdjustTurnAngles(wheel);
                ApplyStabilizationForce(wheel);
                
                UpdateWheelPose(wheel);
                wheel.info.rpm = wheel.collider.rpm;
            } 
        }

        private void ApplyGasAcceleration(Wheel wheel)
        {
            if(wheel.collider.rpm < _driveSettings.forwardMaxRpm)
            {
                wheel.collider.brakeTorque = 0;
                wheel.info.gear = Convert.ToInt32(Mathf.Ceil((wheel.collider.rpm + 0.1f) / 
                    _driveSettings.gearRatio));
                wheel.collider.motorTorque = _driveSettings.forwardAcceleration * 
                    Mathf.Log(wheel.info.gear + 1.35f, 10.0f) *
                    _gasAmount;
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
                wheel.collider.motorTorque -=  _carBody.drag * _driveSettings.decelerationFromDrag;
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
            oppositeWheel = _wheelSettings.GetOppositeWheelOnAxle(wheel);
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
                Debug.Log($@"Applying {-antiRollForce} on axel: {wheel.info.isFrontWheel}
                    side {wheel.info.isRightWheel} wheel");
                _carBody.AddForceAtPosition(wheel.collider.transform.up * -antiRollForce,
                    wheel.collider.transform.position);
            }
            if(oppositeWheelGrounded)
            {
                Debug.Log($@"Applying {-antiRollForce} on {(wheel.info.isFrontWheel ? "front" : "rear")}
                    {(wheel.info.isRightWheel ? "right" : "left")} wheel");
                _carBody.AddForceAtPosition(oppositeWheel.collider.transform.up * antiRollForce,
                    oppositeWheel.collider.transform.position);
            }

            var forwardFrictionLimit = wheel.collider.forwardFriction.extremumSlip;
            var sidewaysFrictionLimit = wheel.collider.sidewaysFriction.extremumSlip;
            if(wheelHit.forwardSlip > forwardFrictionLimit || wheelHit.sidewaysSlip > forwardFrictionLimit)
            {
                //Debug.Log($"Slipping! Forward: {wheelHit.forwardSlip}, Sideways: {wheelHit.sidewaysSlip}");
            }
        }

        private void UpdateWheelPose(Wheel wheel)
        {
            wheel.collider.GetWorldPose(out wheel.info.position, out wheel.info.rotation);
            wheel.model.position = wheel.info.position;
            wheel.model.rotation = wheel.info.rotation;
        }
    }
}