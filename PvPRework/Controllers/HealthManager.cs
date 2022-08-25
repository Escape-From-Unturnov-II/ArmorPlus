using Rocket.Core.Logging;
using Rocket.Unturned.Player;
using SDG.Unturned;
using SpeedMann.PvPRework.Models;
using SpeedMann.PvPRework.Models.Config;
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
        internal static HealthManagerConfig Conf { get; private set; }
        private static int maxHeadHealth = 35;
        private static int maxChestHealth = 85;
        private static int maxStomachHealth = 70;
        private static int maxArmHealth = 60;
        private static int maxLegHealth = 65;
        private static float blackedMulti = 1.3f;
        private static List<BodyPart> bodyPartOrder;
        private static List<BodyPart> fractureableBodyPartOrder;
        private static Dictionary<ushort, MedicalExtension> betterMedDict;
        

        internal static void Init(HealthManagerConfig conf)
        {
            Conf = conf;
            betterMedDict = PvPRework.createDictionaryFromItemExtensions(Conf.BetterMeds);
            bodyPartOrder = new List<BodyPart>();
            foreach (BodyPart part in BodyPart.GetValues(typeof(BodyPart)))
            {
                bodyPartOrder.Add(part);
            }
            fractureableBodyPartOrder = new List<BodyPart> { BodyPart.ArmLeft, BodyPart.ArmRight, BodyPart.LegLeft, BodyPart.LegRight };

            DrugEffectControler.Init(Conf);
        }

        internal static void Update()
        {
            // add health changes
            // store cahnged data in db
        }

        private static Dictionary<CSteamID, HealthStatus> healthStatusOfPlayers = new Dictionary<CSteamID, HealthStatus>();

        internal static void OnPlayerConnected(UnturnedPlayer player)
        {
            HealthStatus newStatus = resetHealthStatus(player);
            HealthUIHandler.spawnHealthUI(player.CSteamID, newStatus);
            DrugEffectControler.OnPlayerConnected(player);
        }
        internal static void OnPlayerDisconnected(UnturnedPlayer player)
        {
            healthStatusOfPlayers.Remove(player.CSteamID);
            
        }
        internal static void OnPrePlayerDisconnected(CSteamID playerId)
        {
            DrugEffectControler.OnPrePlayerDisconnected(playerId);
        }
        internal static void OnPlayerRevived(UnturnedPlayer player)
        {
            HealthStatus newStatus = resetHealthStatus(player);
            HealthUIHandler.spawnHealthUI(player.CSteamID, newStatus);
        }
        internal static void OnPlayerDeath(UnturnedPlayer player)
        {
            HealthUIHandler.setHealthUIVisibility(player.CSteamID, false);
        }
        internal static void OnConsumed(Player target, Player instigator, ItemConsumeableAsset asset)
        {
            UnturnedPlayer player = UnturnedPlayer.FromPlayer(target);
            if (!healthStatusOfPlayers.TryGetValue(player.CSteamID, out HealthStatus status))
            {
                Logger.LogError($"no player health status for {player.CSteamID}");
                return;
            }
            if (Conf.EnableReuseableMeds && asset.amount > 1)
            {
                byte page = instigator.equipment.equippedPage;
                byte x = instigator.equipment.equipped_x;
                byte y = instigator.equipment.equipped_y;
                byte index = instigator.inventory.getIndex(page, x, y);
                ItemJar itemJar = instigator.inventory.getItem(page, index);

                if (itemJar.item.amount > 1)
                {
                    instigator.inventory.sendUpdateAmount(page, x, y, (byte)(itemJar.item.amount - 1));
                }
                else
                {
                    instigator.inventory.removeItem(page, index);
                }
            }
            if(betterMedDict.TryGetValue(asset.id, out MedicalExtension med))
            {
                if(med.Effects.Count > 0)
                {
                    UnturnedPlayer uPlayer = UnturnedPlayer.FromPlayer(target); 
                    DrugEffectControler.updateEffects(uPlayer, med.Effects);
                }
            }
            
            Logger.Log($"Healed {asset.health}");
            heal(player, asset.health);
        }
        internal static void fractureCheck(PlayerLife playerLife)
        {
            UnturnedPlayer player = UnturnedPlayer.FromPlayer(playerLife.player);
            if (!healthStatusOfPlayers.TryGetValue(player.CSteamID, out HealthStatus status))
            {
                Logger.LogError($"no player health status for {player.CSteamID}");
                return;
            }
            Logger.Log($"Fracture Update: {status.vanillaBrokenLimb} / {playerLife.isBroken}");
            if(status.vanillaBrokenLimb != playerLife.isBroken)
            {
                if (status.vanillaBrokenLimb)
                {
                    removeFracture(player, true);
                }
                else
                {
                    addFracture(player, BodyPart.LegLeft, false);
                    addFracture(player, BodyPart.LegRight, true);
                }
                status.vanillaBrokenLimb = !status.vanillaBrokenLimb;
            }
        }
        internal static void bleedCheck(PlayerLife playerLife)
        {
            UnturnedPlayer player = UnturnedPlayer.FromPlayer(playerLife.player);
            if (!healthStatusOfPlayers.TryGetValue(player.CSteamID, out HealthStatus status))
            {
                Logger.LogError($"no player health status for {player.CSteamID}");
                return;
            }
            if (status.vanillaBleeding != playerLife.isBleeding)
            {
                if (status.vanillaBleeding)
                {
                    stopBleed(player, false);
                }
                else
                {
                    addBleed(player, BodyPart.Stomach, 1);
                }
                status.vanillaBleeding = !status.vanillaBleeding;
            }

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
                case EDeathCause.SPLASH:
                case EDeathCause.BLEEDING:
                case EDeathCause.BURNING:
                // Explosions
                case EDeathCause.CHARGE:
                case EDeathCause.GRENADE:
                case EDeathCause.MISSILE:
                case EDeathCause.LANDMINE:
                case EDeathCause.VEHICLE:
                case EDeathCause.BURNER:
                    damageWholeBody(player, amount * 2, out dead);
                    break;
                case EDeathCause.BONES:
                    bool killed;
                    damageBodyPart(player, BodyPart.LegLeft, amount * 2, out killed);
                    if (killed) dead = true;
                    damageBodyPart(player, BodyPart.LegRight, amount * 2, out killed);
                    if (killed) dead = true;
                    break;
                case EDeathCause.BREATH:
                    damageBodyPart(player, BodyPart.Head, amount, out dead);
                    break;
                case EDeathCause.INFECTION:
                case EDeathCause.WATER:
                case EDeathCause.FOOD:
                    damageBodyPart(player, BodyPart.Stomach, amount * 4, out dead);
                    break;
                case EDeathCause.ZOMBIE:
                    damageBodyPart(player, BodyPart.Chest, amount, out dead);
                    break;
                // Zombies
                case EDeathCause.ACID:
                case EDeathCause.SPARK:
                case EDeathCause.SPIT:
                case EDeathCause.BOULDER:
                // Animal
                case EDeathCause.ANIMAL:
                // PVP
                case EDeathCause.SHRED:
                case EDeathCause.SENTRY:
                case EDeathCause.ROADKILL:
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
                if (remainingHealth <= 0) break;
                status.heal(part, ref remainingHealth);
            }

            if (updateUI)
            {
                HealthUIHandler.updateHealthUI(player.CSteamID, status);
            } 
        }
        internal static void stopBleed(UnturnedPlayer player, bool heavy = false, bool updateUI = true)
        {
            if (!healthStatusOfPlayers.TryGetValue(player.CSteamID, out HealthStatus status))
            {
                Logger.LogError($"no player health status for {player.CSteamID}");
                return;
            }

            foreach(BodyPart part in bodyPartOrder)
            {
                if(status.tryHealBleeding(part, heavy))
                {
                    break;
                }
            }

            if (updateUI)
            {
                HealthUIHandler.updateHealthUI(player.CSteamID, status);
            }
        }
        internal static void addBleed(UnturnedPlayer player, BodyPart bodyPart, int count, bool heavy = false, bool updateUI = true)
        {
            if (!healthStatusOfPlayers.TryGetValue(player.CSteamID, out HealthStatus status))
            {
                Logger.LogError($"no player health status for {player.CSteamID}");
                return;
            }
            if(count > 0)
            {
                status.tryAddBleeding(bodyPart, count, heavy);
            }

            if (updateUI)
            {
                HealthUIHandler.setHealthEffectVisibility(player.CSteamID, HealthUIHandler.HealthEffect.BleedingLight, count);
            }
        }
        internal static void removeFracture(UnturnedPlayer player, bool updateUI = true)
        {
            if (!healthStatusOfPlayers.TryGetValue(player.CSteamID, out HealthStatus status))
            {
                Logger.LogError($"no player health status for {player.CSteamID}");
                return;
            }

            foreach (BodyPart part in fractureableBodyPartOrder)
            {
                if (status.tryRepairLimb(part))
                {
                    break;
                }
            }

            if (updateUI)
            {
                HealthUIHandler.updateHealthUI(player.CSteamID, status);
            }
        }
        internal static void addFracture(UnturnedPlayer player, BodyPart bodyPart, bool updateUI = true)
        {
            if (!healthStatusOfPlayers.TryGetValue(player.CSteamID, out HealthStatus status))
            {
                Logger.LogError($"no player health status for {player.CSteamID}");
                return;
            }
            status.tryBreakLimb(bodyPart);

            if (updateUI)
            {
                HealthUIHandler.updateHealthUI(player.CSteamID, status);
            }
        }
        internal static void addFracture(CSteamID executorID, HealthStatus status, BodyPart bodyPart, bool updateUI = true)
        {
            status.tryBreakLimb(bodyPart);

            if (updateUI)
            {
                HealthUIHandler.updateHealthUI(executorID, status);
            }
        }
        internal static void damageWholeBody(UnturnedPlayer player, int totalDamage, out bool dead, bool updateUI = true)
        {
            dead = false;
            if (!healthStatusOfPlayers.TryGetValue(player.CSteamID, out HealthStatus status))
            {
                Logger.LogError($"no player health status for {player.CSteamID}");
                return;
            }

            List<BodyPart> validBodyParts = new List<BodyPart>(bodyPartOrder);

            damageRemainingBodyParts(status, validBodyParts, totalDamage, out dead, true);

            if (updateUI)
            {
                HealthUIHandler.updateHealthUI(player.CSteamID, status);
            }
            
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
                List<BodyPart> validBodyParts = getRemainingBodyParts(status, false);
                damageRemainingBodyParts(status, validBodyParts, damage, out dead, false);
            }

            HealthUIHandler.updateHealthUI(player.CSteamID, status);
        }
        private static void damageRemainingBodyParts(HealthStatus status, List<BodyPart> bodyParts, int totalDamage, out bool dead, bool directHit = true)
        {
            dead = false;
            bool killed;
            int remainingDamage = totalDamage;
            for (int i = 0; i < bodyParts.Count; i++)
            {
                int damage = remainingDamage / (bodyParts.Count - i);
                remainingDamage -= damage;
                status.damage(bodyParts[i], ref damage, out killed, directHit);
                remainingDamage += damage;

                if (remainingDamage <= 0) break;
                if (killed) dead = true;
            }
            dead = dead || remainingDamage > 0;
        }
        #endregion
        private static HealthStatus resetHealthStatus(UnturnedPlayer player)
        {
            HealthStatus newStatus = new HealthStatus(blackedMulti, maxHeadHealth, maxChestHealth, maxStomachHealth, maxArmHealth, maxLegHealth);
            newStatus.vanillaBleeding = player.Player.life.isBleeding;
            newStatus.vanillaBrokenLimb = player.Player.life.isBroken;
            if (healthStatusOfPlayers.ContainsKey(player.CSteamID))
            {
                healthStatusOfPlayers[player.CSteamID] = newStatus;
            }
            else
            {
                healthStatusOfPlayers.Add(player.CSteamID, newStatus);
            }
            return newStatus;
        }
        private static List<BodyPart> getRemainingBodyParts(HealthStatus status, bool reversed = false)
        {
            List<BodyPart> validBodyParts = new List<BodyPart>();
            foreach (BodyPart bodyPart in bodyPartOrder)
            {
                switch (bodyPart)
                {
                    case BodyPart.Head:
                    case BodyPart.Chest:
                        validBodyParts.Add(bodyPart);
                        break;
                    default:
                        if (status.isBlacked(bodyPart)) continue;
                        validBodyParts.Add(bodyPart);
                        break;
                }
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
