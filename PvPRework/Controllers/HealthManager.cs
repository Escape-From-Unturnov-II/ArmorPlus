using Rocket.Core.Logging;
using Rocket.Unturned.Player;
using SDG.Unturned;
using SpeedMann.PvPRework.Models;
using SpeedMann.PvPRework.UI;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeedMann.PvPRework.Controllers
{
    internal class HealthManager
    {
        private static int maxHeadHealth = 35;
        private static int maxChestHealth = 85;
        private static int maxStomachHealth = 70;
        private static int maxArmHealth = 60;
        private static int maxLegHealth = 65;
        private static float blackedMulti = 1.3f;
        private static List<BodyPart> bodyPartOrder;

        internal static void Init()
        {
            bodyPartOrder = new List<BodyPart>();
            foreach (BodyPart part in BodyPart.GetValues(typeof(BodyPart)))
            {
                bodyPartOrder.Add(part);
            }
        }

        internal static void Update()
        {
            // add health changes
            // store cahnged data in db
        }

        private static Dictionary<CSteamID, HealthStatus> healthStatusOfPlayers = new Dictionary<CSteamID, HealthStatus>();

        internal static void OnPlayerConnected(UnturnedPlayer player)
        {
            HealthStatus newStatus = new HealthStatus(blackedMulti, maxHeadHealth, maxChestHealth, maxStomachHealth, maxArmHealth, maxLegHealth);
            if (healthStatusOfPlayers.ContainsKey(player.CSteamID))
            {
                healthStatusOfPlayers[player.CSteamID] = newStatus;
            }
            else
            {
                healthStatusOfPlayers.Add(player.CSteamID, newStatus);
            }
            HealthUIHandler.spawnHealthUI(player.CSteamID);
        }
        internal static void OnPlayerDisconnected(UnturnedPlayer player)
        {
            healthStatusOfPlayers.Remove(player.CSteamID);
        }
        internal static void OnPlayerRevived(UnturnedPlayer player)
        {
            HealthUIHandler.spawnHealthUI(player.CSteamID);
        }
        internal static void OnPlayerDeath(UnturnedPlayer player)
        {
            HealthUIHandler.setHealthUIVisibility(player.CSteamID, false);
        }
        internal static void OnConsumed(Player target, ItemConsumeableAsset asset)
        {
            UnturnedPlayer player = UnturnedPlayer.FromPlayer(target);
            if (asset.bleedingModifier != ItemConsumeableAsset.Bleeding.None)
            {
                modifyBleeding(player, asset.bleedingModifier == ItemConsumeableAsset.Bleeding.Cut);
            }
            if(asset.bonesModifier != ItemConsumeableAsset.Bones.None)
            {
                modifyFracture(player, asset.bonesModifier == ItemConsumeableAsset.Bones.Break, BodyPart.LegLeft);
                modifyFracture(player, asset.bonesModifier == ItemConsumeableAsset.Bones.Break, BodyPart.LegRight);
            }
            
            heal(player, asset.health);
        }
        internal static void damageCheck(UnturnedPlayer player, ref byte amount, EDeathCause cause, ref ELimb limb, CSteamID killer, ref bool canCauseBleeding, ref bool shouldAllow)
        {
            bool dead = false;
            switch (cause)
            {
                // Special
                case EDeathCause.KILL:
                case EDeathCause.SUICIDE:
                    dead = true;
                    break;
                // Environment
                case EDeathCause.ARENA:
                case EDeathCause.FREEZING:
                case EDeathCause.INFECTION:
                case EDeathCause.WATER:
                case EDeathCause.FOOD:
                case EDeathCause.BREATH:
                case EDeathCause.SPLASH:
                case EDeathCause.BLEEDING:
                case EDeathCause.BONES:
                case EDeathCause.BURNING:
                // Explosions
                case EDeathCause.CHARGE:
                case EDeathCause.GRENADE:
                case EDeathCause.MISSILE:
                case EDeathCause.LANDMINE:
                    damageWholeBody(player, amount, out dead);
                    break;
                // Zombies
                case EDeathCause.ACID:
                case EDeathCause.SPARK:
                case EDeathCause.SPIT:
                case EDeathCause.BURNER:
                case EDeathCause.BOULDER:
                case EDeathCause.ZOMBIE:
                // Animal
                case EDeathCause.ANIMAL:
                // PVP
                case EDeathCause.SHRED:
                case EDeathCause.SENTRY:
                case EDeathCause.ROADKILL:
                case EDeathCause.VEHICLE:
                    damageBodyPart(player, limb, amount, out dead);
                    break;
                case EDeathCause.GUN:
                case EDeathCause.PUNCH:
                case EDeathCause.MELEE:
                    // already handled with armor logic with extended hit zones
                    break;
            }
            amount = 1;
            if (dead) amount = 101;
        }
        #region Health Functions
        internal static void heal(UnturnedPlayer player, int health, bool updateUI = true)
        {
            if (!healthStatusOfPlayers.TryGetValue(player.CSteamID, out HealthStatus status))
            {
                Logger.LogError($"no player health status for {player.CSteamID}");
                return;
            }
            int remainingHealth = health; 
            foreach (BodyPart part in bodyPartOrder)
            {
                if (remainingHealth <= 0) return;
                status.heal(part, ref remainingHealth);
            }

            if (updateUI)
            {
                HealthUIHandler.updateHealthUI(player.CSteamID, status);
            } 
        }
        internal static void modifyBleeding(UnturnedPlayer player, bool modification)
        {
            if (!healthStatusOfPlayers.TryGetValue(player.CSteamID, out HealthStatus status))
            {
                Logger.LogError($"no player health status for {player.CSteamID}");
                return;
            }

            HealthUIHandler.setHealthEffectVisibility(player.CSteamID, HealthUIHandler.HealthEffect.Bleeding, modification);
        }
        internal static void modifyFracture(UnturnedPlayer player, bool modification, BodyPart bodyPart)
        {
            if (!healthStatusOfPlayers.TryGetValue(player.CSteamID, out HealthStatus status))
            {
                Logger.LogError($"no player health status for {player.CSteamID}");
                return;
            }
            status.tryBreakLimb(bodyPart);

            HealthUIHandler.setHealthEffectVisibility(player.CSteamID, HealthUIHandler.HealthEffect.Fracture, status);
        }
        internal static void damageWholeBody(UnturnedPlayer player, int totalDamage, out bool dead)
        {
            dead = false;
            if (!healthStatusOfPlayers.TryGetValue(player.CSteamID, out HealthStatus status))
            {
                Logger.LogError($"no player health status for {player.CSteamID}");
                return;
            }

            List<BodyPart> validBodyParts = new List<BodyPart>(bodyPartOrder);
            validBodyParts.Reverse();

            damageBodyRemainingBodyParts(status, validBodyParts, totalDamage, out dead);

            HealthUIHandler.updateHealthUI(player.CSteamID, status);
        }
        internal static void damageBodyPart(UnturnedPlayer player, ELimb limb, int damage, out bool dead)
        {
            ExtendetHitLocation hitLocation = ExtendedHitLocations.getExtendetHitlocation(limb);
            damageBodyPart(player, hitLocation, damage, out dead);
        }
        internal static void damageBodyPart(UnturnedPlayer player, ExtendetHitLocation hitLocation, int damage, out bool dead)
        {
            dead = false;
            BodyPart bodyPart;
            switch (hitLocation)
            {
                case ExtendetHitLocation.EARS:
                case ExtendetHitLocation.FACE:
                case ExtendetHitLocation.SKULL:
                    bodyPart = BodyPart.Head;
                    break;
                case ExtendetHitLocation.SPINE:
                    bodyPart = BodyPart.Chest;
                    break;
                case ExtendetHitLocation.STOMACH:
                    bodyPart = BodyPart.Stomach;
                    break;
                case ExtendetHitLocation.LEFT_ARM:
                    bodyPart = BodyPart.ArmLeft;
                    break;
                case ExtendetHitLocation.RIGHT_ARM:
                    bodyPart = BodyPart.ArmRight;
                    break;
                case ExtendetHitLocation.LEFT_LEG:
                    bodyPart = BodyPart.LegLeft;
                    break;
                case ExtendetHitLocation.RIGHT_LEG:
                    bodyPart = BodyPart.LegRight;
                    break;
                default:
                    Logger.LogError($"invalid ExtendetHitLocation in damageBodyPart {hitLocation}");
                    return;
            }

            damageBodyPart(player, bodyPart, damage, out dead);
        }
        internal static void damageBodyPart(UnturnedPlayer player, BodyPart bodyPart, int damage, out bool dead)
        {
            dead = false;
            if (!healthStatusOfPlayers.TryGetValue(player.CSteamID, out HealthStatus status))
            {
                Logger.LogError($"no player health status for {player.CSteamID}");
                return;
            }
            Logger.Log($"Damaged {bodyPart}");

            status.damage(bodyPart, ref damage, out dead);
            if (!dead && damage > 0)
            {
                List<BodyPart> validBodyParts = getRemainingBodyParts(status, true);
                damageBodyRemainingBodyParts(status, validBodyParts, damage, out dead);
            }

            HealthUIHandler.updateHealthUI(player.CSteamID, status);
        }
        private static void damageBodyRemainingBodyParts(HealthStatus status, List<BodyPart> bodyParts, int totalDamage, out bool dead, bool deadly = true)
        {
            dead = false;
            int remainingDamage = totalDamage;
            for (int i = 0; i < bodyParts.Count; i++)
            {
                if (status.isBlacked(bodyParts[i])) continue;

                int damage = remainingDamage / (bodyParts.Count - i);
                remainingDamage -= damage;
                status.damage(bodyParts[i], ref damage, out dead);
                remainingDamage += damage;

                if (deadly && dead || remainingDamage <= 0) break;
            }
            dead = dead || remainingDamage > 0;
        }
        #endregion

        private static List<BodyPart> getRemainingBodyParts(HealthStatus status, bool reversed = false)
        {
            List<BodyPart> validBodyParts = new List<BodyPart>();
            foreach (BodyPart bodyPart in bodyPartOrder)
            {
                if (status.isBlacked(bodyPart)) continue;
                validBodyParts.Add(bodyPart);
            }

            if(reversed) validBodyParts.Reverse();

            return validBodyParts;
        }
        // this order is important for damage and heal order
        public enum BodyPart
        {
            Head,
            Chest,
            Stomach,
            ArmLeft,
            ArmRight,
            LegRight,
            LegLeft,
        }
    }
}
