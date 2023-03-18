using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SpeedMann.PvPRework.Models.Config.ItemExtensions
{
    public class InternalMagAmmoStack : ItemExtension
    {
        public List<InternalMagGun> CompatibleGuns = new List<InternalMagGun>();
    }
}
