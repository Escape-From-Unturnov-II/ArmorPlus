using Rocket.Core.Logging;
using Rocket.Unturned.Player;
using SDG.NetTransport;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using Logger = Rocket.Core.Logging.Logger;
using UnityEngine;
using SpeedMann.PvPRework.Models.Config;

namespace SpeedMann.PvPRework
{


    public class EffectController
    {
        class UI_Status
        {
            internal short Key = 0;
            internal ushort EffectId = 0;
        }

        static Dictionary<ulong, List<UI_Status>> PlayerUIStatus;

        public static void checkClothingEffect<T>(Dictionary<ushort, T> clothingExtensions, UnturnedPlayer player, ushort clothingId, bool spawned = false) where T : ItemUIExtension
        {
            if(player == null)
            {
                Logger.LogError("Clothing effect check for player null");
                return;
            }

            PVPReworkConfiguration conf = PvPRework.Conf;
            T clothingExtension;
            ushort equipedClothingId;
            short effectKey;
            ushort uneqipId = 0;
            ushort equipId = 0;

            if (typeof(T).Equals(typeof(GlassesExtension)))
            {
                effectKey = conf.BetterArmor.GlassesEffectKey;
                equipedClothingId = player.Player.clothing.glasses;
            }
            else if (typeof(T).Equals(typeof(HatExtension)))
            {
                effectKey = conf.BetterArmor.HatEffectKey;
                equipedClothingId = player.Player.clothing.hat;
            }
            else
            {
                Logger.LogError("Clothing effect check for unimplemented clothing type");
                return;
            }

            if (!spawned && clothingExtensions.TryGetValue(equipedClothingId, out clothingExtension) && clothingExtension.EquipEffectId > 0)
            {
                spawnUI(clothingExtension.UnequipEffectId, effectKey, player.CSteamID);
                if (conf.Debug)
                    Logger.Log("Clothing UI enabled: " + equipId);
            }
            if (clothingExtensions.TryGetValue(clothingId, out clothingExtension) && clothingExtension.EquipEffectId > 0)
            {
                spawnUI(clothingExtension.EquipEffectId, effectKey, player.CSteamID);
                if (conf.Debug)
                    Logger.Log("Clothing UI disabled with: " + uneqipId);
            }
        }
        public static void spawnUI(ushort effectId, short effectKey)
        {
            EffectManager.sendUIEffect(effectId, effectKey, true);
        }
        public static void spawnUI(ushort effectId, short effectKey, CSteamID executorID)
        {
            ITransportConnection transportConnection = Provider.findTransportConnection(executorID);
            if (transportConnection == null)
            {
                Logger.LogError("Error in Event UI while trying to show UI (CSteamID not found)");
                return;
            }
            EffectManager.sendUIEffect(effectId, effectKey, transportConnection, true);
        }
        public static void setVisibility(bool visible, short effectKey, string panelName)
        {
            foreach (SteamPlayer player in Provider.clients)
            {
                UnturnedPlayer uPlayer = UnturnedPlayer.FromSteamPlayer(player);
                setVisibility(visible, effectKey, panelName, uPlayer.CSteamID);
            }
        }
        public static void setVisibility(bool visible, short effectKey, string panelName, CSteamID executorID)
        {
            ITransportConnection transportConnection = Provider.findTransportConnection(executorID);
            if (transportConnection == null)
            {
                Logger.LogError("Error in Event UI while trying to hide UI (CSteamID not found)");
                return;
            }
            EffectManager.sendUIEffectVisibility(effectKey, transportConnection, false, panelName, visible);
        }

        private static UI_Status getUIStatus(ulong steamId, short key)
        {
            if(PlayerUIStatus == null)
            {
                PlayerUIStatus = new Dictionary<ulong, List<UI_Status>>();
            }
            List<UI_Status> UI_Statuses;
            if (!PlayerUIStatus.TryGetValue(steamId, out UI_Statuses))
            {
                UI_Statuses = new List<UI_Status>();
                PlayerUIStatus.Add(steamId, UI_Statuses);
            }
            UI_Status status = UI_Statuses.Find( x => x.Key == key);
            if (status == null)
            {
                status = new UI_Status { Key = key, EffectId = 0 };
                UI_Statuses.Add(status);
            }
            return status;
        }
    }
}
