using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeedMann.PvPRework.Models.Config
{
    public class HealthManagerConfig
    {
        public bool Debug = true;
        public bool UseUI = false;

        public int FractureRunningDamage = 2;
        public float FractureRunningDamageInterval = 1;
        public byte FractureRunningFlinch = 5;  // valid values 0 - 25
        public List<MedicalExtension> BetterMeds = new List<MedicalExtension>();
    }
}
