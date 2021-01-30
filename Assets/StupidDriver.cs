using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StupidDriver : MonoBehaviour
{
	WheelCollider _wheel;
	public Vector3 _wheelPosition;
	public Quaternion _wheelRotation;
	void Awake()
	{
		_wheel = GetComponent<WheelCollider>();
	}

	void FixedUpdate()
	{
		if (Input.GetAxis("Vertical") > 0)
		{
			_wheel.motorTorque = 50;
			_wheel.brakeTorque = 0;
		}
		else if (Input.GetAxis("Vertical") < 0)
		{
			_wheel.motorTorque = 0;
			_wheel.brakeTorque = 2000;
		}
		else
		{
			_wheel.motorTorque = 0;
			_wheel.brakeTorque = 0;
		}

		if (Input.GetAxis("Horizontal") < 0)
		{
			_wheel.steerAngle = -30;
		}
		else if (Input.GetAxis("Horizontal") > 0)
		{
			_wheel.steerAngle = 30;
		}
		else
		{
			_wheel.steerAngle = 0;
		}

		UpdateWheelPose();
	}

	private void UpdateWheelPose()
	{
		_wheel.GetWorldPose(out _wheelPosition, out _wheelRotation);
	}
}
