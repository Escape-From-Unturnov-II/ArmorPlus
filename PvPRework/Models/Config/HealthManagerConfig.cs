using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeedMann.PvPRework.Models.Config
{
    public class HealthManagerConfig
    {
        public bool EnableReuseableMeds = true;
        public List<MedicalExtension> BetterMeds = new List<MedicalExtension>();
    }
}
