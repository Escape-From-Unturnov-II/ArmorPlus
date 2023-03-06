using Rocket.Unturned.Events;
using Rocket.Unturned.Player;
using SDG.Unturned;
using SpeedMann.PvPRework.Models.Config;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static SpeedMann.PvPRework.Models.SkillTypes;
using Logger = Rocket.Core.Logging.Logger;

namespace SpeedMann.PvPRework.Models.MedicalEffects
{
    internal class PreventiveEffect : MedicalEffect
    {
        DrugEffectType type;
        CSteamID steamID;
        internal PreventiveEffect(Player player, float effectDuration, float effectDelay, DrugEffectType type) : base(player, effectDuration, effectDelay)
        {
            steamID = player.channel.owner.playerID.steamID;
            this.type = type;

        }
        protected override void startInner()
        {
            switch (type)
            {
                case DrugEffectType.NoBleeding:
                    UnturnedPlayerEvents.OnPlayerUpdateBleeding += stopBleeding;
                    break;
                case DrugEffectType.NoFracture:
                    UnturnedPlayerEvents.OnPlayerUpdateBroken += healFracture;
                    break;
                default:
                    Logger.LogError($"Preventive effect {type} is not supported!");
                    break;
            }
            
        }

        protected override void stopInner()
        {
            switch (type)
            {
                case DrugEffectType.NoBleeding:
                    UnturnedPlayerEvents.OnPlayerUpdateBleeding -= stopBleeding;
                    break;
                case DrugEffectType.NoFracture:
                    UnturnedPlayerEvents.OnPlayerUpdateBroken -= healFracture;
                    break;
                default:
                    Logger.LogError($"Preventive effect {type} is not supported!");
                    break;
            }
        }

        private void stopBleeding(UnturnedPlayer player, bool isBleeding)
        {
            if (isBleeding && player.CSteamID.Equals(steamID))
            {
                player.Player.life.serverSetBleeding(false);
            }
        }
        private void healFracture(UnturnedPlayer player, bool hasFracture)
        {
            if (hasFracture && player.CSteamID.Equals(steamID))
            {
                player.Player.life.serverSetLegsBroken(false);
            }
        }
    }
}
