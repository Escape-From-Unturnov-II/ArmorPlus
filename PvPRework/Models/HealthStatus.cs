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
        
        private Dictionary<BodyPart, BodyPartStatus> bodyParts = new Dictionary<BodyPart, BodyPartStatus>();

        internal HealthStatus(int headHelth, int chestHealth, int somachHealth, int armHealth, int legHealth)
        {
            foreach (BodyPart bodyPart in BodyPart.GetValues(typeof(BodyPart)))
            {
                switch (bodyPart)
                {
                    case BodyPart.Head:
                        bodyParts.Add(bodyPart, new BodyPartStatus(headHelth));
                        break;
                    case BodyPart.Chest:
                        bodyParts.Add(bodyPart, new BodyPartStatus(chestHealth));
                        break;
                    case BodyPart.Stomach:
                        bodyParts.Add(bodyPart, new BodyPartStatus(somachHealth));
                        break;
                    case BodyPart.ArmLeft:
                    case BodyPart.ArmRight:
                        bodyParts.Add(bodyPart, new LimbStatus(armHealth));
                        break;
                    case BodyPart.LegLeft:
                    case BodyPart.LegRight:
                        bodyParts.Add(bodyPart, new LimbStatus(legHealth));
                        break;
                    default:
                        Logger.LogError($"Tried to create HealthStatus for invalid body part {bodyPart}!");
                        break;
                }
            }
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
            if (bodyParts.TryGetValue(bodyPart, out BodyPartStatus status) && !status.blacked)
            {
                if (status.health + heal > status.maxHealth)
                {
                    status.health = status.maxHealth;
                    heal -= (status.maxHealth - status.health);
                }
                status.health += heal;
                heal = 0;
            }
        }
        internal void damage(BodyPart bodyPart, ref int damage, out bool dead)
        {
            dead = false;
            if (!bodyParts.TryGetValue(bodyPart, out BodyPartStatus status)) return;

            if (status.blacked)
            {
                if (bodyPart == BodyPart.Head || bodyPart == BodyPart.Chest)
                {
                    dead = true;
                }
                damage = (int)Math.Round(damage * blackedDamageMultiplier);
                return;
            }
            Logger.Log($"Damaged {bodyPart} {damage}");
            if (status.health - damage <= 0)
            {
                status.health = 0;
                if (bodyPart == BodyPart.Head || bodyPart == BodyPart.Chest)
                {
                    dead = true;
                }
                damage -= status.health;
            }
            status.health -= damage;
            damage = 0;
        }

        internal void breakLimb(BodyPart bodyPart)
        {
            if (bodyParts.TryGetValue(bodyPart, out BodyPartStatus bodyPartStatus) && bodyPartStatus is LimbStatus)
            {
                ((LimbStatus)bodyPartStatus).broken = true;
            }
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
