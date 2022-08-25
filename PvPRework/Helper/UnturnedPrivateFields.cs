using Rocket.Core.Logging;
using SDG.NetTransport;
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
        private static FieldInfo GunAttachmentsField;
        private static FieldInfo ClothingArmorField;

        private static FieldInfo SkinColorField;

        private static FieldInfo SendSingleSkillLevelField;

        private static MethodInfo ReplicateStanceMethod;
        public static bool getGunAttachments(UseableGun gun, out Attachments result)
        {
            if (GunAttachmentsField != null)
            {
                result = (Attachments)GunAttachmentsField.GetValue(gun);
                return true;
            }
            result = null;
            return false;
        }
        public static bool setClothingArmor(ItemClothingAsset asset, float armor)
        {
            if (ClothingArmorField != null)
            {
                if(armor > 0)
                {
                    ClothingArmorField.SetValue(asset, armor);
                }
                
                return true;
            }
            return false;
        }
        public static bool setPalyerStance(PlayerStance playerStance)
        {
            if (ReplicateStanceMethod != null)
            {
                ReplicateStanceMethod.Invoke(playerStance, new object[] { true });

                return true;
            }
            return false;
        }

        public static bool trySetSkinColor(SteamPending playerLife, UnityEngine.Color newColor)
        {

            if (SkinColorField != null)
            {
                try
                {
                    SkinColorField.SetValue(playerLife, newColor);
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

        public static bool trySendSingleSkillLevel(PlayerSkills playerSkills, byte specialityIndex, byte skillIndex, byte newLevel)
        {
            if (SendSingleSkillLevelField != null)
            {
                try
                {
                    ClientInstanceMethod<byte, byte, byte> sender = SendSingleSkillLevelField.GetValue(playerSkills) as ClientInstanceMethod<byte, byte, byte>;
                    if (sender != null)
                    {
                        sender.InvokeAndLoopback(playerSkills.GetNetId(), ENetReliability.Reliable, Provider.EnumerateClients_Remote(), specialityIndex, skillIndex, newLevel);
                    }
                }
                catch (Exception e)
                {
                    Logger.LogException(e, "Exception sending SingleSkillLevel");
                    return false;
                }
            }
            return false;
        }

        public static void Init()
        {
            Type type;

            type = typeof(SteamPending);
            SkinColorField = type.GetField("_skin", BindingFlags.NonPublic | BindingFlags.Instance);

            type = typeof(UseableGun);
            GunAttachmentsField = type.GetField("thirdAttachments", BindingFlags.NonPublic | BindingFlags.Instance);

            type = typeof(ItemClothingAsset);
            ClothingArmorField = type.GetField("_armor", BindingFlags.NonPublic | BindingFlags.Instance);

            type = typeof(PlayerStance);
            ReplicateStanceMethod = type.GetMethod("replicateStance", BindingFlags.NonPublic | BindingFlags.Instance);

            type = typeof(PlayerSkills);
            SendSingleSkillLevelField = type.GetField("SendSingleSkillLevel", BindingFlags.NonPublic | BindingFlags.Static);
        }
    }
}
