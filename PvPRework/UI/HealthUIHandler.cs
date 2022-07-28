using Rocket.Core.Logging;
using SpeedMann.PvPRework.Models.UI;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeedMann.PvPRework.UI
{
    internal class HealthUIHandler
    {
        private static ushort HealthUI_ID = 52320;
        private static short  HealthUI_Key = 5230;
        private static string HealthUIPanelName = "UnturnedHealthPanel";

        private static string HealthUIHead = "Head";
        private static string HealthUIChest = "Chest";
        private static string HealthUIStomach = "Stomach";
        private static string HealthUIRightArm = "RightArm";
        private static string HealthUILeftArm = "LeftArm";
        private static string HealthUIRightLeg = "RightLeg";
        private static string HealthUILeftLeg = "LeftLeg";

        private static string HealthUIDamageBlack = "Black";
        private static string HealthUIDamageRed = "Red";
        private static string HealthUIDamageOrange = "Orange";
        private static string HealthUIDamageYellow = "Yellow";
        private static string HealthUIDamageGreen = "Green";


        private static Dictionary<CSteamID, HeathUIState> uIStates = new Dictionary<CSteamID, HeathUIState>();

        internal static void spawnHealthUI(CSteamID executorID)
        {
            EffectControler.spawnUI(HealthUI_ID, HealthUI_Key, executorID);
            if (uIStates.ContainsKey(executorID))
            {
                uIStates.Add(executorID, new HeathUIState());
            }
        }
        internal static void setHealthUIVisibility(CSteamID executorID, bool visible)
        {
            EffectControler.setVisibility(visible, HealthUI_Key, HealthUIPanelName, executorID);
        }
        internal static void changeHealthUI(CSteamID executorID, BodyPart bodyPart, DamageColor newDamageColor)
        {
            if (!uIStates.TryGetValue(executorID, out HeathUIState state)) return;
            if (!state.damageColors.TryGetValue(bodyPart, out DamageColor oldDamageColor))
            {
                Logger.LogError($"Change health could not find {bodyPart} in state");
                return;
            }

            EffectControler.setVisibility(false, HealthUI_Key, getBodyPartName(bodyPart) + getDamageColorName(oldDamageColor), executorID);
            EffectControler.setVisibility(true, HealthUI_Key, getBodyPartName(bodyPart) + getDamageColorName(newDamageColor), executorID);
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
        public enum BodyPart
        {
            Head,
            Chest,
            Stomach,
            ArmRight,
            ArmLeft,
            LegRight,
            LegLeft,
        }
        public enum DamageColor
        {
            Green,
            Yellow,
            Orange,
            Red,
            Black,
        }
    }
}
