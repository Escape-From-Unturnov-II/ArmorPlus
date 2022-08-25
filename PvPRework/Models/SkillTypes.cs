using SpeedMann.PvPRework.Models.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeedMann.PvPRework.Models
{
    public class SkillTypes
    {
        public enum SkillType
        {
            // page Offense
            Overkill,
            Sharpshooter,
            Dexterity,
            Cardio,
            Exercise,
            Diving,
            Parkour,
            // page Defense
            Sneakybeaky,
            Vitality,
            Immunity,
            Toughness,
            Strength,
            Warmblooded,
            Survival,
            // page Support
            Healing,
            Crafting,
            Outdoors,
            Cooking,
            Fishing,
            Agriculture,
            Mechanic,
            Engineer,
        }

        public static bool tryGetSkillType(DrugEffectType effectType, out SkillType skill)
        {   
            skill = SkillType.Overkill;
            if((int)effectType <= 21)
            {
                skill = (SkillType)effectType;
                return true;
            }
            return false;
        }
        public static bool tryGetIndexes(DrugEffectType type, out byte page, out byte index)
        {
            page = 0;
            index = 0;
            if (tryGetSkillType(type, out SkillType skillType))
            {
                return tryGetIndexes(skillType, out page, out index);
            }
            return false;
        }
        public static bool tryGetIndexes(SkillType type, out byte page, out byte index)
        {
            page = 0;
            index = 0;
            switch (type)
            {
                case SkillType.Overkill:
                case SkillType.Sharpshooter:
                case SkillType.Dexterity:
                case SkillType.Cardio:
                case SkillType.Exercise:
                case SkillType.Diving:
                case SkillType.Parkour:
                    page = 0;
                    index = (byte)type;
                    break;
                case SkillType.Sneakybeaky:
                case SkillType.Vitality:
                case SkillType.Immunity:
                case SkillType.Toughness:
                case SkillType.Strength:
                case SkillType.Warmblooded:
                case SkillType.Survival:
                    page = 1;
                    index = (byte)(type-7);
                    break;
                case SkillType.Healing:
                case SkillType.Crafting:
                case SkillType.Outdoors:
                case SkillType.Cooking:
                case SkillType.Fishing:
                case SkillType.Agriculture:
                case SkillType.Mechanic:
                case SkillType.Engineer:
                    page = 2;
                    index = (byte)(type - 14);
                    break;
                default:
                    return false;
            }
            return true;
        }
    }
}
