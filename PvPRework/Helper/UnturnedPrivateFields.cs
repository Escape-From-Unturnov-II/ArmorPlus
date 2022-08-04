using Rocket.Core.Logging;
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
        private static FieldInfo GunAttachments;
        private static FieldInfo ClothingArmor;

        private static FieldInfo SkinColor;

        private static MethodInfo ReplicateStance;
        public static bool getGunAttachments(UseableGun gun, out Attachments result)
        {
            if (GunAttachments != null)
            {
                result = (Attachments)GunAttachments.GetValue(gun);
                return true;
            }
            result = null;
            return false;
        }
        public static bool setClothingArmor(ItemClothingAsset asset, float armor)
        {
            if (ClothingArmor != null)
            {
                if(armor > 0)
                {
                    ClothingArmor.SetValue(asset, armor);
                }
                
                return true;
            }
            return false;
        }
        public static bool setPalyerStance(PlayerStance playerStance)
        {
            if (ReplicateStance != null)
            {
                ReplicateStance.Invoke(playerStance, new object[] { true });

                return true;
            }
            return false;
        }

        public static bool trySetSkinColor(SteamPending playerLife, UnityEngine.Color newColor)
        {

            if (SkinColor != null)
            {
                try
                {
                    SkinColor.SetValue(playerLife, newColor);
                }
                catch (Exception e)
                {
                    Logger.LogException(e, "Exception setting private field skinColor");
                    return false;
                }
                return true;
            }
            return false;
        }

        public static void Init()
        {
            Type type;

            type = typeof(SteamPending);
            SkinColor = type.GetField("_skin", BindingFlags.NonPublic | BindingFlags.Instance);

            type = typeof(UseableGun);
            GunAttachments = type.GetField("thirdAttachments", BindingFlags.NonPublic | BindingFlags.Instance);

            type = typeof(ItemClothingAsset);
            ClothingArmor = type.GetField("_armor", BindingFlags.NonPublic | BindingFlags.Instance);

            type = typeof(PlayerStance);
            ReplicateStance = type.GetMethod("replicateStance", BindingFlags.NonPublic | BindingFlags.Instance);
        }
    }
}
