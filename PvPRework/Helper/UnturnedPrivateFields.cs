using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SpeedMann.PvPRework.Helper
{
    class UnturnedPrivateFields
    {
        private static FieldInfo Gun_Attachments;
        private static FieldInfo Clothing_Armor;
        public static bool getGunAttachments(UseableGun gun, out Attachments result)
        {
            if (Gun_Attachments != null)
            {
                result = (Attachments)Gun_Attachments.GetValue(gun);
                return true;
            }
            result = null;
            return false;
        }
        public static bool setClothingArmor(ItemClothingAsset asset, float armor)
        {
            if (Clothing_Armor != null)
            {
                if(armor > 0)
                {
                    Clothing_Armor.SetValue(asset, armor);
                }
                
                return true;
            }
            return false;
        }


        public static void Init()
        {
            Type type;

            type = typeof(UseableGun);
            Gun_Attachments = type.GetField("thirdAttachments", BindingFlags.NonPublic | BindingFlags.Instance);

            type = typeof(ItemClothingAsset);
            Clothing_Armor = type.GetField("_armor", BindingFlags.NonPublic | BindingFlags.Instance);
        }
    }
}
