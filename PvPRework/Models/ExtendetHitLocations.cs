using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeedMann.PvPRework.Models
{
	public class ExtendetHitLocations
    {
		public static ExtendetHitLocation getExtendetHitlocation(ELimb eLimb)
		{
			return (ExtendetHitLocation)eLimb;
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
