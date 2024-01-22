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
using static System.Net.Mime.MediaTypeNames;
using Logger = Rocket.Core.Logging.Logger;

namespace SpeedMann.PvPRework.Models.MedicalEffects
{
    internal class PainkillerEffect : MedicalEffect
    {
        private CSteamID steamID;
        private PainkillerConfig config;

        private bool legsBroken = false;
        private bool isSprinting = false;
        private bool gotDamaged = false;
        private bool active = false;
        private float currentFallDamage = 0;
        internal PainkillerEffect(Player player, float effectDuration, float effectDelay, PainkillerConfig config) : base(player, effectDuration, effectDelay)
        {
            unique = true;
            steamID = player.channel.owner.playerID.steamID;
            this.config = config;
            this.config.FractureRunningDamageInterval = config.FractureRunningDamageInterval > 0 ? config.FractureRunningDamageInterval : 1;
        }
        protected override void startInner()
        {
            active = true;
            gotDamaged = false;
            isSprinting = player.stance.stance == EPlayerStance.SPRINT;
            legsBroken = player.life.isBroken;
            player.life.serverSetLegsBroken(false);

            HealthManager.OnTriedHealingFracture += triedHealingFracture;
            UnturnedPlayerEvents.OnPlayerUpdateBroken += fractureChanged;
            player.stance.onStanceUpdated += stanceChanged;
            //TODO: move landing handler to HealthManager to also damage if painkillers are not active
            UnturnedPatches.OnPreLanded += preLanding;
            player.life.OnFallDamageRequested += onVanillaFallDamage;
            UnturnedPatches.OnPostLanded += postLanding;


            player.StartCoroutine(sprintDamageCheck());
        }

        protected override void stopInner()
        {
            HealthManager.OnTriedHealingFracture -= triedHealingFracture;
            UnturnedPlayerEvents.OnPlayerUpdateBroken -= fractureChanged;
            player.stance.onStanceUpdated -= stanceChanged;

            UnturnedPatches.OnPreLanded -= preLanding;
            player.life.OnFallDamageRequested -= onVanillaFallDamage;
            UnturnedPatches.OnPostLanded -= postLanding;

            player.life.serverSetLegsBroken(legsBroken);
            isSprinting = false;
            active = false;
        }
        private void stanceChanged()
        {
            EPlayerStance newStance = player.stance.stance;
            isSprinting = newStance == EPlayerStance.SPRINT;
            checkSprintDamage();
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
        private IEnumerator sprintDamageCheck()
        {
            while (active && player?.life != null)
            {
                checkSprintDamage();

                yield return new WaitForSecondsRealtime(config.FractureRunningDamageInterval);
                gotDamaged = false;
            }
        }
        private void preLanding(PlayerLife life, float velocity)
        {
            if(!legsBroken || !life.player.channel.owner.playerID.steamID.Equals(steamID))
                return;

            velocity = -velocity;
            if (velocity < config.FractureLandingMaxVelocity)
                return;

           
            currentFallDamage = config.FractureLandingBaseDamage * (velocity / config.FractureLandingVelocitySteps);
        }
        private void onVanillaFallDamage(PlayerLife life, float velocity, ref float damage, ref bool shouldBreakLegs)
        {
            if (!life.player.channel.owner.playerID.steamID.Equals(steamID))
                return;

            if(currentFallDamage > damage)
            {
                damage = currentFallDamage;
                currentFallDamage = 0;
            }
        }
        private void postLanding(PlayerLife life)
        {
            if (!life.player.channel.owner.playerID.steamID.Equals(steamID))
                return;

            if (currentFallDamage > 0)
            {
                Logger.Log($"{life.player.channel.owner.playerID.steamID} was damaged {currentFallDamage} by falling!");
                causeFractureDamage(currentFallDamage);
            }
            currentFallDamage = 0;
        }
        private void checkSprintDamage()
        {
            if (gotDamaged || !legsBroken || !isSprinting)
                return;
            Logger.Log($"{player.channel.owner.playerID.steamID} was damaged {config.FractureRunningDamage} by sprinting!");
            gotDamaged = true;
            causeFractureDamage(config.FractureRunningDamage);
        }
        private void causeFractureDamage(float damage)
        {
            DamagePlayerParameters parameters = new DamagePlayerParameters(player);
            parameters.cause = EDeathCause.BONES;
            parameters.limb = ELimb.LEFT_LEG;
            parameters.killer = steamID;
            parameters.damage = damage;
            parameters.applyGlobalArmorMultiplier = false;

            DamageTool.damagePlayer(parameters, out EPlayerKill _);
            HealthManager.causeFlinching(player.life, config.FractureDamageFlinch);
        }
    }
}
