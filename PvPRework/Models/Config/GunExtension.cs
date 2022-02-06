using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeedMann.PvPRework.Models.Config
{
    public class GunExtension : MagazineOverride
    {
        public float PenetrationMultiplier= 1;
        public float FleshDamageMultiplier = 1;
        public float ArmorDamageMultiplier = 1;
        public List<MagazineOverride> MagazineOverrides = new List<MagazineOverride>();
        public GunExtension()
        {

        }
    }
}
