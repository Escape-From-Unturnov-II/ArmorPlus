using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SpeedMann.PvPRework.Models.Config
{
    public class VestExtension : ItemExtension
    {
        public bool ProtectStomach = true; // if this vest should protect the stomach
        public float ShoulderPlateLength = 0; // 0 - 0.9 (0 is disabled, 0.23 is only shoulder , 0.4 is upper arm, 0.9 is full arm)
        public float ArmorShoulderPlate = 1; // vanilla armor rating for shoulders / arms from (0-1 where 1 is no armor)
        public float ThighPlateLength = 0;  // 0 - 0.9 (0 is disabled, 0.3 is full thigh, 0.9 is full leg)
        public float ArmorThighPlate = 1; // vanilla armor rating for thighs / legs from (0-1 where 1 is no armor)

        public VestExtension()
        {

        }
        public bool isProtected(ELimb limb, Vector3 hitPoint)
        {
            switch (limb)
            {
                case ELimb.LEFT_ARM:
                case ELimb.RIGHT_ARM:
                    return hitPoint.x > -ShoulderPlateLength;
                case ELimb.LEFT_FOOT:
                case ELimb.RIGHT_FOOT:
                    return hitPoint.x > -ThighPlateLength;
                case ELimb.SPINE:
                    return ProtectStomach;
            }
            return false;   
        }
    }
}
