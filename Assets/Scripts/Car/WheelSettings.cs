using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KartRacer.Car
{
    public class WheelSettings : MonoBehaviour
    {
        public List<Wheel> wheels;

        // Start is called before the first frame update
        public void Start()
        {
            if(wheels == null || wheels.Count < 2)
            {
                throw new ArgumentNullException("Wheel settings does not have enough wheels.");
            }        
        }

        public Wheel GetOppositeWheelOnAxle(Wheel wheel)
        {
            return wheels.Where(opp => 
                opp.info.isFrontWheel == wheel.info.isFrontWheel && 
                opp.info.isRightWheel != wheel.info.isRightWheel).FirstOrDefault();
        }
    }
}