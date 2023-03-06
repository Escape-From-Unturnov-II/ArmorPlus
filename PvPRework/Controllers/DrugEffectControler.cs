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

namespace SpeedMann.PvPRework.Controllers
{
    internal class DrugEffectControler
    {
        internal static HealthManagerConfig Conf { get; private set; }

        private static Dictionary<DrugEffectType, DrugEffectLimit> DrugStateDict;
        private static Dictionary<CSteamID, DrugEffectHandler> PlayerDrugSates = new Dictionary<CSteamID, DrugEffectHandler>();

        internal static void Init(HealthManagerConfig config)
        {
            Conf = config;
        }

        internal static void OnPlayerConnected(UnturnedPlayer player)
        {
            PlayerDrugSates.Add(player.CSteamID, new DrugEffectHandler(player.Player));
        }
        internal static void OnPrePlayerDisconnected(CSteamID playerId)
        {
            if(PlayerDrugSates.TryGetValue(playerId, out DrugEffectHandler handler)){
                handler.stopAllMeds();
            }
            PlayerDrugSates.Remove(playerId);
        }
        
        internal static void AddItemEffects(UnturnedPlayer player, ushort itemId, List<MedicalEffectConfig> effectConfigs)
        {
            if (!PlayerDrugSates.TryGetValue(player.CSteamID, out var drugEffectHandler) || drugEffectHandler == null)
            {
                Logger.LogError($"player {player.CSteamID} has no Drug effect handler");
                PlayerDrugSates.Remove(player.CSteamID);
                PlayerDrugSates.Add(player.CSteamID, new DrugEffectHandler(player.Player));
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
