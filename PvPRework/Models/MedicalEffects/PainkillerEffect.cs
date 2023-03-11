using Rocket.Core.Steam;
using Rocket.Unturned.Events;
using Rocket.Unturned.Player;
using SDG.NetTransport;
using SDG.Unturned;
using SpeedMann.PvPRework.Controllers;
using SpeedMann.PvPRework.Helper;
using SpeedMann.PvPRework.Models.Config;
using Steamworks;
using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Logger = Rocket.Core.Logging.Logger;

namespace SpeedMann.PvPRework.Models.MedicalEffects
{
    internal class PainkillerEffect : MedicalEffect
    {
        private CSteamID steamID;
        private int damage;
        private float interval;
        private byte flinchAmount;

        private bool legsBroken = false;
        private bool isSprinting = false;
        private bool gotDamaged = false;
        private bool active = false;
        internal PainkillerEffect(Player player, float effectDuration, float effectDelay, int change, float interval, byte flinchAmount) : base(player, effectDuration, effectDelay)
        {
            steamID = player.channel.owner.playerID.steamID;
            damage = change;
            this.flinchAmount = flinchAmount;
            this.interval = interval > 0 ? interval : 1;
        }
        protected override void startInner()
        {
            active = true;
            isSprinting = player.stance.stance == EPlayerStance.SPRINT;
            legsBroken = player.life.isBroken;
            player.life.serverSetLegsBroken(false);

            HealthManager.OnTriedHealingFracture += triedHealingFracture;
            UnturnedPlayerEvents.OnPlayerUpdateBroken += fractureChanged;
            StanceHandler.OnPostStanceChange += stanceChanged;
            
            player.StartCoroutine(sprintDamage());
        }

        protected override void stopInner()
        {
            HealthManager.OnTriedHealingFracture -= triedHealingFracture;
            UnturnedPlayerEvents.OnPlayerUpdateBroken -= fractureChanged;
            StanceHandler.OnPostStanceChange -= stanceChanged;

            player.life.serverSetLegsBroken(legsBroken);
            isSprinting = false;
            active = false;
        }
        private void stanceChanged(EPlayerStance newStance)
        {

            isSprinting = newStance == EPlayerStance.SPRINT;
            checkDamagePlayer();
        }
        private void fractureChanged(UnturnedPlayer player, bool wasFractured)
        {
            if (wasFractured && player.CSteamID.Equals(steamID))
            {
                legsBroken = true;
                player.Player.life.serverSetLegsBroken(false);
            }
        }
        private void triedHealingFracture()
        {
            legsBroken = false;
        }
        private IEnumerator sprintDamage()
        {
            while (active && player?.life != null)
            {
                checkDamagePlayer();

                yield return new WaitForSecondsRealtime(interval);
                gotDamaged = false;
            }
        }
        private void checkDamagePlayer()
        {
            if (gotDamaged || !legsBroken || !isSprinting)
                return;

            gotDamaged = true;
            DamagePlayerParameters parameters = new DamagePlayerParameters(player);
            parameters.cause = EDeathCause.BONES;
            parameters.limb = ELimb.LEFT_LEG;
            parameters.killer = steamID;
            parameters.damage = damage;
            parameters.applyGlobalArmorMultiplier = false;

            DamageTool.damagePlayer(parameters, out EPlayerKill _);
            UnturnedPrivateFields.trySendDamagedEvent(player.life, flinchAmount, Vector3.left);
        }
    }
}
