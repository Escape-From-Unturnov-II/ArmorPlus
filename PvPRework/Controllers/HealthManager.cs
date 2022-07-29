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
        private static float maxHeadHealth = 35;
        private static float maxChestHealth = 85;
        private static float maxStomachHealth = 70;
        private static float maxArmHealth = 60;
        private static float maxLegHealth = 65;
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

        private static Dictionary<CSteamID, HealthStatus> healthStatusOfPlayers = new Dictionary<CSteamID, HealthStatus>();

        internal static void OnPlayerConnected(UnturnedPlayer player)
        {
            healthStatusOfPlayers.Add(player.CSteamID, new HealthStatus(maxHeadHealth, maxChestHealth, maxStomachHealth, maxArmHealth, maxLegHealth));
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
            modifyBleeding(player, asset.bleedingModifier);
            modifyFracture(player, asset.bonesModifier);
            heal(player, asset.health);
        }
        internal static void heal(UnturnedPlayer player, float health, bool updateUI = true)
        {
            if (!healthStatusOfPlayers.TryGetValue(player.CSteamID, out HealthStatus status))
            {
                Logger.LogError($"no player health status for {player.CSteamID}");
                return;
            }
            float remainingHealth = health; 
            foreach (BodyPart part in bodyPartOrder)
            {
                if (remainingHealth <= 0) return;
                remainingHealth = status.heal(part, health);
            }

            if (updateUI)
            {
                HealthUIHandler.updateHealthUI(player.CSteamID, status);
            } 
        }
        internal static void modifyBleeding(UnturnedPlayer player, ItemConsumeableAsset.Bleeding modification)
        {
            if (modification == ItemConsumeableAsset.Bleeding.None) return;
            if (!healthStatusOfPlayers.TryGetValue(player.CSteamID, out HealthStatus status))
            {
                Logger.LogError($"no player health status for {player.CSteamID}");
                return;
            }

            HealthUIHandler.setHealthEffectVisibility(player.CSteamID, HealthUIHandler.HealthEffect.Bleeding, modification == ItemConsumeableAsset.Bleeding.Cut);
        }
        internal static void modifyFracture(UnturnedPlayer player, ItemConsumeableAsset.Bones modification)
        {
            if (modification == ItemConsumeableAsset.Bones.None) return;
            if (!healthStatusOfPlayers.TryGetValue(player.CSteamID, out HealthStatus status))
            {
                Logger.LogError($"no player health status for {player.CSteamID}");
                return;
            }
            HealthUIHandler.setHealthEffectVisibility(player.CSteamID, HealthUIHandler.HealthEffect.Fracture, modification == ItemConsumeableAsset.Bones.Break);
        }
        internal static void damageBodyPart(UnturnedPlayer player, ExtendetHitLocation hitLocation, float damage, out bool dead)
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
        internal static void damageBodyPart(UnturnedPlayer player, BodyPart bodyPart, float damage, out bool dead)
        {
            dead = false;
            if (!healthStatusOfPlayers.TryGetValue(player.CSteamID, out HealthStatus status))
            {
                Logger.LogError($"no player health status for {player.CSteamID}");
                return;
            }

            float remainingDamage = status.damage(bodyPart, damage, out dead);
            if (dead) return;
            
            if(remainingDamage >= 1)
            {
                int count = 0;
                damageRecursion(status, 0, remainingDamage * blackedMulti, ref count, ref dead);
            }
            Logger.Log($"Health manager Damaged {bodyPart}");
            HealthUIHandler.updateHealthUI(player.CSteamID, status);
        }
        private static void damageRecursion(HealthStatus status, int index, float damage, ref int count, ref bool dead)
        {
            if (index >= bodyPartOrder.Count) return;
            bool valid = false;
            BodyPart current = bodyPartOrder[index];
            if (current == BodyPart.Head || current == BodyPart.Chest || !status.isBlacked(current)){
                count++;
                 valid = true;
            }

            damageRecursion(status, index++, damage, ref count, ref dead);
            if (valid && !dead)
            {
                status.damage(current, damage / count, out dead);
            }
            
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
