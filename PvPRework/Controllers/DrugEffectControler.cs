using Rocket.Core.Logging;
using Rocket.Unturned.Player;
using SDG.Unturned;
using SpeedMann.PvPRework.Helper;
using SpeedMann.PvPRework.Models;
using SpeedMann.PvPRework.Models.Config;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Logger = Rocket.Core.Logging.Logger;

namespace SpeedMann.PvPRework.Controllers
{
    internal class DrugEffectControler
    {
        internal static HealthManagerConfig Conf { get; private set; }

        private static Dictionary<DrugEffectType, DrugEffectLimit> DrugStateDict;
        private static Dictionary<CSteamID, DrugEffectHandler> ConsumeableEffectHandlers = new Dictionary<CSteamID, DrugEffectHandler>();

        internal static void Init(HealthManagerConfig config)
        {
            Conf = config;
        }
        internal static void Cleanup() 
        {
            foreach(var handler in ConsumeableEffectHandlers)
            {
                handler.Value.stopAllMeds();
            }
            ConsumeableEffectHandlers.Clear();
        }
        internal static void OnPlayerConnected(UnturnedPlayer player)
        {
            ConsumeableEffectHandlers.Add(player.CSteamID, new DrugEffectHandler(player.Player, Conf.UseUI));
        }
        internal static void OnPrePlayerDisconnected(CSteamID playerId)
        {
            if(ConsumeableEffectHandlers.TryGetValue(playerId, out DrugEffectHandler handler)){
                StopAllDrugEffects(UnturnedPlayer.FromCSteamID(playerId));
            }
            ConsumeableEffectHandlers.Remove(playerId);
        }
        internal static void OnPlayerDeath(UnturnedPlayer player)
        {
            StopAllDrugEffects(player);
        }
        internal static void StopAllDrugEffects(UnturnedPlayer player)
        {
            if (!ConsumeableEffectHandlers.TryGetValue(player.CSteamID, out var drugEffectHandler) || drugEffectHandler == null)
            {
                Logger.LogError($"player {player.CSteamID} has no drug effect handler");
                return;
            }
            drugEffectHandler.stopAllMeds();
        }
        internal static void AddDrugEffects(UnturnedPlayer player, ushort itemId, List<MedicalEffectConfig> effectConfigs)
        {
            if (!ConsumeableEffectHandlers.TryGetValue(player.CSteamID, out var drugEffectHandler) || drugEffectHandler == null)
            {
                Logger.LogError($"player {player.CSteamID} has no drug effect handler");
                ConsumeableEffectHandlers.Remove(player.CSteamID);
                drugEffectHandler = new DrugEffectHandler(player.Player, Conf.UseUI);
                ConsumeableEffectHandlers.Add(player.CSteamID, drugEffectHandler);
            }

            drugEffectHandler.startDrugEffects(itemId, effectConfigs);
        }

        internal static bool tryGetEffectLimits(DrugEffectType type, out int min, out int max)
        {
            min = 0;
            max = 0;
            if (DrugStateDict.TryGetValue(type, out DrugEffectLimit limit))
            {
                min = limit.MinValue;
                max = limit.MaxValue;
                return true;
            }
            return false;
        }
        internal static Dictionary<DrugEffectType, DrugEffectLimit> createDrugEffectsLimitsDictionary(List<DrugEffectLimit> limits)
        {
            Dictionary<DrugEffectType, DrugEffectLimit> dict = new Dictionary<DrugEffectType, DrugEffectLimit>();
            foreach (DrugEffectLimit limit in limits)
            {
                if (dict.ContainsKey(limit.DrugEffectType))
                {
                    Logger.LogWarning("DrugEffectTypeLimit with Type:" + limit.DrugEffectType + " is a duplicate!");
                }
                else
                {
                    dict.Add(limit.DrugEffectType, limit);
                }
            }

            return dict;
        }
    }
}
