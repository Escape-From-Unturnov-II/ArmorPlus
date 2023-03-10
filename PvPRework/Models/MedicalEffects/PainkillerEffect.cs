using Rocket.Core.Steam;
using Rocket.Unturned.Events;
using Rocket.Unturned.Player;
using SDG.Unturned;
using SpeedMann.PvPRework.Controllers;
using SpeedMann.PvPRework.Models.Config;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeedMann.PvPRework.Models.MedicalEffects
{
    internal class PainkillerEffect : MedicalEffect
    {
        private PlayerLife life;
        private bool legsBroken;
        private CSteamID steamID;
        internal PainkillerEffect(Player player, float effectDuration, float effectDelay) : base(player, effectDuration, effectDelay)
        {
            life = player.life;
            steamID = player.channel.owner.playerID.steamID;
        }
        protected override void startInner()
        {
            legsBroken = life.isBroken;
            life.serverSetLegsBroken(false);
            HealthManager.OnTriedHealingFracture += triedHealingFracture;
            UnturnedPlayerEvents.OnPlayerUpdateBroken += healFracture;
        }

        protected override void stopInner()
        {
            HealthManager.OnTriedHealingFracture -= triedHealingFracture;
            UnturnedPlayerEvents.OnPlayerUpdateBroken -= healFracture;
            life.serverSetLegsBroken(legsBroken);
        }
        private void healFracture(UnturnedPlayer player, bool hasFracture)
        {
            if (hasFracture && player.CSteamID.Equals(steamID))
            {
                legsBroken = true;
                life.serverSetLegsBroken(false);
            }
        }
        private void triedHealingFracture()
        {
            legsBroken = false;
        }
    }
}
