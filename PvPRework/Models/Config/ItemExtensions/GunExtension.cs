using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SpeedMann.PvPRework.Models.Config
{
    public class GunExtension : MagazineExtension
    {
        public float PenetrationMultiplier= 1;
        public float FleshDamageMultiplier = 1;
        public float ArmorDamageMultiplier = 1;
        public byte InternalMagazineSize = 0;
        [XmlArrayItem(ElementName = "MagazineOverride")]
        public List<MagazineExtension> MagazineOverrides = new List<MagazineExtension>();
        public GunExtension()
        {

        }
    }
}
