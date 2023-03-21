using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SpeedMann.PvPRework.Models.Config.ItemExtensions
{
    public class ItemReplaceInfo : ItemExtension
    {
        public ReplaceType AmmountReplacementType = ReplaceType.Keep;
        public ReplaceType DurabilityReplacementType = ReplaceType.Keep;
        public List<ItemExtension> ReplaceTargets = new List<ItemExtension>();

        public ItemReplaceInfo()
        {

        }
        public ItemReplaceInfo(ushort id, string name = "") : base(id, name) { }
        public ItemReplaceInfo(ushort id, ReplaceType amountReplace, ReplaceType durabilityReplace)
        {
            Id = id;
            AmmountReplacementType = amountReplace;
            DurabilityReplacementType = durabilityReplace;
        }
    }

    public enum ReplaceType
    {
        Empty,
        Keep,
        Full
    }
}
