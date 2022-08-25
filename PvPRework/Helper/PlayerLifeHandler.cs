using Rocket.Core.Logging;
using SDG.Unturned;
using SpeedMann.PvPRework.Controllers;
using SpeedMann.PvPRework.Models;
using SpeedMann.PvPRework.Models.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace SpeedMann.PvPRework.Helper
{
    internal class PlayerLifeHandler
    {
        private bool dead = true;
        private byte oldStamina = 100;
        private byte oldHealth = 100;
        private byte oldFood = 100;
        private byte oldWater = 100;
        private byte oldInfection = 100;

        private Player _player;

        private Dictionary<DrugEffectType, EffectState> drugEffectStates = new Dictionary<DrugEffectType, EffectState>();
        private List<Timer> stopEffectTimers = new List<Timer>();
        private List<Timer> delayEffectTimers = new List<Timer>();
        internal PlayerLifeHandler(Player player, byte stamina, byte health, byte food, byte water, byte infection)
        {
            oldStamina = stamina;
            oldHealth = health;
            oldFood = food;
            oldWater = water;
            oldInfection = infection;
            dead = false;
            _player = player;

            player.life.onStaminaUpdated += OnStaminaUpdated;
        }

        private void startDrugEffectsInner(List<MedicalEffect> effects)
        {
            Dictionary<uint, List<MedicalEffect>> effectsByEffectDuration = new Dictionary<uint, List<MedicalEffect>>();
            foreach (MedicalEffect effect in effects)
            {
                if (effectsByEffectDuration.TryGetValue(effect.EffectDuration, out List<MedicalEffect> entry))
                {
                    entry.Add(effect);
                }
                else
                {
                    effectsByEffectDuration.Add(effect.EffectDuration, new List<MedicalEffect> { effect });
                }
                updateDrugEffect(effect.Type, effect.Value);
            }

            foreach (KeyValuePair<uint, List<MedicalEffect>> entry in effectsByEffectDuration)
            {
                Timer timer = getEffectTimer(entry.Key);
                timer.Elapsed += (object sender, ElapsedEventArgs e) => {
                    foreach(MedicalEffect effect in entry.Value)
                    {
                        updateDrugEffect(effect.Type, -effect.Value);
                    }
                    stopEffectTimers.Remove(timer);
                };
                timer.Start();
                stopEffectTimers.Add(timer);
            }
        }
        private void updateDrugEffect(DrugEffectType type, int modifier)
        {
            
            if (drugEffectStates.TryGetValue(type, out EffectState state))
            {
                int newValue = modifier;
                if (DrugEffectControler.tryGetEffectLimits(type, out int min, out int max))
                {
                    newValue = getNewLimitedModifier(state.value, modifier, min, max);
                    if (newValue == 0)
                    {
                        drugEffectStates.Remove(type);
                        return;
                    }
                }
                if (SkillTypes.tryGetIndexes(type, out byte page, out byte index))
                {
                    updatePlayerSkill(page, index, (short)modifier);
                }
                state.value = newValue;
            }
            else
            {
                if(SkillTypes.tryGetIndexes(type, out byte page, out byte index))
                {
                    updatePlayerSkill(page, index, (short)modifier);
                }
                drugEffectStates.Add(type, new EffectState(modifier));
                Logger.Log($"New drug effect {type} , {modifier}");
            }
        }
        internal void startDrugEffects(List<MedicalEffect> effects)
        {
            Dictionary<uint, List<MedicalEffect>> delayedEffects = new Dictionary<uint, List<MedicalEffect>>();
            List<MedicalEffect> instantEffects = new List<MedicalEffect>();
            foreach (MedicalEffect effect in effects)
            {
                if (effect.StartEffectDelay == 0)
                {
                    instantEffects.Add(effect);
                }
                else
                {
                    if (delayedEffects.TryGetValue(effect.StartEffectDelay, out List<MedicalEffect> entry))
                    {
                        entry.Add(effect);
                    }
                    else
                    {
                        delayedEffects.Add(effect.StartEffectDelay, new List<MedicalEffect> { effect });
                    }
                }
            }
            startDrugEffectsInner(instantEffects);

            foreach (KeyValuePair<uint, List<MedicalEffect>> entry in delayedEffects)
            {
                Timer timer = getEffectTimer(entry.Key);
                timer.Elapsed += (object sender, ElapsedEventArgs e) => {
                    foreach (MedicalEffect effect in entry.Value)
                    {
                        updateDrugEffect(effect.Type, effect.Value);
                    }
                    delayEffectTimers.Remove(timer);
                };
                timer.Start();
                delayEffectTimers.Add(timer);
            }
        }

        internal void OnStaminaUpdated(byte newStamina)
        {
            if (drugEffectStates.TryGetValue(DrugEffectType.StaminaRegen, out EffectState state))
            {
                state.updated = !state.updated;
                if (!state.updated)
                {
                    if(oldStamina < newStamina)
                    {
                        if (state.value < 0)
                        {
                            _player.life.askTire((byte)(state.value * -1));
                        }
                        else
                        {
                            _player.life.askRest((byte)state.value);
                        }
                    }
                }
            }
            oldStamina = newStamina;
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
        private void updatePlayerSkill(byte type, byte index, short levelChange)
        {
            Logger.Log($"Updated skill {type}, {levelChange}");
            byte newLevel = (byte)(_player.skills.skills[type][index].level + levelChange);
            UnturnedPrivateFields.trySendSingleSkillLevel(_player.skills, type, index, newLevel);
        }
        //PlayerMovement: ReceivePluginGravityMultiplier ReceivePluginJumpMultiplier ReceivePluginSpeedMultiplier
        internal void removeAllEffects()
        {
            foreach (Timer timer in stopEffectTimers)
            {
                timer.Stop();
            }
            stopEffectTimers.Clear();
            foreach (Timer timer in delayEffectTimers)
            {
                timer.Stop();
            }
            delayEffectTimers.Clear();
            foreach (KeyValuePair<DrugEffectType, EffectState> entry in drugEffectStates)
            {
                updateDrugEffect(entry.Key, entry.Value.value);
            }
        }
        private int getNewLimitedModifier(int value, int modifier, int min = 0, int max = 100)
        {
            int newValue = value - modifier;
            if (newValue > max || newValue < 0)
            {
                return 0;
            }
            return modifier;
        }
        private Timer getEffectTimer(uint time)
        {
            Timer effectTimer = new Timer(time * 1000);
            effectTimer.AutoReset = false;
            return effectTimer;
        }
        class EffectState
        {
            internal int value;
            internal bool updated;

            internal EffectState(int value)
            {
                this.value = value;
                updated = false;
            }
        }
    }
}
