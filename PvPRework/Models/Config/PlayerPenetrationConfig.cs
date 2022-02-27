using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeedMann.PvPRework.Models.Config
{
    public class PlayerPenetrationConfig
    {
        public bool Enabled = true;
        public int MaxPenetrations = 2;
        public PenResistence Arm = new PenResistence
        {
            RequiredPenetration = 10,
            PenetrationForMinReduction = 40,
            MaxPenReduction = 0.3f,
            MinPenReduction = 0.1f,
        };
        public PenResistence Leg = new PenResistence
        {
            RequiredPenetration = 10,
            PenetrationForMinReduction = 40,
            MaxPenReduction = 0.3f,
            MinPenReduction = 0.1f,
        };
        public PenResistence Skull = new PenResistence
        {
            RequiredPenetration = 10,
            PenetrationForMinReduction = 40,
            MaxPenReduction = 0.3f,
            MinPenReduction = 0.1f,
        };
        public PenResistence Spine = new PenResistence
        {
            RequiredPenetration = 10,
            PenetrationForMinReduction = 40,
            MaxPenReduction = 0.3f,
            MinPenReduction = 0.1f,
        };
        public PenResistence Stomach = new PenResistence
        {
            RequiredPenetration = 10,
            PenetrationForMinReduction = 40,
            MaxPenReduction = 0.3f,
            MinPenReduction = 0.1f,
        };
    }
}
