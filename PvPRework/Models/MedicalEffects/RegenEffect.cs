using SDG.Unturned;
using SpeedMann.PvPRework.Models.Config;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Formatters;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Logger = Rocket.Core.Logging.Logger;

namespace SpeedMann.PvPRework.Models.MedicalEffects
{
    internal class RegenEffect : MedicalEffect
    {
        private bool regenActive = false;
        private int change;
        private float interval;
        private IEnumerator coroutine;
        private DrugEffectType type;

        internal RegenEffect(Player player, float effectDuration, float effectDelay, int change, float interval, DrugEffectType type) : base(player, effectDuration, effectDelay)
        {
            this.type = type;
            this.change = change > 0 ? change : 1;
            this.interval = interval > 0 ? interval : 1;
        }

        protected override void startInner()
        {
            regenActive = true;
            coroutine = regen();
            player.StartCoroutine(coroutine);
        }

        protected override void stopInner()
        {
            regenActive = false;
        }

        private IEnumerator regen()
        {
            while (regenActive && player.life != null)
            {
                switch (type)
                {
                    case DrugEffectType.StaminaRegen:
                        player.life.serverModifyStamina(change);
                        break;
                    case DrugEffectType.HealthRegen:
                        player.life.serverModifyHealth(change);
                        break;
                    case DrugEffectType.FoodRegen:
                        player.life.serverModifyFood(change);
                        break;
                    case DrugEffectType.WaterRegen:
                        player.life.serverModifyWater(change);
                        break;
                    default:
                        regenActive = false;
                        Logger.LogError($"Regen effect {type} is not supported!");
                        break;
                }
                
                yield return new WaitForSecondsRealtime(interval);
            }
        }
    }
}
