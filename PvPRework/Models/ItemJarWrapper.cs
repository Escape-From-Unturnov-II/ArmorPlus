using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeedMann.PvPRework.Models
{
    public class ItemJarWrapper
    {
        public ItemJar itemJar;
        public byte index;
        public byte page;

        public ItemJarWrapper()
        {

        }
        public ItemJarWrapper(ItemJar itemJar, byte page, byte index = 0)
        {
            this.itemJar = itemJar;
            this.page = page;
            this.index = index;
        }
    }
}
