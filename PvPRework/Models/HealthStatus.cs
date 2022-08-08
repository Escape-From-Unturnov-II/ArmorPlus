using Rocket.Core.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SpeedMann.PvPRework.Controllers.HealthManager;

namespace SpeedMann.PvPRework.Models
{

    internal class HealthStatus
    {
        private float blackedDamageMultiplier = 1.3f;
        private int maxLightBleedsPerBodyPart = 3;
        private int maxHeavyBleedsPerBodyPart = 1;
        private int maxHealth = 0;
        internal bool vanillaBrokenLimb = false;
        internal bool vanillaBleeding = false;

        private Dictionary<BodyPart, BodyPartStatus> bodyParts = new Dictionary<BodyPart, BodyPartStatus>();

        internal HealthStatus(float blackedDamageMultiplier, int headHealth, int chestHealth, int somachHealth, int armHealth, int legHealth)
        {
            this.blackedDamageMultiplier = blackedDamageMultiplier;
            foreach (BodyPart bodyPart in BodyPart.GetValues(typeof(BodyPart)))
            {
                switch (bodyPart)
                {
                    case BodyPart.Head:
                        bodyParts.Add(bodyPart, new BodyPartStatus(headHealth));
                        maxHealth += headHealth;
                        break;
                    case BodyPart.Chest:
                        bodyParts.Add(bodyPart, new BodyPartStatus(chestHealth));
                        maxHealth += chestHealth;
                        break;
                    case BodyPart.Stomach:
                        bodyParts.Add(bodyPart, new BodyPartStatus(somachHealth));
                        maxHealth += somachHealth;
                        break;
                    case BodyPart.ArmLeft:
                    case BodyPart.ArmRight:
                        bodyParts.Add(bodyPart, new LimbStatus(armHealth));
                        maxHealth += armHealth;
                        break;
                    case BodyPart.LegLeft:
                    case BodyPart.LegRight:
                        bodyParts.Add(bodyPart, new LimbStatus(legHealth));
                        maxHealth += legHealth;
                        break;
                    default:
                        Logger.LogError($"Tried to create HealthStatus for invalid body part {bodyPart}!");
                        break;
                }
            }
        }
        internal int getMaxHealth()
        {
            return maxHealth;
        }
        internal int getMaxHealth(BodyPart bodyPart)
        {
            if (bodyParts.TryGetValue(bodyPart, out BodyPartStatus status))
            {
                return status.maxHealth;
            }
            return 0;
        }
        internal int getHealth(BodyPart bodyPart)
        {
            if (bodyParts.TryGetValue(bodyPart, out BodyPartStatus status))
            {
                return status.health;
            }
            return 0;
        }
        internal int getLightBleedCount(BodyPart bodyPart)
        {
            if (bodyParts.TryGetValue(bodyPart, out BodyPartStatus status))
            {
                return status.bleedCountLight;
            }
            return 0;
        }
        internal int getHeavyBleedCount(BodyPart bodyPart)
        {
            if (bodyParts.TryGetValue(bodyPart, out BodyPartStatus status))
            {
                return status.bleedCountHeavy;
            }
            return 0;
        }
        internal bool isBlacked(BodyPart bodyPart)
        {
            if (bodyParts.TryGetValue(bodyPart, out BodyPartStatus status))
            {
                return status.blacked;
            }
            return false;
        }
        internal void heal(BodyPart bodyPart, ref int heal)
        {
            if (!bodyParts.TryGetValue(bodyPart, out BodyPartStatus status) || status.blacked || status.health == status.maxHealth) return;
            
            Logger.Log($"Healed {bodyPart}");
            if (status.health + heal > status.maxHealth)
            {
                heal -= (status.maxHealth - status.health);
                status.health = status.maxHealth;
                return;
            }
            status.health += heal;
            heal = 0;
        }
        internal void damage(BodyPart bodyPart, ref int damage, out bool dead, bool directHit = true)
        {
            dead = false;
            if (!bodyParts.TryGetValue(bodyPart, out BodyPartStatus status)) return;

            if (status.blacked)
            {
                if (bodyPart == BodyPart.Head || bodyPart == BodyPart.Chest)
                {
                    dead = true;
                }
                if (directHit)
                {
                    damage = (int)Math.Round(damage * blackedDamageMultiplier);
                }
                return;
            }
            Logger.Log($"Damaged {bodyPart} {damage}");
            if (status.health <= damage)
            {
                damage -= status.health;
                status.health = 0;
                if (bodyPart == BodyPart.Head || bodyPart == BodyPart.Chest)
                {
                    dead = true;
                }
                return;
            }
            status.health -= damage;
            damage = 0;
        }

        internal bool tryHealBleeding(BodyPart bodyPart, bool heavy = false)
        {
            if (bodyParts.TryGetValue(bodyPart, out BodyPartStatus bodyPartStatus))
            {
                if (!heavy && bodyPartStatus.bleedCountLight > 0)
                {
                    bodyPartStatus.bleedCountLight--;
                    return true;
                }
                if (heavy && bodyPartStatus.bleedCountHeavy > 0)
                {
                    bodyPartStatus.bleedCountHeavy--;
                    return true;
                }
                return false;
            }
            Logger.LogError($"Could not stop {(heavy ? "heavy":"light")} bleeding of {bodyPart}");
            return false;
        }
        internal bool tryAddBleeding(BodyPart bodyPart, int count, bool heavy = false)
        {
            if (bodyParts.TryGetValue(bodyPart, out BodyPartStatus bodyPartStatus))
            {
                if (!heavy)
                {
                    if (bodyPartStatus.bleedCountLight + count <= maxLightBleedsPerBodyPart)
                    {
                        bodyPartStatus.bleedCountLight += count;
                        return true;
                    }
                    else
                    {
                        bodyPartStatus.bleedCountLight = maxLightBleedsPerBodyPart;
                        return true;
                    }
                }
                else
                {
                    if(bodyPartStatus.bleedCountHeavy + count <= maxHeavyBleedsPerBodyPart)
                    {
                        bodyPartStatus.bleedCountHeavy += count;
                        return true;
                    }
                    else
                    {
                        bodyPartStatus.bleedCountHeavy = maxHeavyBleedsPerBodyPart;
                        return true;
                    }
                }
            }
            Logger.LogError($"Could not add {(heavy ? "heavy" : "light")} bleeding to {bodyPart}");
            return false;
        }
        internal bool tryRepairLimb(BodyPart bodyPart)
        {
            if (bodyParts.TryGetValue(bodyPart, out BodyPartStatus bodyPartStatus) && bodyPartStatus is LimbStatus)
            {
                LimbStatus limb = (LimbStatus)bodyPartStatus;
                if (limb.broken)
                {
                    limb.broken = false;
                    return true;
                }
                return false;
            }
            Logger.LogError($"Could not repair {bodyPart}");
            return false;
        }
        internal bool tryBreakLimb(BodyPart bodyPart)
        {
            if (bodyParts.TryGetValue(bodyPart, out BodyPartStatus bodyPartStatus) && bodyPartStatus is LimbStatus)
            {
                LimbStatus limb = (LimbStatus)bodyPartStatus;
                if (!limb.broken)
                {
                    limb.broken = true;
                    return true;
                }
                return false;
            }
            Logger.LogError($"Could not break {bodyPart}");
            return false;
        }
        internal int getBrokenLimbCount()
        {
            int count = 0;
            foreach (KeyValuePair<BodyPart, BodyPartStatus> entry in bodyParts)
            {
                if (isBroken(entry.Key))
                {
                    count++;
                }
            }
            return count;
        }
        internal bool isBroken(BodyPart bodyPart)
        {
            if(bodyParts.TryGetValue(bodyPart, out BodyPartStatus bodyPartStatus) && bodyPartStatus is LimbStatus)
            {
                return ((LimbStatus)bodyPartStatus).broken;
            }
            return false;
        }


        internal class BodyPartStatus
        {
            internal int maxHealth = 0;
            internal int health = 0;
            internal int bleedCountLight = 0;
            internal int bleedCountHeavy = 0;
            internal bool blacked { get { return health <= 0; } }
            internal BodyPartStatus(int maxHealth)
            {
                this.maxHealth = maxHealth;
                this.health = maxHealth;
            }
        }
        internal class LimbStatus : BodyPartStatus
        {
            internal bool broken = false;

            internal LimbStatus(int maxHealth) : base(maxHealth)
            {
            }
        }
    }
}
