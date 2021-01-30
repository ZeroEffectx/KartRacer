using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KartRacer.Car
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
}


