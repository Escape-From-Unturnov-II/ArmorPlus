using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeedMann.PvPRework.Models.Config.ItemExtensions
{
    public class InternalMagazine : ItemExtension
    {
        public List<ItemExtension>  CompatibleGuns;
        public InternalMagazine()
        {

        }
        public InternalMagazine(ushort id, string name = "") : base(id, name) { }
    }
}
