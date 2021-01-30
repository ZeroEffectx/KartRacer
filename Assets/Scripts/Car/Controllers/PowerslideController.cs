using System;
using UnityEngine;

namespace KartRacer.Car
{
    public class PowerslideController : MonoBehaviour
    {
        [System.Serializable]
        public struct PowerslidePhysicsSettings
        {
            public FrictionSettings frontDefaultForwardFriction;
            public FrictionSettings frontDefaultSidewaysFriction;
            public FrictionSettings rearDefaultForwardFriction;
            public FrictionSettings rearDefaultSidewaysFriction;
            public FrictionSettings frontPowerslideForwardFriction;
            public FrictionSettings frontPowerslideSidewaysFriction;
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

        public enum WheelState
        {
            normalDrive = 0,
            powerslide = 1
        }

        public enum FrictionType
        {
            forward = 0,
            sideways = 1
        }
        
        public PowerslidePhysicsSettings _physicsSettings;
        public float _powerslideJumpForce;
        private WheelSettings _wheelSettings;
        private Rigidbody _carBody;
        private bool _powerslidePressed;
        private bool _powerslideReleased;

        // Start is called before the first frame update
        public void Start()
        {
            _powerslidePressed = false;
            _powerslideReleased = false;
            _wheelSettings = this.GetComponent<WheelSettings>();
            _carBody = this.GetComponent<Rigidbody>();
            foreach(Wheel wheel in _wheelSettings.wheels)
            {
                ApplyDefaultPhysicsSettings(wheel);
            }
        }

        // Update is called once per frame
        public void Update()
        {
            _powerslidePressed = Input.GetKeyDown(KeyCode.A) || Input.GetButtonDown("Powerslide");
            _powerslideReleased = Input.GetKeyUp(KeyCode.A) || Input.GetButtonUp("Powerslide");
        }

        public void FixedUpdate()
        {
            foreach(Wheel wheel in _wheelSettings.wheels)
            {
                if (_powerslidePressed)
                {
                    Debug.Log("Powerslide pressed!");
                    ApplyPowerslidePhysicsSettings(wheel);
                }
                if (_powerslideReleased)
                {
                    Debug.Log("Powerslide released!");
                    ApplyDefaultPhysicsSettings(wheel);
                }
            }
        }

        private void ApplyDefaultPhysicsSettings(Wheel wheel)
        {
            var defaultForwardFrictionSettings = GetFrictionSettingsForState(wheel, WheelState.normalDrive,
                FrictionType.forward);
            var defaultSidewaysFrictionSettings = GetFrictionSettingsForState(wheel, WheelState.normalDrive,
                FrictionType.sideways);
            
            wheel.collider.forwardFriction = GetFrictionCurveFromSettings(defaultForwardFrictionSettings);
            wheel.collider.sidewaysFriction = GetFrictionCurveFromSettings(defaultSidewaysFrictionSettings);
        }

        private void ApplyPowerslidePhysicsSettings(Wheel wheel)
        {
            var powerslideForwardFrictionSettings = GetFrictionSettingsForState(wheel, WheelState.powerslide,
                FrictionType.forward);
            var powerslideSidewaysFrictionSettings = GetFrictionSettingsForState(wheel, WheelState.powerslide,
                FrictionType.sideways);
            
            wheel.collider.forwardFriction = GetFrictionCurveFromSettings(powerslideForwardFrictionSettings);
            wheel.collider.sidewaysFriction = GetFrictionCurveFromSettings(powerslideSidewaysFrictionSettings);
            
            _carBody.AddForce(_carBody.transform.up * _powerslideJumpForce, ForceMode.Impulse);
            //_rigidbody.transform.rotation = new Quaternion(0, 30 * _turnAmount >= 0 ? 1 : -1, 0, 0);
        }

        private FrictionSettings GetFrictionSettingsForState(Wheel wheel, WheelState state, 
            FrictionType frictionType)
        {
            if(wheel.info.isFrontWheel)
            {
                switch(state)
                {
                    case WheelState.powerslide:
                        return frictionType == FrictionType.forward ?
                            _physicsSettings.frontPowerslideForwardFriction : 
                            _physicsSettings.frontPowerslideSidewaysFriction;
                    default:
                        return frictionType == FrictionType.forward ?
                            _physicsSettings.frontDefaultForwardFriction :
                            _physicsSettings.frontDefaultSidewaysFriction;
                }
            }
            else
            {
                switch(state)
                {
                    case WheelState.powerslide:
                        return frictionType == FrictionType.forward ?
                            _physicsSettings.rearPowerslideForwardFriction : 
                            _physicsSettings.rearPowerslideSidewaysFriction;
                    default:
                        return frictionType == FrictionType.forward ?
                            _physicsSettings.rearDefaultForwardFriction :
                            _physicsSettings.rearDefaultSidewaysFriction;
                }
            }

            throw new ArgumentOutOfRangeException($@"No friction settings found for wheel: {wheel.info.isFrontWheel},
                state: {state}, frictionType: {frictionType}");
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
    }
}