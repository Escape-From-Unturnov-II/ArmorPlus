using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SpeedMann.PvPRework.Models.Config
{
    public class MedicalEffectConfig
    {
        [XmlAttribute("Type")]
        public DrugEffectType Type;
        public uint StartDelay;
        public uint Duration;
        public int Value;
        public float Interval;

    }
}
