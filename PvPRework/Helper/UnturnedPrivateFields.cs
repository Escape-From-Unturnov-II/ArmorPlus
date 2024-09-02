using Rocket.Core.Logging;
using SDG.NetTransport;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Logger = Rocket.Core.Logging.Logger;

namespace SpeedMann.PvPRework.Helper
{
    class UnturnedPrivateFields
    {
        private static FieldInfo GunAttachmentsField;
        private static FieldInfo ClothingArmorField;

        private static FieldInfo SkinColorField;

        private static FieldInfo SendSingleSkillLevelField;
        private static FieldInfo SendDamagedEventField;
        private static FieldInfo SendWearGlassesField;

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
        public static bool trySetSkinColor(SteamPending playerLife, Color newColor)
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
                        return true;
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
        public static bool trySendDamagedEvent(PlayerLife playerLife, byte flinchAmount, Vector3 direction)
        {
            if (SendDamagedEventField == null || playerLife?.player?.channel == null)
                return false;

            try
            {
                ClientInstanceMethod<byte, Vector3> sender = SendDamagedEventField.GetValue(playerLife) as ClientInstanceMethod<byte, Vector3>;
                if (sender == null)
                    return false;

                sender.Invoke(playerLife.GetNetId(), ENetReliability.Reliable, playerLife.player.channel.GetOwnerTransportConnection(), flinchAmount, direction);
            }
            catch (Exception e)
            {
                Logger.LogException(e, "Exception sending DamagedEvent");
                return false;
            }
            return true;

        }
        public static bool trySendWearGlasses(PlayerClothing playerClothing, ItemGlassesAsset asset, byte quality, byte[] state, bool playEffect, List<ITransportConnection> transportConnections)
        {
            if (SendWearGlassesField == null || playerClothing?.channel == null)
            {
                return false;
            }
            try
            {
                ClientInstanceMethod<Guid, byte, byte[], bool> sender = SendWearGlassesField.GetValue(null) as ClientInstanceMethod<Guid, byte, byte[], bool>;
                if (sender == null)
                {
                    return false;
                }
                sender.Invoke(playerClothing.GetNetId(), ENetReliability.Reliable, transportConnections, asset?.GUID ?? Guid.Empty, quality, state, playEffect);
            }
            catch (Exception e)
            {
                Logger.LogException(e, "Exception sending WearGlasses");
                return false;
            }
            return true;

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

            type = typeof(PlayerLife);
            SendDamagedEventField = type.GetField("SendDamagedEvent", BindingFlags.NonPublic | BindingFlags.Static);

            type = typeof(PlayerClothing);
            SendWearGlassesField = type.GetField("SendWearGlasses", BindingFlags.NonPublic | BindingFlags.Static);
            if (SendWearGlassesField == null)
            {
                Logger.LogError($"Could not get {nameof(SendWearGlassesField)}");
            }
        }
    }
}
