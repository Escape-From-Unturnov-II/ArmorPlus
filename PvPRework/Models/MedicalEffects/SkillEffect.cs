using SDG.Unturned;
using SpeedMann.PvPRework.Helper;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static SpeedMann.PvPRework.Models.SkillTypes;
using Logger = Rocket.Core.Logging.Logger;

namespace SpeedMann.PvPRework.Models.MedicalEffects
{
    internal class SkillEffect : MedicalEffect
    {
        int change = 0;
        byte page, index;
        internal SkillEffect(Player player, float effectDuration, float effectDelay, SkillType skill, int change) : base(player, effectDuration, effectDelay)
        {
            if (!tryGetIndexes(skill, out page, out index))
            {
                Logger.LogError($"Skill {skill} is invalid");
            }
            else
            {
                this.change = change;
            }
        }

        protected override void startInner()
        {
            if(change == 0)
            {
                return;
            }
            updatePlayerSkill(page, index, change);
        }

        protected override void stopInner()
        {
            if (change == 0)
            {
                return;
            }
            updatePlayerSkill(page, index, -change);
        }
        private void updatePlayerSkill(byte type, byte index, int levelChange)
        {
            byte newLevel = (byte)(player.skills.skills[type][index].level + levelChange);
            UnturnedPrivateFields.trySendSingleSkillLevel(player.skills, type, index, newLevel);
        }
    }
}
