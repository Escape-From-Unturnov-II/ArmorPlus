using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PvPRework
{
    public class BulletLimbDamageChance
    {
        public string Limb { get; set; }
        public int BreakChanceMin { get; set; }
        public int BreakChanceMax { get; set; }
        public int BreakChanceDamageMin { get; set; }
        public int BreakChanceDamageMax { get; set; }

public BulletLimbDamageChance() { }
    }
}
