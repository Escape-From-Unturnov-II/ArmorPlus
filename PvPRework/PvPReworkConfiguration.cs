using Rocket.API;
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

        public string Version; //auto updating Version Number
        public bool Debug; //to display debug information on server console
        public bool BreakLegs; //if bullets should be able to break legs
        public BetterArmorConfig BetterArmor;

        public List<ArmorClass> ArmorClasses;
        public List<BulletLimbDamageChance> BoneBreakingChances;
        public List<HatExtension> HatExtensions;
        public List<GlassesExtension> GlassesExtensions;
        public List<VestExtension> VestExtensions;
        public List<GunExtension> GunExtensions;

        public void LoadDefaults()
        {
            Version = PvPRework.PluginVersion;
            Debug = true;
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
                },
            };
            BoneBreakingChances = new List<BulletLimbDamageChance>{
                new BulletLimbDamageChance{ Limb = "LEG", BreakChanceMin = 10, BreakChanceMax = 95, BreakChanceDamageMin = 10, BreakChanceDamageMax = 50},
                new BulletLimbDamageChance{ Limb = "FOOT", BreakChanceMin = 10, BreakChanceMax = 95, BreakChanceDamageMin = 10, BreakChanceDamageMax = 50},
                new BulletLimbDamageChance{ Limb = "ARM", BreakChanceMin = 0, BreakChanceMax = 0, BreakChanceDamageMin = 0, BreakChanceDamageMax = 0},
                new BulletLimbDamageChance{ Limb = "HAND", BreakChanceMin = 0, BreakChanceMax = 0, BreakChanceDamageMin = 0, BreakChanceDamageMax = 0},
                new BulletLimbDamageChance{ Limb = "SKULL", BreakChanceMin = 0, BreakChanceMax = 0, BreakChanceDamageMin = 0, BreakChanceDamageMax = 0},
                new BulletLimbDamageChance{ Limb = "SPINE", BreakChanceMin = 0, BreakChanceMax = 0, BreakChanceDamageMin = 0, BreakChanceDamageMax = 0}
            };

            ArmorClasses = new List<ArmorClass>{
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
                    gunPenValues = null;
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
                    vestsProtectingArms = null;
                }
                if (vestsProtectingLegs != null)
                {
                    foreach (KeyValueElement<ushort, float> vestValue in vestsProtectingLegs)
                    {
                        if(vestValue.Key == 0)
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
                    vestsProtectingLegs = null;
                }

                ArmorClasses = armorClasses;
                armorClasses = null;
                BoneBreakingChances = boneBreakingChances;
                boneBreakingChances = null;
                Version = "1.1.0";
            }
            if(Version == "1.1.0"){
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

                Version = "1.2.0";
            }


            Version = PvPRework.PluginVersion;

            PvPRework.Inst.Configuration.Save();
        }
        public void addNames()
        {
            addNames(HatExtensions);
            addNames(GlassesExtensions);
            addNames(VestExtensions);
            addNames(GunExtensions);
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
