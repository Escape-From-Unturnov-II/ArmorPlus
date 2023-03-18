using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeedMann.PvPRework.Models.Config.ItemExtensions
{
    public class ItemReplaceInfo : ItemExtension
    {
        public ReplaceType DurabilityReplacementType = ReplaceType.Keep;
        public ReplaceType AmmountReplacementType = ReplaceType.Keep;
        public List<ItemExtension> ReplaceTargets = new List<ItemExtension>();
    }

    public enum ReplaceType
    {
        Empty,
        Keep,
        Full
    }
}
