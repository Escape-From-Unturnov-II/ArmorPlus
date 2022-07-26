using Rocket.Core.Logging;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Logger = Rocket.Core.Logging.Logger;

namespace SpeedMann.PvPRework.Models
{
	public class ExtendedHitLocations
    {
		public static ExtendetHitLocation getExtendetHitlocation(ELimb eLimb)
		{
			return (ExtendetHitLocation)eLimb;
		}
        public static ExtendetHitLocation getExtendetHitlocation(ELimb eLimb, Vector3 localPoint)
        {
            if(eLimb == ELimb.SKULL)
            {
                if (isEarHit(eLimb, localPoint)) return ExtendetHitLocation.EARS;
                if (isFaceHit(eLimb, localPoint)) return ExtendetHitLocation.FACE;
            }
            if(eLimb == ELimb.SPINE)
            {
                if (isStomachHit(eLimb, localPoint)) return ExtendetHitLocation.STOMACH;
            }
            return (ExtendetHitLocation)eLimb;
        }
        private static bool isEarHit(ELimb limb, Vector3 localPoint)
        {
            if (limb == ELimb.SKULL)
            {
                bool earsHit = localPoint.x > -0.5 && (localPoint.y >= 0.2 || localPoint.y <= -0.2) && localPoint.z > -0.05;
                if (earsHit && PvPRework.Conf.Debug)
                {
                    Logger.Log("Raycast hit in the Ears");
                }
                return earsHit;
            }
            return false;
        }
        private static bool isFaceHit(ELimb limb, Vector3 localPoint)
        {
            if (limb == ELimb.SKULL)
            {
                bool faceHit = localPoint.x > -0.55 && localPoint.z >= 0.2;
                if (faceHit && PvPRework.Conf.Debug)
                {
                    Logger.Log("Raycast hit in the Face");
                }
                return faceHit;
            }
            return false;
        }
        private static bool isStomachHit(ELimb limb, Vector3 localPoint)
        {
            if (limb == ELimb.SPINE)
            {
                bool stomachHit = localPoint.x > -0.23;
                if (stomachHit && PvPRework.Conf.Debug)
                {
                    Logger.Log("Raycast hit in the Stomach");
                }
                return stomachHit;
            }
            return false;
        }
    }
	public enum ExtendetHitLocation
	{
		LEFT_FOOT,
		LEFT_LEG,
		RIGHT_FOOT,
		RIGHT_LEG,
		LEFT_HAND,
		LEFT_ARM,
		RIGHT_HAND,
		RIGHT_ARM,
		LEFT_BACK,
		RIGHT_BACK,
		LEFT_FRONT,
		RIGHT_FRONT,
		SPINE,
		SKULL,
		STOMACH,
		FACE,
		EARS,
	}
}
