using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SpeedMann.PvPRework.Models.Config
{
    public class ItemExtension
    {
        [XmlAttribute("Id")]
        public ushort Id;
        [XmlAttribute("Name")]
        public string Name;
        public ItemExtension()
        {

        }
        public ItemExtension(ushort id, string name = "")
        {
            Id = id;
            Name = name != "" ? name : "Undefined";
        }

        public bool Equals(ItemExtension other)
        {
            return other.Id == this.Id;
        }
    }
}
