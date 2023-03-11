using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeedMann.PvPRework.Models
{
    public class PainkillerConfig
    {
        public int FractureRunningDamage = 1;
        public float FractureRunningDamageInterval = 1;
        public float FractureLandingMaxVelocity = 5; // only positive values
        public float FractureLandingVelocitySteps = 7;
        public float FractureLandingBaseDamage = 2;
        public byte FractureDamageFlinch = 4;  // valid values 0 - 25
    }
}
