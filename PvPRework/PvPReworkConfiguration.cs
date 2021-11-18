using Rocket.API;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace PvPRework
{
    public class PVPReworkConfiguration : IRocketPluginConfiguration
    {
        [XmlArrayItem(ElementName = "ArmoClasses")]
        public List<ArmorClass> armoClasses;
        [XmlArrayItem(ElementName = "BoneBreakingChance")]
        public List<BulletLimbDamageChance> boneBreakingChances;
        [XmlArrayItem(ElementName = "Vest")]
        public List<KeyValueElement<ushort, float>> vestsProtectingArms;
        [XmlArrayItem(ElementName = "Vest")]
        public List<KeyValueElement<ushort, float>> vestsProtectingLegs;
        [XmlArrayItem(ElementName = "Gun")]
        public List<KeyValueElement<ushort, float>> gunPenValues;

        public bool Debug; //to display debug information on server console
        public bool BreakLegs; //if bullets should be able to break legs
        public bool BetterArmor; //if better armor calculations should be used (requred for armorClasses and vestsProtectArms / Pants)
        public bool HasDuribility; //set to true if server has durability
        public bool UseArmorClasses; //defines if armor classes should be used

        public float ArmorDamageMultiplierOnPen; //multiplier used for damage done to armor when penetrating armor

        public void LoadDefaults()
        {
            Debug = true;
            HasDuribility = true;
            BreakLegs = true;
            BetterArmor = true;
            UseArmorClasses = true;
            ArmorDamageMultiplierOnPen = 0.5f;

            boneBreakingChances = new List<BulletLimbDamageChance>{
                new BulletLimbDamageChance{ Limb = "LEG", BreakChanceMin = 10, BreakChanceMax = 95, BreakChanceDamageMin = 10, BreakChanceDamageMax = 50},
                new BulletLimbDamageChance{ Limb = "FOOT", BreakChanceMin = 10, BreakChanceMax = 95, BreakChanceDamageMin = 10, BreakChanceDamageMax = 50},
                new BulletLimbDamageChance{ Limb = "ARM", BreakChanceMin = 0, BreakChanceMax = 0, BreakChanceDamageMin = 0, BreakChanceDamageMax = 0},
                new BulletLimbDamageChance{ Limb = "HAND", BreakChanceMin = 0, BreakChanceMax = 0, BreakChanceDamageMin = 0, BreakChanceDamageMax = 0},
                new BulletLimbDamageChance{ Limb = "SKULL", BreakChanceMin = 0, BreakChanceMax = 0, BreakChanceDamageMin = 0, BreakChanceDamageMax = 0},
                new BulletLimbDamageChance{ Limb = "SPINE", BreakChanceMin = 0, BreakChanceMax = 0, BreakChanceDamageMin = 0, BreakChanceDamageMax = 0}
            };
            armoClasses = new List<ArmorClass>{
                new ArmorClass{
                    Tier=1, Armor = 0.95f,
                    PercentForNormalDamage = 20, PercentForMaxDamage = 90,
                    DamageMultiplierMin = 0.8f,    DamageMultiplierNormal = 1f,
                    DamageToDamageArmorMin = 0, DamageToDamageArmorMax = 40,
                    MinArmorDamage = 1, MaxArmorDamage = 4,
                    StopDamageMulti = 0.4f, PenLossMulti = 0},

                new ArmorClass{
                    Tier=2f, Armor = 0.9f,
                    PercentForNormalDamage = 20, PercentForMaxDamage = 90,
                    DamageMultiplierMin = 0.8f,    DamageMultiplierNormal = 1f,
                    DamageToDamageArmorMin = 0, DamageToDamageArmorMax = 40,
                    MinArmorDamage = 1, MaxArmorDamage = 4,
                    StopDamageMulti = 0.2f, PenLossMulti = 0.1f},

                new ArmorClass{
                    Tier=3f, Armor = 0.8f,
                    PercentForNormalDamage = 20, PercentForMaxDamage = 90,
                    DamageMultiplierMin = 0.4f,    DamageMultiplierNormal = 0.8f,
                    DamageToDamageArmorMin = 15, DamageToDamageArmorMax = 60,
                    MinArmorDamage = 1, MaxArmorDamage = 4,
                    StopDamageMulti = 0.1f, PenLossMulti = 0.2f},

                new ArmorClass{
                    Tier=3.5f, Armor = 0.7f,
                    PercentForNormalDamage = 20, PercentForMaxDamage = 90,
                    DamageMultiplierMin = 0.4f,    DamageMultiplierNormal = 0.8f,
                    DamageToDamageArmorMin = 15, DamageToDamageArmorMax = 60,
                    MinArmorDamage = 1, MaxArmorDamage = 4,
                    StopDamageMulti = 0.05f, PenLossMulti = 0.3f},

                new ArmorClass{
                    Tier=4f, Armor = 0.65f,
                    PercentForNormalDamage = 20, PercentForMaxDamage = 90,
                    DamageMultiplierMin = 0.4f,    DamageMultiplierNormal = 0.8f,
                    DamageToDamageArmorMin = 15, DamageToDamageArmorMax = 80,
                    MinArmorDamage = 1, MaxArmorDamage = 4,
                    StopDamageMulti = 0.02f, PenLossMulti = 0.35f},
            };

            vestsProtectingArms = new List<KeyValueElement<ushort, float>> {
                new KeyValueElement<ushort, float> { Key = 1169, Value = 0.7f }
            };

            vestsProtectingLegs = new List<KeyValueElement<ushort, float>> {
                new KeyValueElement<ushort, float> { Key = 310, Value = 0.7f }
            };

            gunPenValues = new List<KeyValueElement<ushort, float>> { 

                // Civ Pistols
                new KeyValueElement<ushort, float> {Key = 107, Value = 17 },    //Ace
                new KeyValueElement<ushort, float> {Key = 99, Value = 12.5f },   //Cobra
                new KeyValueElement<ushort, float> {Key = 97, Value = 12.5f },   //Colt
                new KeyValueElement<ushort, float> {Key = 1039, Value = 12.5f }, //Kryzkarek
                new KeyValueElement<ushort, float> {Key = 1476, Value = 12.5f }, //Luger

                // Civ Guns
                new KeyValueElement<ushort, float> {Key = 109, Value = 27 },    //Hawkhound
                new KeyValueElement<ushort, float> {Key = 479, Value = 25 },    //Rifle_Birch
                new KeyValueElement<ushort, float> {Key = 474, Value = 25 },    //Rifle_Maple
                new KeyValueElement<ushort, float> {Key = 480, Value = 25 },    //Rifle_Pine
                new KeyValueElement<ushort, float> {Key = 101, Value = 27 },    //Schofield
                new KeyValueElement<ushort, float> {Key = 484, Value = 15.5f },    //Sportshot
                new KeyValueElement<ushort, float> {Key = 1027, Value = 10.5f}, //Viper

                // LC Ranger Pistol
                new KeyValueElement<ushort, float> {Key = 1360, Value = 10.5f},     //Teklowvka

                // LC Ranger Guns
                new KeyValueElement<ushort, float> {Key = 1362, Value = 18 },       //Augewehr
                new KeyValueElement<ushort, float> {Key = 1369, Value = 15.5f },    //Bulldog
                new KeyValueElement<ushort, float> {Key = 1379, Value = 15.5f },    //Card
                new KeyValueElement<ushort, float> {Key = 1364, Value = 19 },       //Fury
                new KeyValueElement<ushort, float> {Key = 1375, Value = 19 },       //Fusilaut
                new KeyValueElement<ushort, float> {Key = 1477, Value = 15.5f },    //MP40
                new KeyValueElement<ushort, float> {Key = 1377, Value = 18 },       //Nightraider
                new KeyValueElement<ushort, float> {Key = 126, Value = 19 },        //Nykorev
                new KeyValueElement<ushort, float> {Key = 129, Value = 22 },        //Snayperskya
                new KeyValueElement<ushort, float> {Key = 1041, Value = 15.5f },    //Yuri
                new KeyValueElement<ushort, float> {Key = 122, Value = 18 },        //Zubeknakov       
                
                // HC Ranger
                new KeyValueElement<ushort, float> {Key = 1382, Value = 35 },   //Ekho
                new KeyValueElement<ushort, float> {Key = 1000, Value = 30 },   //Matamorez

                // LC Mil Pistol
                new KeyValueElement<ushort, float> {Key = 1021, Value = 10.5f}, //Avenger
                
                // LC Mil Guns
                new KeyValueElement<ushort, float> {Key = 116, Value = 17.5f }, //Honeybadger
                new KeyValueElement<ushort, float> {Key = 4, Value = 18 },      //Eaglefire
                new KeyValueElement<ushort, float> {Key = 1481, Value = 15.5f },//Empire
                new KeyValueElement<ushort, float> {Key = 1037, Value = 18.5f}, //Heartbreaker
                new KeyValueElement<ushort, float> {Key = 363, Value = 18 },    //Maplestrike
                new KeyValueElement<ushort, float> {Key = 1024, Value = 15.5f },//Peacemaker
                new KeyValueElement<ushort, float> {Key = 1018, Value = 22 },   //Sabertooth
                new KeyValueElement<ushort, float> {Key = 1447, Value = 15.5f },//Scalar
                new KeyValueElement<ushort, float> {Key = 1488, Value = 19 },   //Swissgewehr                

                // HC Mil Pistol
                new KeyValueElement<ushort, float> {Key = 488, Value = 18 },    //Desert_Falcon

                // HC Mil Guns
                new KeyValueElement<ushort, float> {Key = 297, Value = 35 },    //Grizzly
                new KeyValueElement<ushort, float> {Key = 132, Value = 27 },    //Dragonfang
                new KeyValueElement<ushort, float> {Key = 18, Value = 35 },     //Timberwolf

                // Shotguns
                new KeyValueElement<ushort, float> {Key = 112, Value = 10.5f },     //Bluntforce
                new KeyValueElement<ushort, float> {Key = 1484, Value = 13.5f },   //Bane
                new KeyValueElement<ushort, float> {Key = 1480, Value = 10.5f },    //Determinator
                new KeyValueElement<ushort, float> {Key = 380, Value = 13.5f },    //Masterkey
                new KeyValueElement<ushort, float> {Key = 1436, Value = 13.5f },   //Quadbarrel
                new KeyValueElement<ushort, float> {Key = 1143, Value = 13 },   //Sawed_Off
                new KeyValueElement<ushort, float> {Key = 1366, Value = 10.5f },    //Vonya

                // Other
                new KeyValueElement<ushort, float> {Key = 355, Value = 16 },    //Bow_Birch
                new KeyValueElement<ushort, float> {Key = 353, Value = 16 },    //Bow_Maple
                new KeyValueElement<ushort, float> {Key = 356, Value = 16 },    //Bow_Pine
                new KeyValueElement<ushort, float> {Key = 357, Value = 18 },    //Bow_Compound
                new KeyValueElement<ushort, float> {Key = 346, Value = 18 },    //Crossbow
                new KeyValueElement<ushort, float> {Key = 1165, Value = 3 },    //Nailgun
                new KeyValueElement<ushort, float> {Key = 1337, Value = 0 },    //Paintballgun
                new KeyValueElement<ushort, float> {Key = 300, Value = 27 },    //Shadowstalker
                new KeyValueElement<ushort, float> {Key = 1441, Value = 27 },   //ShadowstalkerMk2

                // HMG
                new KeyValueElement<ushort, float> {Key = 1394, Value = 25 },    //HMG
                new KeyValueElement<ushort, float> {Key = 1471, Value = 25 },    //HMG_Fighter_Jet
            };

        }
    }
}
