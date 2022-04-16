using Rocket.API;
using Rocket.Core.Logging;
using SDG.Unturned;
using SpeedMann.PvPRework.Models.Config;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace SpeedMann.PvPRework
{
    public class PVPReworkConfiguration : IRocketPluginConfiguration
    {
        #region Old
        [ObsoleteAttribute("This property is obsolete. Use BetterArmorConfig.Enabled instead.", false)]
        [XmlElement(ElementName = "BetterArmor")]
        public bool OldBetterArmor;
        [ObsoleteAttribute("This property is obsolete. Use BetterArmorConfig.UseArmorClasses instead.", false)]
        [XmlElement(ElementName = "UseArmorClasses")]
        public bool OldUseArmorClasses;
        [ObsoleteAttribute("This property is obsolete. Use BetterArmorConfig.ArmorDamageMultiplierOnPen instead.", false)]
        [XmlElement(ElementName = "ArmorDamageMultiplierOnPen")]
        public float OldArmorDamageMultiplierOnPen;
        [ObsoleteAttribute("This property is obsolete. Use BetterArmorConfig.PenDamgeDelta instead.", false)]
        [XmlElement(ElementName = "PenDamgeDelta")]
        public float OldPenDamgeDelta;

        [XmlArrayItem(ElementName = "Vest")]
        public List<KeyValueElement<ushort, float>> vestsProtectingArms;
        [XmlArrayItem(ElementName = "Vest")]
        public List<KeyValueElement<ushort, float>> vestsProtectingLegs;
        [XmlArrayItem(ElementName = "Gun")]
        public List<KeyValueElement<ushort, float>> gunPenValues;

        [XmlArrayItem(ElementName = "ArmoClass")]
        public List<ArmorClass> armorClasses;
        [XmlArrayItem(ElementName = "BoneBreakingChance")]
        public List<BulletLimbDamageChance> boneBreakingChances;
        #endregion

        private static bool useVanillaDefaults = false;
        public string Version; //auto updating Version Number
        public bool Debug; //to display debug information on server console
        public bool DisableCosmetics;
        public bool BreakLegs; //if bullets should be able to break legs
        public bool UseNotificationUI = true;
        public short NotificationEffectKey = 5230;

        public KillFeed KillFeed;
        public BetterArmorConfig BetterArmor;
        public MovementExtension MovementExtension;

        public List<ArmorClass> ArmorClasses;
        public List<BulletLimbDamageChance> BoneBreakingChances;
        public List<HatExtension> HatExtensions;
        public List<MaskExtension> MaskExtensions;
        public List<GlassesExtension> GlassesExtensions;
        public List<VestExtension> VestExtensions;
        public List<GunExtension> GunExtensions;
        public List<Caliber> BulletCalibers; 
        [XmlArrayItem(ElementName = "HelmetCycle")]
        public List<List<ItemExtension>> CyclableHelmets;
        [XmlArrayItem(ElementName = "SightCycle")]
        public List<List<ItemExtension>> CyclableSights;

        public void LoadDefaults()
        {
            Version = PvPRework.PluginVersion;
            Debug = true;
            DisableCosmetics = true;
            KillFeed = new KillFeed
            {
                Enabled = true,
                MessageColor = "yellow",
                UseCustomUI = false,
                UI_ID = 52313,
                UI_Key = 5255,
            };

            if (useVanillaDefaults)
            {
                BreakLegs = true;

                BetterArmor = new BetterArmorConfig()
                {
                    Enabled = true,
                    UseArmorClasses = true,
                    ArmorDamageMultiplierOnPen = 0.5f,
                    PenDamgeDelta = 0.7f,
                    BetterHitZones = new BetterHitZonesConfig
                    {
                        Enabled = true,
                        HatsProtectFace = false,
                        VestsProtectStomach = true,
                        PlayerPenetration = new PlayerPenetrationConfig
                        {
                            Enabled = true,
                            Arm = new PenResistence
                            {
                                RequiredPenetration = 10,
                                PenetrationForMinReduction = 40,
                                MaxPenReduction = 0.3f,
                                MinPenReduction = 0.1f,
                            },
                            Leg = new PenResistence
                            {
                                RequiredPenetration = 10,
                                PenetrationForMinReduction = 40,
                                MaxPenReduction = 0.3f,
                                MinPenReduction = 0.1f,
                            },
                            Skull = new PenResistence
                            {
                                RequiredPenetration = 20,
                                PenetrationForMinReduction = 45,
                                MaxPenReduction = 0.4f,
                                MinPenReduction = 0.2f,
                            },
                            Spine = new PenResistence
                            {
                                RequiredPenetration = 25,
                                PenetrationForMinReduction = 50,
                                MaxPenReduction = 0.5f,
                                MinPenReduction = 0.2f,
                            },
                            Stomach = new PenResistence
                            {
                                RequiredPenetration = 15,
                                PenetrationForMinReduction = 40,
                                MaxPenReduction = 0.35f,
                                MinPenReduction = 0.15f,
                            },
                        }
                    },
                };
                BoneBreakingChances = new List<BulletLimbDamageChance>
                {
                    new BulletLimbDamageChance{ Limb = "LEG", BreakChanceMin = 10, BreakChanceMax = 95, BreakChanceDamageMin = 10, BreakChanceDamageMax = 50},
                    new BulletLimbDamageChance{ Limb = "FOOT", BreakChanceMin = 10, BreakChanceMax = 95, BreakChanceDamageMin = 10, BreakChanceDamageMax = 50},
                    new BulletLimbDamageChance{ Limb = "ARM", BreakChanceMin = 0, BreakChanceMax = 0, BreakChanceDamageMin = 0, BreakChanceDamageMax = 0},
                    new BulletLimbDamageChance{ Limb = "HAND", BreakChanceMin = 0, BreakChanceMax = 0, BreakChanceDamageMin = 0, BreakChanceDamageMax = 0},
                    new BulletLimbDamageChance{ Limb = "SKULL", BreakChanceMin = 0, BreakChanceMax = 0, BreakChanceDamageMin = 0, BreakChanceDamageMax = 0},
                    new BulletLimbDamageChance{ Limb = "SPINE", BreakChanceMin = 0, BreakChanceMax = 0, BreakChanceDamageMin = 0, BreakChanceDamageMax = 0}
                };
                MovementExtension = new MovementExtension
                {
                    PushupStaminaDrain = 10,
                    ReequipGunsOnProne = true,
                };
                ArmorClasses = new List<ArmorClass>
                {
                    new ArmorClass{
                        Tier=0.5f, Armor = 0.95f,
                        PercentForNormalDamage = 0.2f, PercentForMaxDamage = 0.8f,
                        DamageMultiplierMin = 0.8f,    DamageMultiplierNormal = 1f,
                        DamageToDamageArmorMin = 0, DamageToDamageArmorMax = 40,
                        MinArmorDamage = 1, MaxArmorDamage = 4,
                        StopDamageMulti = 0.4f, PenLossMulti = 0},
                    new ArmorClass{
                        Tier=1f, Armor = 0.9f,
                        PercentForNormalDamage =  0.2f, PercentForMaxDamage = 0.8f,
                        DamageMultiplierMin = 0.8f,    DamageMultiplierNormal = 1f,
                        DamageToDamageArmorMin = 0, DamageToDamageArmorMax = 40,
                        MinArmorDamage = 1, MaxArmorDamage = 4,
                        StopDamageMulti = 0.2f, PenLossMulti = 0.1f},
                    new ArmorClass{
                        Tier=2f, Armor = 0.85f,
                        PercentForNormalDamage = 0.2f, PercentForMaxDamage = 0.8f,
                        DamageMultiplierMin = 0.4f,    DamageMultiplierNormal = 0.8f,
                        DamageToDamageArmorMin = 15, DamageToDamageArmorMax = 60,
                        MinArmorDamage = 1, MaxArmorDamage = 4,
                        StopDamageMulti = 0.1f, PenLossMulti = 0.2f},
                    new ArmorClass{
                        Tier=3f, Armor = 0.8f,
                        PercentForNormalDamage = 0.2f, PercentForMaxDamage = 0.8f,
                        DamageMultiplierMin = 0.4f,    DamageMultiplierNormal = 0.8f,
                        DamageToDamageArmorMin = 15, DamageToDamageArmorMax = 60,
                        MinArmorDamage = 1, MaxArmorDamage = 4,
                        StopDamageMulti = 0.1f, PenLossMulti = 0.2f},
                    new ArmorClass{
                        Tier=3.5f, Armor = 0.7f,
                        PercentForNormalDamage = 0.2f, PercentForMaxDamage = 0.8f,
                        DamageMultiplierMin = 0.4f,    DamageMultiplierNormal = 0.8f,
                        DamageToDamageArmorMin = 15, DamageToDamageArmorMax = 60,
                        MinArmorDamage = 1, MaxArmorDamage = 4,
                        StopDamageMulti = 0.05f, PenLossMulti = 0.3f},
                    new ArmorClass{
                        Tier=4f, Armor = 0.65f,
                        PercentForNormalDamage = 0.2f, PercentForMaxDamage = 0.8f,
                        DamageMultiplierMin = 0.4f,    DamageMultiplierNormal = 0.8f,
                        DamageToDamageArmorMin = 15, DamageToDamageArmorMax = 80,
                        MinArmorDamage = 1, MaxArmorDamage = 4,
                        StopDamageMulti = 0.02f, PenLossMulti = 0.35f},
                };

                HatExtensions = new List<HatExtension>
                {
                    new HatExtension()
                    {
                        Id = 1525,
                        Name = "Spec Ops Helmet",
                        ProtectFace = true,
                        ArmorFace = 0.85f,
                    }
                };

                MaskExtensions = new List<MaskExtension> { };

                GlassesExtensions = new List<GlassesExtension>
                {
                    new GlassesExtension()
                    {
                        Id = 334,
                        Name = "Military Nightvision",
                        EquipEffectId = 0,
                        UnequipEffectId = 0,
                    }
                };

                VestExtensions = new List<VestExtension>
                {
                    new VestExtension()
                    {
                        Id = 1169,
                        Name = "Spec Ops Vest",
                        ProtectStomach = true,
                        ShoulderPlateLength = 0.4f,
                        ArmorShoulderPlate = 0.65f,
                        ThighPlateLength = 0.3f,
                        ArmorThighPlate = 0.65f,
                    },
                    new VestExtension()
                    {
                        Id = 1168,
                        Name = "Vest_Civilian",
                        ProtectStomach = false,
                        ShoulderPlateLength = 0,
                        ArmorShoulderPlate = 1,
                        ThighPlateLength = 0,
                        ArmorThighPlate = 1,
                    }
                };

                GunExtensions = new List<GunExtension>
                {
                    // Civ Pistols
                    new GunExtension() { Id = 107, Name = "Ace", Penetration = 17 },
                    new GunExtension() { Id = 99, Name = "Cobra", Penetration = 12.5f },
                    new GunExtension() { Id = 97, Name = "Colt", Penetration = 12.5f },
                    new GunExtension() { Id = 1039, Name = "Kryzkarek", Penetration = 12.5f },
                    new GunExtension() { Id = 1476, Name = "Luger", Penetration = 12.5f },
                    // Civ Guns
                    new GunExtension() { Id = 109, Name = "Hawkhound", Penetration = 27 },
                    new GunExtension() { Id = 101, Name = "Schofield", Penetration = 27 },
                    new GunExtension() { Id = 479, Name = "Rifle_Birch", Penetration = 25 },
                    new GunExtension() { Id = 474, Name = "Rifle_Maple", Penetration = 25 },
                    new GunExtension() { Id = 480, Name = "Rifle_Pine", Penetration = 25 },
                    new GunExtension() { Id = 484, Name = "Sportshot", Penetration =  15.5f},
                    new GunExtension() { Id = 1027, Name = "Viper", Penetration = 14f },
                    // LC Ranger Pistol
                    new GunExtension() { Id = 1360, Name = "Teklowvka", Penetration = 15.5f },
                    // LC Ranger Guns
                    new GunExtension() { Id = 1362, Name = "Augewehr", Penetration = 18.5f },
                    new GunExtension() { Id = 1369, Name = "Bulldog", Penetration = 15.5f },
                    new GunExtension() { Id = 1379, Name = "Card", Penetration = 15.5f },
                    new GunExtension() { Id = 1364, Name = "Fury", Penetration = 19 },
                    new GunExtension() { Id = 1375, Name = "Fusilaut", Penetration = 19 },
                    new GunExtension() { Id = 1477, Name = "MP40", Penetration = 15.5f },
                    new GunExtension() { Id = 1377, Name = "Nightraider", Penetration = 18 },
                    new GunExtension() { Id = 126, Name = "Nykorev", Penetration = 19 },
                    new GunExtension() { Id = 129, Name = "Snayperskya", Penetration = 22 },
                    new GunExtension() { Id = 1041, Name = "Yuri", Penetration = 15.5f },
                    new GunExtension() { Id = 122, Name = "Zubeknakov", Penetration = 18 },
                    // HC Ranger
                    new GunExtension() { Id = 1382, Name = "Ekho", Penetration = 35 },
                    new GunExtension() { Id = 1000, Name = "Matamorez", Penetration = 30 },
                    // LC Mil Pistol
                    new GunExtension() { Id = 1021, Name = "Avenger", Penetration = 15.5f },
                    // LC Mil Guns
                    new GunExtension() { Id = 116, Name = "Honeybadger", Penetration = 17.5f },
                    new GunExtension() { Id = 4, Name = "Eaglefire", Penetration = 18 },
                    new GunExtension() { Id = 1481, Name = "Empire", Penetration = 15.5f },
                    new GunExtension() { Id = 1037, Name = "Heartbreaker", Penetration = 18.5f },
                    new GunExtension() { Id = 363, Name = "Maplestrike", Penetration = 18 },
                    new GunExtension() { Id = 1024, Name = "Peacemaker", Penetration = 15.5f },
                    new GunExtension() { Id = 1018, Name = "Sabertooth", Penetration = 22 },
                    new GunExtension() { Id = 1447, Name = "Scalar", Penetration = 15.5f },
                    new GunExtension() { Id = 1488, Name = "Swissgewehr", Penetration = 19 },
                    // HC Mil Pistol
                    new GunExtension() { Id = 488, Name = "Desert_Falcon", Penetration = 19 },
                    // HC Mil Guns
                    new GunExtension() { Id = 297, Name = "Grizzly", Penetration = 35 },
                    new GunExtension() { Id = 132, Name = "Dragonfang", Penetration = 27 },
                    new GunExtension() { Id = 18, Name = "Timberwolf", Penetration = 35 },
                    // Shotguns
                    new GunExtension() { Id = 112, Name = "Bluntforce", Penetration = 13f },
                    new GunExtension() { Id = 1484, Name = "Bane", Penetration = 14f },
                    new GunExtension() { Id = 1480, Name = "Determinator", Penetration = 12f },
                    new GunExtension() { Id = 380, Name = "Masterkey", Penetration = 10.5f },
                    new GunExtension() { Id = 1436, Name = "Quadbarrel", Penetration = 10.5f },
                    new GunExtension() { Id = 1143, Name = "Sawed_Off", Penetration = 10f },
                    new GunExtension() { Id = 1366, Name = "Vonya", Penetration = 13 },
                    // Other
                    new GunExtension() { Id = 355, Name = "Bow_Birch", Penetration = 16 },
                    new GunExtension() { Id = 353, Name = "Bow_Maple", Penetration = 16 },
                    new GunExtension() { Id = 356, Name = "Bow_Pine", Penetration = 16 },
                    new GunExtension() { Id = 357, Name = "Bow_Compound", Penetration = 18 },
                    new GunExtension() { Id = 346, Name = "Crossbow", Penetration = 18 },
                    new GunExtension() { Id = 1165, Name = "Nailgun", Penetration = 3 },
                    new GunExtension() { Id = 1337, Name = "Paintballgun", Penetration = 0 },
                    new GunExtension() { Id = 300, Name = "Shadowstalker", Penetration = 27 },
                    new GunExtension() { Id = 1441, Name = "ShadowstalkerMk2", Penetration = 27 },
                    // HMG
                    new GunExtension() { Id = 1394, Name = "HMG", Penetration = 25 },
                    new GunExtension() { Id = 1471, Name = "HMG_Fighter_Jet", Penetration = 25 },
                };

                CyclableHelmets = new List<List<ItemExtension>> {};

                CyclableSights = new List<List<ItemExtension>> {};
            }
            else
            {
                BreakLegs = true;

                BetterArmor = new BetterArmorConfig()
                {
                    Enabled = true,
                    UseArmorClasses = true,
                    ArmorDamageMultiplierOnPen = 0.5f,
                    PenDamgeDelta = 0.7f,
                    BetterHitZones = new BetterHitZonesConfig
                    {
                        Enabled = true,
                        HatsProtectFace = false,
                        HatsProtectEars = false,
                        VestsProtectStomach = true,
                        PlayerPenetration = new PlayerPenetrationConfig
                        {
                            Enabled = true,
                            MaxPenetrations = 2,
                            Arm = new PenResistence
                            {
                                RequiredPenetration = 10,
                                PenetrationForMinReduction = 40,
                                MaxPenReduction = 0.3f,
                                MinPenReduction = 0.1f,
                            },
                            Leg = new PenResistence
                            {
                                RequiredPenetration = 10,
                                PenetrationForMinReduction = 40,
                                MaxPenReduction = 0.3f,
                                MinPenReduction = 0.1f,
                            },
                            Skull = new PenResistence
                            {
                                RequiredPenetration = 20,
                                PenetrationForMinReduction = 45,
                                MaxPenReduction = 0.4f,
                                MinPenReduction = 0.2f,
                            },
                            Spine = new PenResistence
                            {
                                RequiredPenetration = 25,
                                PenetrationForMinReduction = 50,
                                MaxPenReduction = 0.5f,
                                MinPenReduction = 0.2f,
                            },
                            Stomach = new PenResistence
                            {
                                RequiredPenetration = 15,
                                PenetrationForMinReduction = 40,
                                MaxPenReduction = 0.35f,
                                MinPenReduction = 0.15f,
                            },
                        }
                    },
                };

                BoneBreakingChances = new List<BulletLimbDamageChance>
                {
                    new BulletLimbDamageChance{ Limb = "LEG", BreakChanceMin = 10, BreakChanceMax = 98, BreakChanceDamageMin = 10, BreakChanceDamageMax = 40},
                    new BulletLimbDamageChance{ Limb = "FOOT", BreakChanceMin = 10, BreakChanceMax = 98, BreakChanceDamageMin = 10, BreakChanceDamageMax = 40},
                    new BulletLimbDamageChance{ Limb = "ARM", BreakChanceMin = 0, BreakChanceMax = 0, BreakChanceDamageMin = 0, BreakChanceDamageMax = 0},
                    new BulletLimbDamageChance{ Limb = "HAND", BreakChanceMin = 0, BreakChanceMax = 0, BreakChanceDamageMin = 0, BreakChanceDamageMax = 0},
                    new BulletLimbDamageChance{ Limb = "SKULL", BreakChanceMin = 0, BreakChanceMax = 0, BreakChanceDamageMin = 0, BreakChanceDamageMax = 0},
                    new BulletLimbDamageChance{ Limb = "SPINE", BreakChanceMin = 0, BreakChanceMax = 0, BreakChanceDamageMin = 0, BreakChanceDamageMax = 0}
                };

                MovementExtension = new MovementExtension
                {
                    PushupStaminaDrain = 10,
                    ReequipGunsOnProne = true,
                };

                ArmorClasses = new List<ArmorClass>
                {
                    new ArmorClass{
                        Tier=0.0f, Armor = 1f,
                        PercentForNormalDamage = 0.2f, PercentForMaxDamage = 0.2f,
                        DamageMultiplierMin = 1f,    DamageMultiplierNormal = 1f,
                        DamageToDamageArmorMin = 0, DamageToDamageArmorMax = 40,
                        MinArmorDamage = 1, MaxArmorDamage = 14,
                        StopDamageMulti = 0.4f, PenLossMulti = 0
                    },
                    new ArmorClass{
                        Tier=0.0f, Armor = 0.9f,
                        PercentForNormalDamage = 0.2f, PercentForMaxDamage = 0.2f,
                        DamageMultiplierMin = 1f,    DamageMultiplierNormal = 1f,
                        DamageToDamageArmorMin = 0, DamageToDamageArmorMax = 90,
                        MinArmorDamage = 1, MaxArmorDamage = 14,
                        StopDamageMulti = 0.4f, PenLossMulti = 0
                    },
                    new ArmorClass{
                        Tier=1f, Armor =  0.8f,
                        PercentForNormalDamage = 0.2f, PercentForMaxDamage = 0.8f,
                        DamageMultiplierMin = 0.4f,    DamageMultiplierNormal = 0.8f,
                        DamageToDamageArmorMin = 2, DamageToDamageArmorMax = 90,
                        MinArmorDamage = 0.3f, MaxArmorDamage = 14,
                        StopDamageMulti = 0.4f, PenLossMulti = 0.1f
                    },
                    new ArmorClass{
                        Tier=2f, Armor =  0.7f,
                        PercentForNormalDamage = 0.2f, PercentForMaxDamage = 0.8f,
                        DamageMultiplierMin = 0.4f,    DamageMultiplierNormal = 0.8f,
                        DamageToDamageArmorMin = 5, DamageToDamageArmorMax = 90,
                        MinArmorDamage = 0.3f, MaxArmorDamage = 14,
                        StopDamageMulti = 0.2f, PenLossMulti = 0.1f
                    },
                    new ArmorClass{
                        Tier=3f, Armor = 0.6f,
                        PercentForNormalDamage =  0.2f, PercentForMaxDamage = 0.8f,
                        DamageMultiplierMin = 0.4f,    DamageMultiplierNormal = 0.8f,
                        DamageToDamageArmorMin = 10, DamageToDamageArmorMax = 90,
                        MinArmorDamage = 0.3f, MaxArmorDamage = 14,
                        StopDamageMulti = 0.1f, PenLossMulti = 0.2f
                    },
                    new ArmorClass{
                        Tier=4f, Armor = 0.5f,
                        PercentForNormalDamage = 0.2f, PercentForMaxDamage = 0.8f,
                        DamageMultiplierMin = 0.4f,    DamageMultiplierNormal = 0.8f,
                        DamageToDamageArmorMin = 12, DamageToDamageArmorMax = 90,
                        MinArmorDamage = 0.3f, MaxArmorDamage = 14,
                        StopDamageMulti = 0.1f, PenLossMulti = 0.25f
                    },
                    new ArmorClass{
                        Tier=5f, Armor = 0.4f,
                        PercentForNormalDamage = 0.2f, PercentForMaxDamage = 0.8f,
                        DamageMultiplierMin = 0.4f,    DamageMultiplierNormal = 0.8f,
                        DamageToDamageArmorMin = 16, DamageToDamageArmorMax = 90,
                        MinArmorDamage = 0.3f, MaxArmorDamage = 14,
                        StopDamageMulti = 0.05f, PenLossMulti = 0.3f
                    },
                    new ArmorClass{
                        Tier=6f, Armor = 0.3f,
                        PercentForNormalDamage = 0.2f, PercentForMaxDamage = 0.8f,
                        DamageMultiplierMin = 0.4f,    DamageMultiplierNormal = 0.8f,
                        DamageToDamageArmorMin = 20, DamageToDamageArmorMax = 90,
                        MinArmorDamage = 0.3f, MaxArmorDamage = 14,
                        StopDamageMulti = 0.02f, PenLossMulti = 0.35f
                    },
                    new ArmorClass{
                        Tier=7f, Armor = 0.2f,
                        PercentForNormalDamage = 0.2f, PercentForMaxDamage = 0.8f,
                        DamageMultiplierMin = 0.4f,    DamageMultiplierNormal = 0.8f,
                        DamageToDamageArmorMin = 20, DamageToDamageArmorMax = 90,
                        MinArmorDamage = 0.3f, MaxArmorDamage = 14,
                        StopDamageMulti = 0.02f, PenLossMulti = 0.35f
                    },
                };

                HatExtensions = new List<HatExtension>
                {
                    new HatExtension()
                    {
                        Id = 37430,
                        Name = "Kolpak-1S Helmet",
                        ProtectEars = true,
                        ArmorEars = -1f,
                        ProtectFace = true,
                        ArmorFace = 0.8f,
                        PreventNVGs = true,
                        EquipEffectId = 52114,
                        UnequipEffectId = 52115,
                    },
                    new HatExtension()
                    {
                        Id = 37416,
                        Name = "Fast MT Kek Helmet + SA",
                        ProtectEars = true,
                        ArmorEars = 0.7f,
                    },
                    new HatExtension()
                    {
                        Id = 37417,
                        Name = "Fast MT Kek Helmet + TM",
                        ProtectFace = true,
                        ArmorFace = 0.7f,
                        EquipEffectId = 52120,
                        UnequipEffectId = 52121,
                    },
                    new HatExtension()
                    {
                        Id = 37418,
                        Name = "Fast MT Kek Helmet + TM + SA",
                        ProtectEars = true,
                        ArmorEars = 0.7f,
                        ProtectFace = true,
                        ArmorFace = 0.7f,
                        EquipEffectId = 52120,
                        UnequipEffectId = 52121,
                    },
                    new HatExtension()
                    {
                        Id = 37437,
                        Name = "UNTAR Helmet",
                        ProtectEars = true,
                        ArmorEars = -1f,
                    },
                    new HatExtension()
                    {
                        Id = 37400,
                        Name = "6B47 Ratnik-BSh Flora Helmet",
                        ProtectEars = true,
                        ArmorEars = -1f,
                    },
                    new HatExtension()
                    {
                        Id = 37401,
                        Name = "6B47 Ratnik-BSh Helmet",
                        ProtectEars = true,
                        ArmorEars = -1f,
                    },
                    new HatExtension()
                    {
                        Id = 37434,
                        Name = "DEVTAC Ronin ballistic Helmet",
                        ProtectEars = true,
                        ArmorEars = -1f,
                        ProtectFace = true,
                        ArmorFace = -1,
                        EquipEffectId = 52120,
                        UnequipEffectId = 52121,
                    },
                    new HatExtension()
                    {
                        Id = 37441,
                        Name = "ULACH IIIA Black Helmet",
                        ProtectEars = true,
                        ArmorEars = -1f,
                        EquipEffectId = 52116,
                        UnequipEffectId = 52117,
                    },
                    new HatExtension()
                    {
                        Id = 37442,
                        Name = "ULACH IIIA Tan Helmet",
                        ProtectEars = true,
                        ArmorEars = -1f,
                        EquipEffectId = 52116,
                        UnequipEffectId = 52117,
                    },
                    new HatExtension()
                    {
                        Id = 37405,
                        Name = "Airframe Helmet + SA",
                        ProtectEars = true,
                        ArmorEars = 0.6f,
                        EquipEffectId = 52116,
                        UnequipEffectId = 52117,
                    },
                    new HatExtension()
                    {
                        Id = 37403,
                        Name = "Airframe Helmet + FS",
                        ProtectFace = true,
                        ArmorFace = 0.6f,
                        EquipEffectId = 52114,
                        UnequipEffectId = 52115,
                    },
                    new HatExtension()
                    {
                        Id = 37404,
                        Name = "Airframe Helmet + FS + SA",
                        ProtectEars = true,
                        ArmorEars = 0.6f,
                        ProtectFace = true,
                        ArmorFace = 0.6f,
                        EquipEffectId = 52116,
                        UnequipEffectId = 52117,
                    },
                    new HatExtension()
                    {
                        Id = 37412,
                        Name = "Fast MT Tan Helmet + SLAAP + SA",
                        ProtectEars = true,
                        ArmorEars = 0.6f,
                    },
                    new HatExtension()
                    {
                        Id = 37413,
                        Name = "Fast MT Black Helmet + TM",
                        ProtectFace = true,
                        ArmorFace = 0.7f,
                        EquipEffectId = 52120,
                        UnequipEffectId = 52121,
                    },
                    new HatExtension()
                    {
                        Id = 37414,
                        Name = "Fast MT Black Helmet + TM + SA",
                        ProtectEars = true,
                        ArmorEars = 0.6f,
                        ProtectFace = true,
                        ArmorFace = 0.7f,
                        EquipEffectId = 52120,
                        UnequipEffectId = 52121,
                    },
                    new HatExtension()
                    {
                        Id = 37422,
                        Name = "Fast MT Tan Helmet + SLAAP + SA",
                        ProtectEars = true,
                        ArmorEars = 0.6f,
                    },
                    new HatExtension()
                    {
                        Id = 37423,
                        Name = "Fast MT Tan Helmet + TM",
                        ProtectFace = true,
                        ArmorFace = 0.7f,
                        EquipEffectId = 52120,
                        UnequipEffectId = 52121,
                    },
                    new HatExtension()
                    {
                        Id = 37424,
                        Name = "Fast MT Tan Helmet + TM + SA",
                        ProtectEars = true,
                        ArmorEars = 0.6f,
                        ProtectFace = true,
                        ArmorFace = 0.7f,
                        EquipEffectId = 52120,
                        UnequipEffectId = 52121,
                    },
                    new HatExtension()
                    {
                        Id = 37426,
                        Name = "Ops-Core Light Trooper Helmet ",
                        ProtectEars = true,
                        ArmorEars = -1f,
                        ProtectFace = true,
                        ArmorFace = -1f,
                        EquipEffectId = 52120,
                        UnequipEffectId = 52121,
                    },
                    new HatExtension()
                    {
                        Id = 37425,
                        Name = "Ops-Core Heavy Trooper Helmet ",
                         ProtectEars = true,
                        ArmorEars = -1f,
                        ProtectFace = true,
                        ArmorFace = -1f,
                        EquipEffectId = 52120,
                        UnequipEffectId = 52121,
                    },
                    new HatExtension()
                    {
                        Id = 37435,
                        Name = "Ops-Core Fast MT Samurai Helmet ",
                        ProtectEars = true,
                        ArmorEars = -1f,
                        ProtectFace = true,
                        ArmorFace = -1f,
                        EquipEffectId = 52120,
                        UnequipEffectId = 52121,
                    },
                    new HatExtension()
                    {
                        Id = 37431,
                        Name = "Maska 1Sch Helmet ",
                        ProtectEars = true,
                        ArmorEars = -1f,
                        ProtectFace = true,
                        ArmorFace = -1f,
                        PreventNVGs = true,
                        EquipEffectId = 52110,
                        UnequipEffectId = 52111,
                    },
                    new HatExtension()
                    {
                        Id = 37432,
                        Name = "Maska 1Sch KILLA Helmet",
                        ProtectEars = true,
                        ArmorEars = -1f,
                        ProtectFace = true,
                        ArmorFace = -1f,
                        PreventNVGs = true,
                        EquipEffectId = 52110,
                        UnequipEffectId = 52111,
                    },
                    new HatExtension()
                    {
                        Id = 37408,
                        Name = "Altyn Helmet",
                        ProtectEars = true,
                        ArmorEars = -1f,
                        ProtectFace = true,
                        ArmorFace = -1f,
                        PreventNVGs = true,
                        EquipEffectId = 52112,
                        UnequipEffectId = 52113,
                    },
                };

                MaskExtensions = new List<MaskExtension> { };

                GlassesExtensions = new List<GlassesExtension>
                {
                    new GlassesExtension()
                    {
                        Id = 37555,
                        Name = "GPNVG-18_Nightvision_Tan",
                        EquipEffectId = 52211,
                        UnequipEffectId = 0,
                    },
                    new GlassesExtension()
                    {
                        Id = 37556,
                        Name = "GPNVG-18_Nightvision_Black",
                        EquipEffectId = 52211,
                        UnequipEffectId = 0,
                    },
                    new GlassesExtension()
                    {
                        Id = 37557,
                        Name = "PNV-10T_Nightvision",
                        EquipEffectId = 52210,
                        UnequipEffectId = 0,
                    }
                };

                VestExtensions = new List<VestExtension>
                {
                    new VestExtension()
                    {
                        Id = 37328,
                        Name = "PACA Soft Armor",
                        ProtectStomach = false,
                    },
                    new VestExtension()
                    {
                        Id = 37329,
                        Name = "PACA Soft Armor + DIY",
                        ProtectStomach = false,
                    },
                    new VestExtension()
                    {
                        Id = 37330,
                        Name = "PACA Soft Armor + SOE",
                        ProtectStomach = false,
                    },
                    new VestExtension()
                    {
                        Id = 37331,
                        Name = "PACA Soft Armor + Triton",
                        ProtectStomach = false,
                    },
                    new VestExtension()
                    {
                        Id = 37332,
                        Name = "PACA Soft Armor + Wartech",
                        ProtectStomach = false,
                    },
                    new VestExtension()
                    {
                        Id = 37315,
                        Name = "Highcom Trooper TFO Armor Multicam",
                        ProtectStomach = false,
                    },
                    new VestExtension()
                    {
                        Id = 37316,
                        Name = "Highcom Trooper TFO Armor Multicam + Bags",
                        ProtectStomach = false,
                    },
                    new VestExtension()
                    {
                        Id = 37317,
                        Name = "Highcom Trooper TFO Armor Tropic",
                        ProtectStomach = false,
                    },
                    new VestExtension()
                    {
                        Id = 37318,
                        Name = "Highcom Trooper TFO Armor Tropic + Bags",
                        ProtectStomach = false,
                    },
                    new VestExtension()
                    {
                        Id = 37337,
                        Name = "TV-110 Plate Carrier",
                        ProtectStomach = false,
                    },
                    new VestExtension()
                    {
                        Id = 37338,
                        Name = "TV-110 Plate Carrier + All Bags",
                        ProtectStomach = false,
                    },
                    new VestExtension()
                    {
                        Id = 37339,
                        Name = "TV-110 Plate Carrier + Magazine Bags",
                        ProtectStomach = false,
                    },
                    new VestExtension()
                    {
                        Id = 37340,
                        Name = "TV-110 Plate Carrier + Side Bags",
                        ProtectStomach = false,
                    },
                    new VestExtension()
                    {
                        Id = 37357,
                        Name = "5.11 Hexgrid Plate Carrier",
                        ProtectStomach = false,
                    },
                    new VestExtension()
                    {
                        Id = 37360,
                        Name = "CPC MOD.2 Platecarrier Normal",
                        ProtectStomach = false,
                    },
                    new VestExtension()
                    {
                        Id = 37361,
                        Name = "CPC MOD.2 Platecarrier Mag Bags",
                        ProtectStomach = false,
                    },
                    new VestExtension()
                    {
                        Id = 37362,
                        Name = "CPC MOD.2 Platecarrier Side Bags",
                        ProtectStomach = false,
                    },
                    new VestExtension()
                    {
                        Id = 37363,
                        Name = "CPC MOD.2 Platecarrier All Bags",
                        ProtectStomach = false,
                    },
                    new VestExtension()
                    {
                        Id = 37366,
                        Name = "LBT 6094A Slick Platecarrier",
                        ProtectStomach = false,
                    },
                    new VestExtension()
                    {
                        Id = 37348,
                        Name = "Crye Precision AVS MBAV Tagilla",
                        ProtectStomach = false,
                    },
                    new VestExtension()
                    {
                        Id = 37319,
                        Name = "IOTV Gen4 Armor",
                        ProtectStomach = true,
                        ShoulderPlateLength = 0.4f,
                        ArmorShoulderPlate = 0.45f,
                        ThighPlateLength = 0.3f,
                        ArmorThighPlate = 0.45f,
                    },
                    new VestExtension()
                    {
                        Id = 37320,
                        Name = "IOTV Gen4 Armor + Magazine Bags",
                        ProtectStomach = true,
                        ShoulderPlateLength = 0.4f,
                        ArmorShoulderPlate = 0.45f,
                        ThighPlateLength = 0.3f,
                        ArmorThighPlate = 0.45f,
                    },
                    new VestExtension()
                    {
                        Id = 37303,
                        Name = "6B43 6A Armor",
                        ProtectStomach = true,
                        ShoulderPlateLength = 0.4f,
                        ArmorShoulderPlate =  0.55f,
                        ThighPlateLength =  0.3f,
                        ArmorThighPlate =  0.55f,
                    },
                    new VestExtension()
                    {
                        Id = 37304,
                        Name = "6B43 6A Armor + Magazine Bags",
                        ProtectStomach = true,
                        ShoulderPlateLength = 0.4f,
                        ArmorShoulderPlate =  0.55f,
                        ThighPlateLength =  0.3f,
                        ArmorThighPlate =  0.55f,
                    },

                };

                GunExtensions = new List<GunExtension>
                {
                    #region Assault Rifles AKs 
                    // AK-74N 5.45x39
                    new GunExtension() { Id = 37621, Name = "", Penetration = -1},
                    new GunExtension() { Id = 37622, Name = "", Penetration = -1},
                    new GunExtension() { Id = 37623, Name = "", Penetration = -1},
                    new GunExtension() { Id = 37624, Name = "", Penetration = -1},
                    new GunExtension() { Id = 37625, Name = "", Penetration = -1},
                    new GunExtension() { Id = 37626, Name = "", Penetration = -1},
                    new GunExtension() { Id = 37627, Name = "", Penetration = -1},
                    new GunExtension() { Id = 37628, Name = "", Penetration = -1},
                    // AK-101 5.56x45
                    new GunExtension() { Id = 37651, Name = "", Penetration = -1},
                    new GunExtension() { Id = 37652, Name = "", Penetration = -1},
                    new GunExtension() { Id = 37653, Name = "", Penetration = -1},
                    new GunExtension() { Id = 37654, Name = "", Penetration = -1},
                    new GunExtension() { Id = 37655, Name = "", Penetration = -1},
                    new GunExtension() { Id = 37656, Name = "", Penetration = -1},
                    new GunExtension() { Id = 37657, Name = "", Penetration = -1},
                    new GunExtension() { Id = 37658, Name = "", Penetration = -1},
                    // AKM 7.62x39
                    new GunExtension() { Id = 37661, Name = "", Penetration = -1},
                    new GunExtension() { Id = 37662, Name = "", Penetration = -1},
                    new GunExtension() { Id = 37663, Name = "", Penetration = -1},
                    new GunExtension() { Id = 37664, Name = "", Penetration = -1},
                    new GunExtension() { Id = 37665, Name = "", Penetration = -1},
                    new GunExtension() { Id = 37666, Name = "", Penetration = -1},
                    new GunExtension() { Id = 37667, Name = "", Penetration = -1},
                    new GunExtension() { Id = 37668, Name = "", Penetration = -1},
                    // AKMS 7.62x39
                    new GunExtension() { Id = 37670, Name = "", Penetration = -1},
                    // AKS-74U 5.45x39
                    new GunExtension() { Id = 37631, Name = "", Penetration = -1},
                    new GunExtension() { Id = 37632, Name = "", Penetration = -1},
                    new GunExtension() { Id = 37633, Name = "", Penetration = -1},
                    new GunExtension() { Id = 37634, Name = "", Penetration = -1},
                    new GunExtension() { Id = 37635, Name = "", Penetration = -1},
                    new GunExtension() { Id = 37636, Name = "", Penetration = -1},
                    new GunExtension() { Id = 37637, Name = "", Penetration = -1},
                    new GunExtension() { Id = 37638, Name = "", Penetration = -1},
                    #endregion
                    #region Assault Rifles ARs 
                    // ADAR 2-15 5.56x45
                    new GunExtension() { Id = 37611, Name = "", Penetration = -1},
                    // M4A1 5.56x45
                    new GunExtension() { Id = 37601, Name = "", Penetration = -1},
                    new GunExtension() { Id = 37602, Name = "", Penetration = -1},
                    new GunExtension() { Id = 37603, Name = "", Penetration = -1},
                    new GunExtension() { Id = 37604, Name = "", Penetration = -1},
                    new GunExtension() { Id = 37605, Name = "", Penetration = -1},
                    new GunExtension() { Id = 37606, Name = "", Penetration = -1},
                    new GunExtension() { Id = 37607, Name = "", Penetration = -1},
                    new GunExtension() { Id = 37608, Name = "", Penetration = -1},
                    // MK47 7.62x39
                    new GunExtension() { Id = 37646, Name = "", Penetration = -1},
                    new GunExtension() { Id = 37647, Name = "", Penetration = -1},
                    new GunExtension() { Id = 37648, Name = "", Penetration = -1},
                    new GunExtension() { Id = 37649, Name = "", Penetration = -1},
                    // SA-58 7.62x51
                    new GunExtension() { Id = 38031, Name = "", Penetration = -1},
                    new GunExtension() { Id = 38032, Name = "", Penetration = -1},
                    new GunExtension() { Id = 38033, Name = "", Penetration = -1},
                    new GunExtension() { Id = 38034, Name = "", Penetration = -1},
                    new GunExtension() { Id = 38035, Name = "", Penetration = -1},
                    new GunExtension() { Id = 38036, Name = "", Penetration = -1},
                    new GunExtension() { Id = 38037, Name = "", Penetration = -1},
                    new GunExtension() { Id = 38038, Name = "", Penetration = -1},
                    #endregion
                    #region Assault Rifles Others 
                    // ASh-12 12.7x55
                    new GunExtension() { Id = 37676, Name = "", Penetration = -1},
                    // AS-Val 9x39 
                    new GunExtension() { Id = 38070, Name = "", Penetration = -1},
                    new GunExtension() { Id = 38071, Name = "", Penetration = -1},
                    new GunExtension() { Id = 38072, Name = "", Penetration = -1},
                    new GunExtension() { Id = 38073, Name = "", Penetration = -1},
                    // VPO-209 .366 TKM
                    new GunExtension() { Id = 38131, Name = "", Penetration = -1},
                    new GunExtension() { Id = 38137, Name = "", Penetration = -1},
                    new GunExtension() { Id = 38133, Name = "", Penetration = -1},
                    new GunExtension() { Id = 38135, Name = "", Penetration = -1},
                    new GunExtension() { Id = 38138, Name = "", Penetration = -1},
                    new GunExtension() { Id = 38134, Name = "", Penetration = -1},
                    new GunExtension() { Id = 38136, Name = "", Penetration = -1},
                    new GunExtension() { Id = 38132, Name = "", Penetration = -1},
                    #endregion
                    #region Designated Marksman Rifles 
                    // SKS 7.62x39
                    new GunExtension() { Id = 38051, Name = "", Penetration = -1},
                    new GunExtension() { Id = 38052, Name = "", Penetration = -1},
                    new GunExtension() { Id = 38053, Name = "", Penetration = -1},
                    // VPO-101 7.62x51
                    new GunExtension() { Id = 38046, Name = "", Penetration = -1},
                    // SVDS 7.62x54R
                    new GunExtension() { Id = 38061, Name = "", Penetration = -1},
                    // RFB 7.62x51
                    new GunExtension() { Id = 38093, Name = "", Penetration = -1},
                    // VSS Vintorez 9x39
                    new GunExtension() { Id = 38067, Name = "", Penetration = -1},
                    new GunExtension() { Id = 38068, Name = "", Penetration = -1},
                    #endregion
                    #region Bolt Action Rifles 
                    // M700 7.62x51
                    new GunExtension() { Id = 38043, Name = "", Penetration = -1},
                    new GunExtension() { Id = 38044, Name = "", Penetration = -1},
                    // Mosin Nagant 7.62x54R
                    new GunExtension() { Id = 38119, Name = "", Penetration = -1},
                    // Mosin Infantry 7.62x54R
                    new GunExtension() { Id = 38101, Name = "", Penetration = -1},
                    new GunExtension() { Id = 38102, Name = "", Penetration = -1},
                    // Mosin Infantry Obrez 7.62x54R
                    new GunExtension() { Id = 38103, Name = "", Penetration = -1},
                    new GunExtension() { Id = 38104, Name = "", Penetration = -1},
                    #endregion
                    #region Shotguns 
                    // M870 12ga
                    new GunExtension() { Id = 38021, Name = "", Penetration = -1},
                    new GunExtension() { Id = 38022, Name = "", Penetration = -1},
                    new GunExtension() { Id = 38023, Name = "", Penetration = -1},
                    new GunExtension() { Id = 38024, Name = "", Penetration = -1},
                    new GunExtension() { Id = 38025, Name = "", Penetration = -1},
                    new GunExtension() { Id = 38026, Name = "", Penetration = -1},
                    new GunExtension() { Id = 38027, Name = "", Penetration = -1},
                    new GunExtension() { Id = 38028, Name = "", Penetration = -1},
                    // MP-153 12ga
                    new GunExtension() { Id = 38011, Name = "", Penetration = -1},
                    new GunExtension() { Id = 38012, Name = "", Penetration = -1},
                    new GunExtension() { Id = 38013, Name = "", Penetration = -1},
                    new GunExtension() { Id = 38014, Name = "", Penetration = -1},
                    new GunExtension() { Id = 38015, Name = "", Penetration = -1},
                    new GunExtension() { Id = 38016, Name = "", Penetration = -1},
                    new GunExtension() { Id = 38017, Name = "", Penetration = -1},
                    new GunExtension() { Id = 38018, Name = "", Penetration = -1},
                    // Saiga 12ga 
                    new GunExtension() { Id = 38001, Name = "", Penetration = -1},
                    new GunExtension() { Id = 38002, Name = "", Penetration = -1},
                    new GunExtension() { Id = 38003, Name = "", Penetration = -1},
                    new GunExtension() { Id = 38004, Name = "", Penetration = -1},
                    new GunExtension() { Id = 38005, Name = "", Penetration = -1},
                    new GunExtension() { Id = 38006, Name = "", Penetration = -1},
                    new GunExtension() { Id = 38007, Name = "", Penetration = -1},
                    new GunExtension() { Id = 38008, Name = "", Penetration = -1},
                    #endregion
                    #region Submachine Guns 
                    // MP5 9x19
                    new GunExtension() { Id = 37683, Name = "", Penetration = -1},
                    new GunExtension() { Id = 37685, Name = "", Penetration = -1},
                    new GunExtension() { Id = 37686, Name = "", Penetration = -1},
                    new GunExtension() { Id = 37687, Name = "", Penetration = -1},
                    // PP-19-01 9x19
                    new GunExtension() { Id = 37981, Name = "", Penetration = -1},
                    new GunExtension() { Id = 37983, Name = "", Penetration = -1},
                    new GunExtension() { Id = 37984, Name = "", Penetration = -1},
                    new GunExtension() { Id = 37987, Name = "", Penetration = -1},
                    // PP-91 9x18PM
                    new GunExtension() { Id = 37991, Name = "", Penetration = -1},
                    new GunExtension() { Id = 37992, Name = "", Penetration = -1},
                    // MP9 9x19
                    new GunExtension() { Id = 37691, Name = "", Penetration = -1},
                    // PPSH 7.62x25
                    new GunExtension() { Id = 37978, Name = "", Penetration =  18},
                    #endregion
                    #region Pistols 
                    // Colt 1911 .45 ACP
                    new GunExtension() { Id = 38094, Name = "", Penetration = -1},
                    // Colt M45A1 .45 ACP
                    new GunExtension() { Id = 38096, Name = "", Penetration = -1},
                    // Glock 17 9x19
                    new GunExtension() { Id = 38121, Name = "", Penetration = -1},
                    new GunExtension() { Id = 38122, Name = "", Penetration = -1},
                    new GunExtension() { Id = 38123, Name = "", Penetration = -1},
                    // Glock 18C 9x19
                    new GunExtension() { Id = 38124, Name = "", Penetration = -1},
                    new GunExtension() { Id = 38125, Name = "", Penetration = -1},
                    new GunExtension() { Id = 38127, Name = "", Penetration = -1},
                    new GunExtension() { Id = 38126, Name = "", Penetration = -1},
                    // MP-443 9x19
                    new GunExtension() { Id = 38081, Name = "", Penetration = -1},
                    // P226R 9x19
                    new GunExtension() { Id = 38083, Name = "", Penetration = -1},
                    new GunExtension() { Id = 38084, Name = "", Penetration = -1},
                    // PM 9x18PM
                    new GunExtension() { Id = 38079, Name = "", Penetration = -1},
                    // Rhino 60DS .357 Magnum
                    new GunExtension() { Id = 38091, Name = "", Penetration = -1},
                    // TT 7.62x25 
                    new GunExtension() { Id = 38086, Name = "", Penetration = -1},
                    #endregion
                };

                BulletCalibers = new List<Caliber>
                {
                    new Caliber
                    {
                        Name = "9x19 AP-6.3",
                        Penetration = 30,
                        FleshDamage = 48,
                        ArmorDamage = 48,
                        MagazineCalibers = new List<ushort>
                        {
                            910,
                            912,
                            913,
                            914,
                            919,
                            923,
                        }
                    },
                    new Caliber
                    {
                        Name = "9x19 Pst",
                        Penetration = 19,
                        FleshDamage = 54,
                        ArmorDamage = 33,
                        MagazineCalibers = new List<ushort>
                        {
                            909,
                            901,
                            903,
                            905,
                            911,
                            907,
                        }
                    },
                    new Caliber
                    {
                        Name = "9x18 BZT",
                        Penetration = 16,
                        FleshDamage = 50,
                        ArmorDamage = 28,
                        MagazineCalibers = new List<ushort>
                        {
                            803,
                            801,
                        }
                    },
                    new Caliber
                    {
                        Name = "9x18 PBM",
                        Penetration = 32,
                        FleshDamage = 40,
                        ArmorDamage = 30,
                        MagazineCalibers = new List<ushort>
                        {
                            804,
                            802,
                        }
                    },
                    new Caliber
                    {
                        Name = ".45 FMJ",
                        Penetration = 25,
                        FleshDamage = 76,
                        ArmorDamage = 36,
                        MagazineCalibers = new List<ushort>
                        {
                            451,
                            452,
                        }
                    },
                    new Caliber
                    {
                        Name = "7.62x25 FMJ",
                        Penetration = 11,
                        FleshDamage = 56,
                        ArmorDamage = 29,
                        MagazineCalibers = new List<ushort>
                        {
                            726,
                            725,
                        }
                    },
                    new Caliber
                    {
                        Name = ".357 Magnum",
                        Penetration = 26,
                        FleshDamage = 61,
                        ArmorDamage = 52,
                        MagazineCalibers = new List<ushort>
                        {
                            357
                        }
                    },
                    new Caliber
                    {
                        Name = ".366 AP",
                        Penetration = 40,
                        FleshDamage = 69,
                        ArmorDamage = 60,
                        MagazineCalibers = new List<ushort>
                        {
                            362
                        }
                    },
                    new Caliber
                    {
                         Name = ".366 FMJ",
                        Penetration = 73,
                        FleshDamage = 23,
                        ArmorDamage = 48,
                        MagazineCalibers = new List<ushort>
                        {
                            361
                        }
                    },
                    new Caliber
                    {
                        Name = "9x39 SPP",
                        Penetration = 43,
                        FleshDamage = 54,
                        ArmorDamage = 56,
                        MagazineCalibers = new List<ushort>
                        {
                            939,
                        }
                    },
                    new Caliber
                    {
                        Name = "9x39 SP-5",
                        Penetration = 31,
                        FleshDamage = 58,
                        ArmorDamage = 52,
                        MagazineCalibers = new List<ushort>
                        {
                            938,
                        }
                    },
                    new Caliber
                    {
                        Name = "5.56x45 M855",
                        Penetration = 28,
                        FleshDamage = 50,
                        ArmorDamage = 37,
                        MagazineCalibers = new List<ushort>
                        {
                            55601,
                            55603,
                        }
                    },
                    new Caliber
                    {
                        Name = "5.56x45 M856A1",
                        Penetration = 38,
                        FleshDamage = 47,
                        ArmorDamage = 52,
                        MagazineCalibers = new List<ushort>
                        {
                            55602,
                            55604,
                        }
                    },
                    new Caliber
                    {
                        Name = "5.45x39 BT",
                        Penetration = 39,
                        FleshDamage = 44,
                        ArmorDamage = 49,
                        MagazineCalibers = new List<ushort>
                        {
                            54502,
                        }
                    },
                    new Caliber
                    {
                        Name = "5.45x39 PS",
                        Penetration = 27,
                        FleshDamage = 50,
                        ArmorDamage = 35,
                        MagazineCalibers = new List<ushort>
                        {
                            54501,
                        }
                    },
                    new Caliber
                    {
                        Name = "7.62x39 BP",
                        Penetration = 41,
                        FleshDamage = 51,
                        ArmorDamage = 63,
                        MagazineCalibers = new List<ushort>
                        {
                            702,
                            713,
                            715,

                        }
                    },
                    new Caliber
                    {
                        Name = "7.62x39 PS",
                        Penetration = 30,
                        FleshDamage = 55,
                        ArmorDamage = 84,
                        MagazineCalibers = new List<ushort>
                        {
                            701,
                            712,
                            714,
                        }
                    },
                    new Caliber
                    {
                        Name = "7.62x54 BT",
                        Penetration = 45,
                        FleshDamage = 68,
                        ArmorDamage = 87,
                        MagazineCalibers = new List<ushort>
                        {
                            711,
                            709,
                        }
                    },
                    new Caliber
                    {
                        Name = "7.62x54 PS",
                        Penetration = 36,
                        FleshDamage = 71,
                        ArmorDamage = 32,
                        MagazineCalibers = new List<ushort>
                        {
                            710,
                            708,
                        }
                    },
                    new Caliber
                    {
                        Name = "7.62x51 M62",
                        Penetration = 43,
                        FleshDamage = 57,
                        ArmorDamage = 75,
                        MagazineCalibers = new List<ushort>
                        {
                            704,
                            706,
                            717,
                            721,
                        }
                    },
                    new Caliber
                    {
                        Name = "7.62x51 M80",
                        Penetration = 35,
                        FleshDamage = 60,
                        ArmorDamage = 66,
                        MagazineCalibers = new List<ushort>
                        {
                            703,
                            705,
                            716,
                            720,
                        }
                    },
                    new Caliber
                    {
                        Name = "7.62x51 M993",
                        Penetration = 70,
                        FleshDamage = 80,
                        ArmorDamage = 85,
                        MagazineCalibers = new List<ushort>
                        {
                            707,
                        }
                    },
                    new Caliber
                    {
                        Name = "12.7x55 PS12B",
                        Penetration = 34,
                        FleshDamage = 75,
                        ArmorDamage = 57,
                        MagazineCalibers = new List<ushort>
                        {
                            122,
                        }
                    },
                    new Caliber
                    {
                        Name = "12.7x55 PS12",
                        Penetration = 28,
                        FleshDamage = 83,
                        ArmorDamage = 60,
                        MagazineCalibers = new List<ushort>
                        {
                            121,
                        }
                    },
                    new Caliber
                    {
                        Name = "12x70 Buckshot",
                        Penetration = 4,
                        FleshDamage = 31,
                        ArmorDamage = 10,
                        MagazineCalibers = new List<ushort>
                        {
                            12707,
                            12642,
                            12641,

                        }
                    },
                    new Caliber
                    {
                        Name = "12x70 Slug",
                        Penetration = 23,
                        FleshDamage = 127,
                        ArmorDamage = 55,
                        MagazineCalibers = new List<ushort>
                        {
                            12643,
                            12644,
                            12708,
                        }
                    },
                };

                CyclableHelmets = new List<List<ItemExtension>> { };

                CyclableSights = new List<List<ItemExtension>> 
                {
                    new List<ItemExtension>
                    {
                        new ItemExtension{Name = "Black_ELCAN_1x", Id = 37820},
                        new ItemExtension{Name = "Black_ELCAN_4x", Id = 37888},
                    },
                    new List<ItemExtension>
                    {
                        new ItemExtension{Name = "Black_ELCAN_1x_Mount", Id = 37878},
                        new ItemExtension{Name = "Black_ELCAN_4x_Mount", Id = 37890},
                    },
                    new List<ItemExtension>
                    {
                        new ItemExtension{Name = "Tan_ELCAN_1x", Id = 37877},
                        new ItemExtension{Name = "Tan_ELCAN_4x", Id = 37889},
                    },
                    new List<ItemExtension>
                    {
                        new ItemExtension{Name = "Tan_ELCAN_1x_Mount", Id = 37879},
                        new ItemExtension{Name = "Tan_ELCAN_4x_Mount", Id = 37891},
                    },
                    new List<ItemExtension>
                    {
                        new ItemExtension{Name = "Hensoldt_4x", Id = 37843},
                        new ItemExtension{Name = "Hensoldt_12x", Id = 37893},
                    },
                    new List<ItemExtension>
                    {
                        new ItemExtension{Name = "Hensoldt+Romeo", Id = 37897},
                        new ItemExtension{Name = "Hensoldt_4x+Romeo", Id = 37898},
                        new ItemExtension{Name = "Hensoldt_12x+Romeo", Id = 37899},
                    },
                    new List<ItemExtension>
                    {
                        new ItemExtension{Name = "Nightforce_2x", Id = 37842},
                        new ItemExtension{Name = "Nightforce_8x", Id = 37892},
                    },
                    new List<ItemExtension>
                    {
                        new ItemExtension{Name = "Nightforce+Delta", Id = 37894},
                        new ItemExtension{Name = "Nightforce_2x+Delta", Id = 37895},
                        new ItemExtension{Name = "Nightforce_8x+Delta", Id = 37896},
                    },

                };

            }
        }

        public void updateConfig()
        {
            if (Version == "")
            {
                ArmorClasses = new List<ArmorClass>();
                HatExtensions = new List<HatExtension>();
                VestExtensions = new List<VestExtension>();
                GunExtensions = new List<GunExtension>();

                BetterArmor = new BetterArmorConfig();
                BetterArmor.Enabled = OldBetterArmor;
                BetterArmor.UseArmorClasses = OldUseArmorClasses;
                BetterArmor.ArmorDamageMultiplierOnPen = OldArmorDamageMultiplierOnPen;
                BetterArmor.PenDamgeDelta = OldPenDamgeDelta;
                BetterArmor.BetterHitZones = new BetterHitZonesConfig
                {
                    Enabled = true,
                    HatsProtectFace = false,
                    VestsProtectStomach = true,
                };
                if (gunPenValues != null)
                {
                    foreach (KeyValueElement<ushort, float> gunPenValue in gunPenValues)
                    {
                        if (gunPenValue.Key == 0)
                            continue;

                        GunExtension newGun = new GunExtension() { Id = gunPenValue.Key, Penetration = gunPenValue.Value };
                        if (!GunExtensions.Exists(x => x.Id == newGun.Id))
                        {
                            GunExtensions.Add(newGun);
                        }
                    }

                }

                if (vestsProtectingArms != null)
                {
                    foreach (KeyValueElement<ushort, float> vestValue in vestsProtectingArms)
                    {
                        if (vestValue.Key == 0)
                            continue;

                        ItemVestAsset vestAsset = (ItemVestAsset)Assets.find(EAssetType.ITEM, vestValue.Key);
                        float armor = 1;
                        if (vestAsset != null)
                        {
                            armor = vestAsset.armor * vestValue.Value;
                        }
                        VestExtension newVest = new VestExtension() { Id = vestValue.Key, ShoulderPlateLength = 0.4f, ArmorShoulderPlate = armor };
                        if (!VestExtensions.Exists(x => x.Id == newVest.Id))
                        {
                            VestExtensions.Add(newVest);
                        }
                    }

                }
                if (vestsProtectingLegs != null)
                {
                    foreach (KeyValueElement<ushort, float> vestValue in vestsProtectingLegs)
                    {
                        if (vestValue.Key == 0)
                            continue;

                        ItemVestAsset vestAsset = (ItemVestAsset)Assets.find(EAssetType.ITEM, vestValue.Key);
                        float armor = 1;
                        if (vestAsset != null)
                        {
                            armor = vestAsset.armor * vestValue.Value;
                        }
                        VestExtension newVest = new VestExtension() { Id = vestValue.Key, ThighPlateLength = 0.4f, ArmorThighPlate = armor };
                        if (!VestExtensions.Exists(x => x.Id == newVest.Id))
                        {
                            VestExtensions.Add(newVest);
                        }
                        else
                        {
                            VestExtension existingVest = VestExtensions.Find(x => x.Id == newVest.Id);
                            if (existingVest != null)
                            {
                                existingVest.ThighPlateLength = newVest.ThighPlateLength;
                                existingVest.ArmorThighPlate = newVest.ArmorThighPlate;
                            }
                        }
                    }
                }

                ArmorClasses = armorClasses;
                BoneBreakingChances = boneBreakingChances;

                Version = "1.1.0";
            }
            if (Version == "1.1.0") {
                BetterArmor.GlassesEffectKey = 5210;
                BetterArmor.HatEffectKey = 5211;
                GlassesExtensions = new List<GlassesExtension>()
                {
                    new GlassesExtension()
                    {
                        Id = 334,
                        Name = "Military Nightvision",
                        EquipEffectId = 0,
                        UnequipEffectId = 0,
                    }
                };

                Version = "1.3.0";
            }
            if (Version == "1.3.0")
            {
                foreach (GunExtension gunExtension in GunExtensions)
                {
                    Asset asset = Assets.find(EAssetType.ITEM, gunExtension.Id);
                    if (asset is ItemWeaponAsset)
                    {
                        gunExtension.FleshDamage = ((ItemWeaponAsset)asset).playerDamageMultiplier.damage;
                        gunExtension.ArmorDamage = ((ItemWeaponAsset)asset).barricadeDamage;
                        foreach (MagazineOverride magOverride in gunExtension.MagazineOverrides)
                        {
                            magOverride.FleshDamage = gunExtension.FleshDamage;
                            magOverride.ArmorDamage = gunExtension.ArmorDamage;
                        }
                    }
                }

                Version = "1.5.0";
            }
            if (Version == "1.5.0")
            {
                MovementExtension = new MovementExtension
                {
                    PushupStaminaDrain = 10,
                    ReequipGunsOnProne = true,
                };

                MaskExtensions = new List<MaskExtension>();

                Version = "1.6.0";
            }
            if(Version == "1.6.0")
            {
                BulletCalibers = new List<Caliber>
                {
                    new Caliber
                    {
                        Name = "9x39 SPP",
                        Penetration = 49,
                        FleshDamage = 64,
                        ArmorDamage = 20,
                        MagazineCalibers = new List<ushort>
                        {
                            939
                        }
                    },
                    new Caliber
                    {
                        Name = "9x39 SP-5",
                        Penetration = 33,
                        FleshDamage = 58,
                        ArmorDamage = 20,
                        MagazineCalibers = new List<ushort>
                        {
                            938
                        }
                    }
                };
                KillFeed = new KillFeed();
                Version = "1.7.0";
            }
            if (Version == "1.7.0")
            {
                BetterArmor.BetterHitZones.PlayerPenetration = new PlayerPenetrationConfig
                {
                    Enabled = true,
                    MaxPenetrations = 2,
                    Arm = new PenResistence
                    {
                        RequiredPenetration = 10,
                        PenetrationForMinReduction = 40,
                        MaxPenReduction = 0.3f,
                        MinPenReduction = 0.1f,
                    },
                    Leg = new PenResistence
                    {
                        RequiredPenetration = 10,
                        PenetrationForMinReduction = 40,
                        MaxPenReduction = 0.3f,
                        MinPenReduction = 0.1f,
                    },
                    Skull = new PenResistence
                    {
                        RequiredPenetration = 20,
                        PenetrationForMinReduction = 45,
                        MaxPenReduction = 0.4f,
                        MinPenReduction = 0.2f,
                    },
                    Spine = new PenResistence
                    {
                        RequiredPenetration = 25,
                        PenetrationForMinReduction = 50,
                        MaxPenReduction = 0.5f,
                        MinPenReduction = 0.2f,
                    },
                    Stomach = new PenResistence
                    {
                        RequiredPenetration = 15,
                        PenetrationForMinReduction = 40,
                        MaxPenReduction = 0.35f,
                        MinPenReduction = 0.15f,
                    },
                };
                Version = "1.8.0";
            }
            #region clearOldValues
            gunPenValues = null;
            vestsProtectingArms = null;
            vestsProtectingLegs = null;
            boneBreakingChances = null;
            armorClasses = null;
            #endregion

            Version = PvPRework.PluginVersion;

            PvPRework.Inst.Configuration.Save();
        }
        public void addNames()
        {
            addNames(HatExtensions);
            addNames(GlassesExtensions);
            addNames(VestExtensions);
            addNames(GunExtensions);
            addNames(MaskExtensions);

            foreach (HatExtension hat in HatExtensions)
            {
                if(hat.WhitelistedNVGs != null)
                {
                    addNames(hat.WhitelistedNVGs);
                }
            }
            foreach (List<ItemExtension> cycle in CyclableSights) 
            {
                addNames(cycle);
            }
            foreach (List<ItemExtension> cycle in CyclableHelmets)
            {
                addNames(cycle);
            }
            foreach (GunExtension gunEx in GunExtensions)
            {
                if(gunEx.MagazineOverrides != null)
                {
                    addNames(gunEx.MagazineOverrides);
                }
            }
            PvPRework.Inst.Configuration.Save();
        }
        private void addNames<T>(List<T> itemExtensions) where T : ItemExtension
        {
            foreach (T itemExtension in itemExtensions)
            {
                ItemAsset itemAsset = (ItemAsset)Assets.find(EAssetType.ITEM, itemExtension.Id);
                if (itemAsset != null)
                {
                    itemExtension.Name = itemAsset.name;
                }
            }
        }
    }
}
