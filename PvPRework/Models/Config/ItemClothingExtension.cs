using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SpeedMann.PvPRework.Models.Config
{
    public class ItemClothingExtension : ItemExtension
    {
        [XmlIgnore]
        public float Armor = -1; // vanilla armor -1 is no override
    }
}
