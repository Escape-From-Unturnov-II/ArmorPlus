using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeedMann.PvPRework.Models.Config
{
    public class MaskExtension : ItemClothingExtension
    {
        public new float Armor
        {
            get
            {
                return base.Armor;
            }
            set
            {
                base.Armor = value;
            }
        }
    }
}
