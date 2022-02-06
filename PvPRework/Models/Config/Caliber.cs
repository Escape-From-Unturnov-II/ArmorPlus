using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SpeedMann.PvPRework.Models.Config
{
    public class Caliber
    {
        public string Name = "";
        public float Penetration = 0;
        public float FleshDamage = 0;
        public float ArmorDamage = 0;

        [XmlArrayItem(ElementName = "MagCaliberId")]
        public List<ushort> MagazineCalibers = new List<ushort>();
    }
}
