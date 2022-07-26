using Rocket.Core.Logging;
using Rocket.Unturned.Player;
using SDG.Unturned;
using SpeedMann.PvPRework.Models;
using SpeedMann.PvPRework.Models.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Logger = Rocket.Core.Logging.Logger;

namespace SpeedMann.PvPRework.Helper
{
    internal class PlayerPenetration
    {
        internal static void TryPenetratePlayer(PlayerHit currentHit, UnturnedPlayer shooter, ItemWeaponAsset weapon, EDeathCause cause, ExtendetHitLocation hitBodypart, float penetration)
        {
            if (currentHit == null || shooter == null)
            {
                Logger.LogError($"Could not find shooter or hit");
                return;
            }
            PenResistence penResistance = null;
            switch (hitBodypart)
            {
                case ExtendetHitLocation.EARS:
                case ExtendetHitLocation.FACE:
                case ExtendetHitLocation.SKULL:
                    penResistance = PvPRework.Conf.BetterArmor.BetterHitZones.PlayerPenetration.Skull;
                    break;
                case ExtendetHitLocation.RIGHT_ARM:
                case ExtendetHitLocation.LEFT_ARM:
                    penResistance = PvPRework.Conf.BetterArmor.BetterHitZones.PlayerPenetration.Arm;
                    break;
                case ExtendetHitLocation.RIGHT_LEG:
                case ExtendetHitLocation.LEFT_LEG:
                    penResistance = PvPRework.Conf.BetterArmor.BetterHitZones.PlayerPenetration.Leg;
                    break;
                case ExtendetHitLocation.SPINE:
                    penResistance = PvPRework.Conf.BetterArmor.BetterHitZones.PlayerPenetration.Spine;
                    break;
                case ExtendetHitLocation.STOMACH:
                    penResistance = PvPRework.Conf.BetterArmor.BetterHitZones.PlayerPenetration.Stomach;
                    break;
            }

            if (penResistance == null)
            {
                Logger.LogError($"Could not find penResistance for {hitBodypart}");
                return;
            }

            if (currentHit.penCount <= PvPRework.Conf.BetterArmor.BetterHitZones.PlayerPenetration.MaxPenetrations && penetration >= penResistance.RequiredPenetration)
            {
                penetratePlayer(shooter, weapon, currentHit, penResistance, cause, penetration);
            }
        }
        private static void penetratePlayer(UnturnedPlayer shooter, ItemWeaponAsset weapon, PlayerHit currentHit, PenResistence penResistance, EDeathCause cause, float penetration) 
        {

            Vector3 newStartpoint = currentHit.imputInfo.point + (currentHit.imputInfo.direction.normalized * 0.1f);
            RaycastInfo info = DamageTool.raycast(new Ray(newStartpoint, currentHit.imputInfo.direction), 512f, RayMasks.DAMAGE_CLIENT, null);

            if (info?.player != null)
            {

                // calc pen reduction
                float penReduction = PvPRework.calcMean(penResistance.RequiredPenetration, penResistance.PenetrationForMinReduction, penResistance.MaxPenReduction, penResistance.MinPenReduction, penetration);
                penReduction = penReduction > 1 ? 1 : penReduction < 0 ? 0 : penReduction;
                float remainingPenetration = penReduction * penetration;


                Logger.Log($"Penetrated and hit {info.player.name} in the {info.limb} penReduction: {penReduction}");
                PvPRework.playerHits.Add(new PlayerHit(new InputInfo
                {
                    type = ERaycastInfoType.PLAYER,
                    player = info.player,
                    transform = info.transform,
                    point = info.point,
                    limb = info.limb,
                }, currentHit.penCount + 1, remainingPenetration));

                DamagePlayerParameters damageparam = new DamagePlayerParameters
                {
                    player = info.player,
                    cause = cause,
                    limb = info.limb,
                    direction = currentHit.imputInfo.direction,
                    damage = weapon.playerDamageMultiplier.multiply(info.limb),
                    times = 1,
                    killer = shooter.CSteamID,
                };
                PvPRework.Inst.playerPenetrations.Add(damageparam);
            }
        }
    }
}
