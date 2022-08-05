using Rocket.Core.Logging;
using SpeedMann.PvPRework.Models;
using SpeedMann.PvPRework.Models.UI;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SpeedMann.PvPRework.Controllers.HealthManager;

namespace SpeedMann.PvPRework.UI
{
    internal class HealthUIHandler
    {
        private static ushort HealthUI_ID = 52320;
        private static short  HealthUI_Key = 5230;
        private static string HealthUIPanelName = "UnturnedHealthPanel";

        // Body Parts
        private static string UINameHead = "Head";
        private static string UINameChest = "Chest";
        private static string UINameStomach = "Stomach";
        private static string UINameRightArm = "RightArm";
        private static string UINameLeftArm = "LeftArm";
        private static string UINameRightLeg = "RightLeg";
        private static string UINameLeftLeg = "LeftLeg";

        // Colors
        private static string UINameDamageBlack = "Black";
        private static string UINameDamageRed = "Red";
        private static string UINameDamageOrange = "Orange";
        private static string UINameDamageYellow = "Yellow";
        private static string UINameDamageGreen = "Green";

        // Effects
        private static string UINameEffectFracture = "Fracture";
        private static string UINameFractureCounter = "FractureCounter";
        private static string UINameEffectBleeding = "Bleeding";
        private static string UINameBleedingCounter = "BleedingCounter";

        private static Dictionary<CSteamID, HeathUIState> uIStates = new Dictionary<CSteamID, HeathUIState>();

        internal static void spawnHealthUI(CSteamID executorID)
        {
            EffectControler.spawnUI(HealthUI_ID, HealthUI_Key, executorID);
            if (!uIStates.ContainsKey(executorID))
            {
                uIStates.Add(executorID, new HeathUIState());
            }
            else
            {
                uIStates[executorID] = new HeathUIState();
            }
        }
        internal static void updateHealthUI(CSteamID executorID, HealthStatus status)
        {
            int brokenLimbCount = 0;
            foreach (BodyPart bodyPart in BodyPart.GetValues(typeof(BodyPart)))
            {
                if(status.isBroken(bodyPart)){
                    brokenLimbCount++;
                }
                changeHealthUI(executorID, bodyPart, getDamageColor(status.getHealth(bodyPart), status.getMaxHealth(bodyPart)));
            }

            setHealthEffectVisibility(executorID, HealthEffect.Fracture, brokenLimbCount);

        }
        internal static void setHealthUIVisibility(CSteamID executorID, bool visible)
        {
            EffectControler.setVisibility(visible, HealthUI_Key, HealthUIPanelName, executorID);
        }
        internal static void setHealthEffectVisibility(CSteamID executorID, HealthEffect effect, int count)
        {
            EffectControler.setVisibility(count < 0, HealthUI_Key, getHealthEffectName(effect, out string counterName), executorID);
            EffectControler.setUIValue(HealthUI_Key, executorID, counterName, count.ToString());
            Logger.Log($"UI effect: {effect} {count}");
        }
        
        internal static void changeHealthUI(CSteamID executorID, BodyPart bodyPart, DamageColor newDamageColor)
        {
            if (!uIStates.TryGetValue(executorID, out HeathUIState state)) return;
            if (!state.damageColors.TryGetValue(bodyPart, out DamageColor oldDamageColor))
            {
                Logger.LogError($"Change health could not find {bodyPart} in state");
                return;
            }

            if (oldDamageColor == newDamageColor) return;
            Logger.Log($"HealthUI Update: {bodyPart} from {oldDamageColor} to {newDamageColor}");
            EffectControler.setVisibility(false, HealthUI_Key, getBodyPartName(bodyPart) + getDamageColorName(oldDamageColor), executorID);
            EffectControler.setVisibility(true, HealthUI_Key, getBodyPartName(bodyPart) + getDamageColorName(newDamageColor), executorID);
            state.damageColors[bodyPart] = newDamageColor;
        }
        private static DamageColor getDamageColor(int health, int maxHealth)
        {
            if (maxHealth <= 0 || health == 0) return DamageColor.Black;
            float percentage = health * 100 / maxHealth;
            
            if (percentage >= 75) return DamageColor.Green;
            if (percentage >= 50) return DamageColor.Yellow;
            if (percentage >= 25) return DamageColor.Orange;

            return DamageColor.Red;
        }
        private static string getDamageColorName(DamageColor color)
        {
            switch (color)
            {
                case DamageColor.Green:
                    return UINameDamageGreen;
                case DamageColor.Yellow:
                    return UINameDamageYellow;
                case DamageColor.Orange:
                    return UINameDamageOrange;
                case DamageColor.Red:
                    return UINameDamageRed;
                case DamageColor.Black:
                    return UINameDamageBlack;
            }
            return "";
        }
        private static string getBodyPartName(BodyPart bodyPart)
        {
            switch (bodyPart)
            {
                case BodyPart.Head:
                    return UINameHead;
                case BodyPart.Chest:
                    return UINameChest;
                case BodyPart.Stomach:
                    return UINameStomach;
                case BodyPart.ArmRight:
                    return UINameRightArm;
                case BodyPart.ArmLeft:
                    return UINameLeftArm;
                case BodyPart.LegRight:
                    return UINameRightLeg;
                case BodyPart.LegLeft:
                    return UINameLeftLeg;
            }
            return "";
        }
        private static string getHealthEffectName(HealthEffect effect, out string counterName)
        {
            counterName = "";
            switch (effect)
            {
                case HealthEffect.Bleeding:
                    counterName = UINameBleedingCounter;
                    return UINameEffectBleeding;
                case HealthEffect.Fracture:
                    counterName = UINameBleedingCounter;
                    return UINameEffectFracture;
            }
            return "";
        }
        public enum DamageColor
        {
            Green,
            Yellow,
            Orange,
            Red,
            Black,
        }

        public enum HealthEffect
        {
            Fracture,
            Bleeding,
        }
    }
}
