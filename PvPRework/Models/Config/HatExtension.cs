using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SpeedMann.PvPRework.Models.Config
{
    public class HatExtension : ItemUIExtension
    {
        public bool ProtectFace = false; // if this hat should protect the face
        public float ArmorFace = 1; // vanilla armor rating from (0-1 where 1 is no armor)
        public bool ProtectEars = false;
        public float ArmorEars = 1; // vanilla armor rating from (0-1 where 1 is no armor)

        /*
        public bool ProtetctCheaks = false;
        public float ArmorCheaks = 1; // vanilla armor rating from (0-1 where 1 is no armor)
        public bool ProtectEyes = false;
        public float ArmorEyes = 1; // vanilla armor rating from (0-1 where 1 is no armor)
        */
        public  HatExtension()
        {

        }
        /*
        public bool isProtected(ELimb limb, Vector3 hitPoint)
        {
            if(PvPRework.isFaceHit(limb, hitPoint) && ProtectFace){

            }
            return false;
        }
        */
    }
}
