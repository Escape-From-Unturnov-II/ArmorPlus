using Rocket.Core.Logging;
using Rocket.Unturned.Player;
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
        internal static void OnPlayerRevived(UnturnedPlayer player)
        {
            HealthUIHandler.setHealthUIVisibility(player.CSteamID, true);
        }
        internal static void OnPlayerDeath(UnturnedPlayer player)
        {
            HealthUIHandler.setHealthUIVisibility(player.CSteamID, false);
        }
        internal static void heal(UnturnedPlayer player, float health)
        {
            if (!healthStatusOfPlayers.TryGetValue(player.CSteamID, out HealthStatus status))
            {
                Logger.LogError($"no player health status for {player.CSteamID}");
                return;
            }
            float remainingHealth; 
            foreach (BodyPart part in bodyPartOrder)
            {
                remainingHealth = status.heal(part, health);
                if (remainingHealth <= 0) return;
            }

            HealthUIHandler.updateHealthUI(player.CSteamID, status);
        }
        internal static void damageBodypart(UnturnedPlayer player, BodyPart bodyPart, float damage, out bool dead)
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

            HealthUIHandler.updateHealthUI(player.CSteamID, status);
        }
        private static void damageRecursion(HealthStatus status, int index, float damage, ref int count, ref bool dead)
        {
            if (index >= bodyPartOrder.Count) return;

            BodyPart current = bodyPartOrder[index];
            if (current == BodyPart.Head || current == BodyPart.Chest || !status.isBlacked(current)){
                count++;
                damageRecursion(status, index++, damage, ref count, ref dead);
                if (dead) return;
                status.damage(current, damage/count, out dead);
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
