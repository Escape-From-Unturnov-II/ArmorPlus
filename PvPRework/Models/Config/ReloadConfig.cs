using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeedMann.PvPRework.Models.Config.ItemExtensions
{
    public class ReloadConfig
    {
        public bool Debug;
        public bool SwapMags = true;
        public List<InternalMagAmmoStack> InternalMagAmmoStacks = new List<InternalMagAmmoStack>();
    }
}
