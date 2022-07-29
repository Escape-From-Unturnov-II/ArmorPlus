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
        
        private Dictionary<BodyPart, BodyPartStatus> bodyParts = new Dictionary<BodyPart, BodyPartStatus>();

        internal HealthStatus(float headHelth, float chestHealth, float somachHealth, float armHealth, float legHealth)
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
        internal float getMaxHealth(BodyPart bodyPart)
        {
            if (bodyParts.TryGetValue(bodyPart, out BodyPartStatus status))
            {
                return status.maxHealth;
            }
            return 0;
        }
        internal float getHealth(BodyPart bodyPart)
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
        internal float heal(BodyPart bodyPart, float heal)
        {
            if (bodyParts.TryGetValue(bodyPart, out BodyPartStatus status) && !status.blacked)
            {
                if (status.health + heal > status.maxHealth)
                {
                    status.health = status.maxHealth;
                    return heal - (status.maxHealth - status.health);
                }
                status.health += heal;
                return 0;
            }

            return heal;
        }
        internal float damage(BodyPart bodyPart, float damage, out bool dead)
        {
            dead = false;
            if (bodyParts.TryGetValue(bodyPart, out BodyPartStatus status) && !status.blacked)
            {
                if(status.health - damage < 0)
                {
                    status.health = 0;
                    if(bodyPart == BodyPart.Head || bodyPart == BodyPart.Chest)
                    {
                        dead = true;
                    }
                    return damage - status.health;
                }
                status.health -= damage;
                return 0;
            }
            return damage;
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
            internal float maxHealth = 0;
            internal float health = 0;
            internal bool blacked { get { return health <= 0; } }
            internal BodyPartStatus(float maxHealth)
            {
                this.maxHealth = maxHealth;
            }
        }
        internal class LimbStatus : BodyPartStatus
        {
            internal bool broken = false;

            internal LimbStatus(float maxHealth) : base(maxHealth)
            {
            }
        }
    }
}
