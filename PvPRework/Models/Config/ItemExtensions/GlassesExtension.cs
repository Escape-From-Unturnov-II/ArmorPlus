using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SpeedMann.PvPRework.Models.Config
{
    public class GlassesExtension : ItemUIExtension
    {
        [XmlIgnore]
        public new float Armor = -1;
    }
}
