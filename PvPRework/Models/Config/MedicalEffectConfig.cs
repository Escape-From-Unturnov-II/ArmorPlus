using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeedMann.PvPRework.Models.Config
{
    public class MedicalEffectConfig
    {
        public uint EffectStartDelay = 0;
        public uint EffectDuration = 0;
        public int Value;
        public float Interval = 1;
        public DrugEffectType Type;
    }
}
