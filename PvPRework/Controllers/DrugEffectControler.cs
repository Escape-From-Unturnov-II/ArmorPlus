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
        private static Dictionary<CSteamID, PlayerLifeHandler> PlayerDrugSates = new Dictionary<CSteamID, PlayerLifeHandler>();

        internal static void Init(HealthManagerConfig config)
        {
            Conf = config;

            DrugStateDict = createDrugEffectsLimitsDictionary(Conf.DrugEffectsLimits);
        }

        internal static void updateEffects(UnturnedPlayer player, List<MedicalEffect> effects)
        {
            if (!PlayerDrugSates.TryGetValue(player.CSteamID, out PlayerLifeHandler handler))
            {
                Logger.LogError("Could not modify effect of untracked player!");
                return;
            }
            handler.startDrugEffects(effects);
        }

        internal static void OnPlayerConnected(UnturnedPlayer player)
        {
            PlayerLife life = player.Player.life;
            PlayerDrugSates.Add(player.CSteamID, new PlayerLifeHandler(player.Player, life.stamina, life.food, life.health, life.water, life.virus));
        }
        internal static void OnPrePlayerDisconnected(CSteamID playerId)
        {
            if(PlayerDrugSates.TryGetValue(playerId, out PlayerLifeHandler handler)){
                handler.removeAllEffects();
            }
            PlayerDrugSates.Remove(playerId);
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
