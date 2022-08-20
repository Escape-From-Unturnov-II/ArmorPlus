using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SpeedMann.PvPRework.Models.Config
{
    public class MagazineExtension : ItemExtension
    {
        public float Penetration = -1;
        public float FleshDamage = -1;
        public float ArmorDamage = -1;
    }
}
