using HarmonyLib;
using Rocket.Core.Logging;
using Rocket.Unturned.Player;
using SDG.Unturned;
using SpeedMann.PvPRework.Controllers;
using SpeedMann.PvPRework.Models;
using SpeedMann.PvPRework.Models.Config;
using SpeedMann.PvPRework.Models.MedicalEffects;
using SpeedMann.PvPRework.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using UnityEngine;
using static SDG.Provider.SteamGetInventoryResponse;
using Logger = Rocket.Core.Logging.Logger;

namespace SpeedMann.PvPRework.Helper
{
    internal class DrugEffectHandler
    {

        private Player _player;

        private Dictionary<ushort, List<MedicalEffect>> activeMeds = new Dictionary<ushort, List<MedicalEffect>>();
        internal DrugEffectHandler(Player player)
        {
            _player = player;
        }

        internal void startDrugEffects(ushort itemId, List<MedicalEffectConfig> effectConfigs)
        {
            if (activeMeds.ContainsKey(itemId))
            {
                stopMed(itemId);
            }

            List<MedicalEffect> effects = new List<MedicalEffect>(effectConfigs.Count);
            foreach(var effectConfig in effectConfigs)
            {
               
                if(!tryGetEffect(effectConfig, out var effect))
                {
                    Logger.LogError($"Invalid effect {effectConfig.Type} in item {itemId}");
                    continue;
                }
                effect.OnEffectRanOut += () =>
                {
                    checkMedActive(itemId);
                };
                effects.Add(effect);
            }

            foreach(var effect in effects)
            {
                effect.startEffect();
            }
            activeMeds.Add(itemId, effects);

            UnturnedPlayer uPlayer = UnturnedPlayer.FromPlayer(_player);
            Logger.Log($"{uPlayer.CSteamID} used {itemId} and got\n{String.Join("\n", effectConfigs.Select(x => $"{x.Type}: Value {x.Value}, Duration {x.EffectDuration}, Delay {x.EffectStartDelay}").ToArray())}\n");
        }


        internal void stopAllMeds()
        {
            List<ushort> ids = new List<ushort>();
            foreach(var itemId in activeMeds.Keys)
            {
                ids.Add(itemId);
            }
            foreach(var id in ids)
            {
                stopMed(id);
            }
        }

        private void stopMed(ushort itemId)
        {
            activeMeds.TryGetValue(itemId, out var effects);
            foreach (var effect in effects)
            {
                effect.stopEffect();
            }
            //TODO: prevent med active checks
            activeMeds.Remove(itemId);
        }
        private void checkMedActive(ushort itemId)
        {
            if (!activeMeds.TryGetValue(itemId, out List<MedicalEffect> effects))
                return;

            foreach (var effect in effects)
            {
                if (effect.isActive())
                    return;
            }
            activeMeds.Remove(itemId);
        }

        private bool tryGetEffect(MedicalEffectConfig config, out MedicalEffect effect)
        {
            effect = null;

            switch (config.Type) 
            {
                case DrugEffectType.NoFracture:
                case DrugEffectType.NoBleeding:
                    effect = new PreventiveEffect(_player, config.EffectDuration, config.EffectStartDelay, config.Type);
                    return true;
                case DrugEffectType.StaminaRegen:
                case DrugEffectType.HealthRegen:
                case DrugEffectType.FoodRegen:
                case DrugEffectType.WaterRegen:
                    effect = new RegenEffect(_player, config.EffectDuration, config.EffectStartDelay, config.Value, config.Interval, config.Type);
                    return true;
                case DrugEffectType.Overkill:
                case DrugEffectType.Sharpshooter:
                case DrugEffectType.Dexterity:
                case DrugEffectType.Cardio:
                case DrugEffectType.Exercise:
                case DrugEffectType.Diving:
                case DrugEffectType.Parkour:

                case DrugEffectType.Sneakybeaky:
                case DrugEffectType.Vitality:
                case DrugEffectType.Immunity:
                case DrugEffectType.Toughness:
                case DrugEffectType.Strength:
                case DrugEffectType.Warmblooded:
                case DrugEffectType.Survival:

                case DrugEffectType.Healing:
                case DrugEffectType.Crafting:
                case DrugEffectType.Outdoors:
                case DrugEffectType.Cooking:
                case DrugEffectType.Fishing:
                case DrugEffectType.Agriculture:
                case DrugEffectType.Mechanic:
                case DrugEffectType.Engineer:
                    if (!SkillTypes.tryGetSkillType(config.Type, out SkillTypes.SkillType skill))
                    {
                        Logger.LogError($"{config.Type} is not a valid skill type");
                        break;
                    }
                    effect = new SkillEffect(_player, config.EffectDuration, config.EffectStartDelay, skill, config.Value);
                    return true;
            }
            return false;
        }
        
        private void updatePlayerJump(float multiplier)
        {
            _player.movement.sendPluginJumpMultiplier(multiplier);
        }
        private void updatePlayerGravity(float multiplier)
        {
            _player.movement.sendPluginGravityMultiplier(multiplier);
        }
        private void updatePlayerSpeed(float multiplier)
        {
            _player.movement.sendPluginSpeedMultiplier(multiplier);
        }
    }
}
