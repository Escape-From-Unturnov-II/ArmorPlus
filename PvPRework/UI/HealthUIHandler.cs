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
        private static string HealthUIHead = "Head";
        private static string HealthUIChest = "Chest";
        private static string HealthUIStomach = "Stomach";
        private static string HealthUIRightArm = "RightArm";
        private static string HealthUILeftArm = "LeftArm";
        private static string HealthUIRightLeg = "RightLeg";
        private static string HealthUILeftLeg = "LeftLeg";

        // Colors
        private static string HealthUIDamageBlack = "Black";
        private static string HealthUIDamageRed = "Red";
        private static string HealthUIDamageOrange = "Orange";
        private static string HealthUIDamageYellow = "Yellow";
        private static string HealthUIDamageGreen = "Green";

        // Effects
        private static string HealthUIEffectFracture = "Fracture";
        private static string HealthUIEffectBleeding = "Bleeding";

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
            foreach (BodyPart bodyPart in BodyPart.GetValues(typeof(BodyPart)))
            {
                changeHealthUI(executorID, bodyPart, getDamageColor(status.getHealth(bodyPart), status.getMaxHealth(bodyPart)));
            }
        }
        internal static void setHealthUIVisibility(CSteamID executorID, bool visible)
        {
            EffectControler.setVisibility(visible, HealthUI_Key, HealthUIPanelName, executorID);
        }
        internal static void setHealthEffectVisibility(CSteamID executorID, HealthEffect effect, bool visible)
        {
            EffectControler.setVisibility(visible, HealthUI_Key, getHealthEffectName(effect), executorID);
            Logger.Log($"UI effect: {effect} {visible}");
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
                    return HealthUIDamageGreen;
                case DamageColor.Yellow:
                    return HealthUIDamageYellow;
                case DamageColor.Orange:
                    return HealthUIDamageOrange;
                case DamageColor.Red:
                    return HealthUIDamageRed;
                case DamageColor.Black:
                    return HealthUIDamageBlack;
            }
            return "";
        }
        private static string getBodyPartName(BodyPart bodyPart)
        {
            switch (bodyPart)
            {
                case BodyPart.Head:
                    return HealthUIHead;
                case BodyPart.Chest:
                    return HealthUIChest;
                case BodyPart.Stomach:
                    return HealthUIStomach;
                case BodyPart.ArmRight:
                    return HealthUIRightArm;
                case BodyPart.ArmLeft:
                    return HealthUILeftArm;
                case BodyPart.LegRight:
                    return HealthUIRightLeg;
                case BodyPart.LegLeft:
                    return HealthUILeftLeg;
            }
            return "";
        }
        private static string getHealthEffectName(HealthEffect effect)
        {
            switch (effect)
            {
                case HealthEffect.Bleeding:
                    return HealthUIEffectBleeding;
                case HealthEffect.Fracture:
                    return HealthUIEffectFracture;
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
