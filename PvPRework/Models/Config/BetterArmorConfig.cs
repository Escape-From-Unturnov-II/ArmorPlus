using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeedMann.PvPRework.Models.Config
{
    public class BetterArmorConfig
    {
        public bool Enabled = true; //if better armor calculations should be used (required for ArmorClasses and Hat-/Vest- and GunExtensions to take effect)
        public bool UseArmorClasses = true; //defines if armor classes should be used
        public float ArmorDamageMultiplierOnPen = 0.5f; //multiplier used for damage done to armor when penetrating armor
        public float PenDamgeDelta = 0.7f; //used to reduce pendamge loss on penetration chance
                                           //(1-0 where 0 would equal to no reduction on any penchance and 1 would be 50% penetration chance = 50% pendamage loss)
        public short HatEffectKey = 5210;
        public short GlassesEffectKey = 5211;

        public BetterHitZonesConfig BetterHitZones = new BetterHitZonesConfig();

        public BetterArmorConfig()
        {
        }
    }
}
