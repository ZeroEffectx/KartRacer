using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StupidDriver : MonoBehaviour
{
	WheelCollider _wheel;
	void Awake()
	{
		_wheel = GetComponent<WheelCollider>();
	}

	void FixedUpdate()
	{
		if (Input.GetAxis("Vertical") > 0)
		{
			_wheel.motorTorque = 400;
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
			_wheel.steerAngle = -3;
		}
		else if (Input.GetAxis("Horizontal") > 0)
		{
			_wheel.steerAngle = 3;
		}
		else
		{
			_wheel.steerAngle = 0;
		}
	}
}
