using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using UnityEngine;
using UnityEngine.Rendering;
using Logger = Rocket.Core.Logging.Logger;

namespace SpeedMann.PvPRework.Models
{
    public abstract class MedicalEffect
    {
        public delegate void EffectRanOut();
        public event EffectRanOut OnEffectRanOut;

        private bool active = false;
        private IEnumerator coroutine;
        private float duration;
        private float delay;
        protected Player player;

        public MedicalEffect(Player player, float effectDuration, float effectDelay)
        {
            this.player = player;
            duration = effectDuration;
            delay = effectDelay;
        }
        public bool isActive()
        {
            return active;
        }
        public void startEffect()
        {
            coroutine = delay > 0 ? startWithDelay(delay) : start();
            player.StartCoroutine(coroutine);
            active = true;
        }
        public void stopEffect()
        {
            player.StopCoroutine(coroutine);
            stopInner();
            active = false;
            OnEffectRanOut?.Invoke();
        }
        protected abstract void startInner();
        protected abstract void stopInner();
        private IEnumerator start()
        {
            startInner();
            yield return new WaitForSecondsRealtime(duration);
            stopEffect();
        }
        private IEnumerator startWithDelay(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            coroutine = start();
        }
    }
}
