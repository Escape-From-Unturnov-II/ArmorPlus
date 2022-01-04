using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeedMann.PvPRework.Models.Config
{
    public class HatExtension : ItemExtension
    {
        public bool ProtectFace = false; // if this hat should protect the face
        public float ArmorFace = 1; // vanilla armor rating from (0-1 where 1 is no armor)
        public  HatExtension()
        {

        }
    }
}
