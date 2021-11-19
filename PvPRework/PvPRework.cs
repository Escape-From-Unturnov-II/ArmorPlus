using Rocket.Core.Plugins;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using Logger = Rocket.Core.Logging.Logger;

namespace PvPRework
{
    public class PvPRework : RocketPlugin<PVPReworkConfiguration>
    {
        private static readonly System.Random rand = new System.Random();

        private SerializableDictionary<ushort, float> gunPenValues = new SerializableDictionary<ushort, float>();
        private SerializableDictionary<ushort, float> vestsProtectingArms = new SerializableDictionary<ushort, float>();
        private SerializableDictionary<ushort, float> vestsProtectingLegs = new SerializableDictionary<ushort, float>();

        #region Load
        protected override void Load()
        {
            //converts lists to dictionarys to increase performance
            gunPenValues.serializableDictionary = Configuration.Instance.gunPenValues;
            vestsProtectingArms.serializableDictionary = Configuration.Instance.vestsProtectingArms;
            vestsProtectingLegs.serializableDictionary = Configuration.Instance.vestsProtectingLegs;

            Logger.Log("ArmorPus Loaded, ");
            if(Configuration.Instance.BreakLegs)
                Logger.Log("BreakLegs:\n" +String.Join(
                    "\n", Configuration.Instance.boneBreakingChances.Select(
                        x => $"{x.Limb}: Min {x.BreakChanceMin}% Max {x.BreakChanceMax}% DamageMin {x.BreakChanceDamageMin} DamageMax {x.BreakChanceDamageMax}"
                    ).ToArray()
                ) + "\n");

            if(Configuration.Instance.UseArmorClasses)
                Logger.Log("ArmorClasses:\n" + String.Join(
                    "\n", Configuration.Instance.armorClasses.Select(
                        x => $"Armor {x.Armor}: Tier {x.Tier}\n" +
                        $" PercentForNormalDamage: {x.PercentForNormalDamage} PercentForMaxDamage: {x.PercentForMaxDamage}\n" +
                        $" DamageMultiplierMin: {x.DamageMultiplierMin} DamageMultiplierNormal: {x.DamageMultiplierNormal}\n" +
                        $" MinArmorDamage: {x.MinArmorDamage} MaxArmorDamage: {x.MaxArmorDamage}\n" +
                        $" DamageToDamageArmorMin: {x.DamageToDamageArmorMin} DamageToDamageArmorMax: {x.DamageToDamageArmorMax}\n" +
                        $" StopDamageMulti: {x.StopDamageMulti} PenLossMulti: {x.PenLossMulti}"
                    ).ToArray()
                ) + "\n");
            if (gunPenValues.Count() >= 0)
            {
                Logger.Log("gunPenValues:\n" + String.Join(
                    "\n", gunPenValues.RealDictionary.Select(
                         x => $" {Assets.find(EAssetType.ITEM, x.Key)?.name ?? "> INVALID ID <"} [{x.Key}] Penetration: "+ x.Value
                    ).ToArray()
                ) + "\n");
            }
            
            if (vestsProtectingLegs != null)
            {
                Logger.Log("vestsProtectingLegs:\n" + String.Join(
                    "\n", vestsProtectingLegs.RealDictionary.Select(
                         x => $" {Assets.find(EAssetType.ITEM, x.Key)?.name ?? "> INVALID ID <"} [{x.Key}] ArmorMultiplier: " + x.Value
                    ).ToArray()
                ) + "\n");
            }
            if (vestsProtectingArms != null)
            {
                Logger.Log("vestsProtectingArms:\n" + String.Join(
                    "\n", vestsProtectingArms.RealDictionary.Select(
                         x => $" {Assets.find(EAssetType.ITEM, x.Key)?.name ?? "> INVALID ID <"} [{x.Key}] ArmorMultiplier: " + x.Value
                    ).ToArray()
                ) + "\n");
            }
            
            DamageTool.damagePlayerRequested += DamagePlayerRequested;

            if (Configuration.Instance.armorClasses.IsEmpty())
            {
                Configuration.Instance.UseArmorClasses = false;
            }
            //TODO: get mode (difficulty) and check for item/Has_Durability

            //TODO: add weight option for calcmean parameter
        }
        protected override void Unload()
        {
            DamageTool.damagePlayerRequested -= DamagePlayerRequested;
        }
        #endregion

        private void DamagePlayerRequested(ref DamagePlayerParameters parameters, ref bool shouldAllow)
        {
            shouldAllow = true;

            if (Configuration.Instance.Debug)
                Logger.Log(parameters.player.name + " was damaged in the " + parameters.limb.ToString() + " Cause: " + parameters.cause + " Times: " + parameters.times + "!");

            switch (parameters.cause)
            {
                case EDeathCause.GUN:
                case EDeathCause.MELEE:
                    if (Configuration.Instance.BetterArmor)
                        ArmorPenCheck(parameters.player, parameters.limb, parameters.killer, ref parameters.damage, ref parameters.respectArmor);
                    if (Configuration.Instance.BreakLegs)
                        BreakBoneCheck(parameters.player, parameters.limb, parameters.damage);
                    break;

                default:
                    return;
            }
                       
        }
        #region ArmorCheck
        private void ArmorPenCheck(Player player, ELimb limb, CSteamID oponentId, ref float damage, ref bool respectArmor)
        {
            respectArmor = false;

            bool didPenetrate = false;

            float armor = 1;

            UnturnedPlayer oponent = UnturnedPlayer.FromCSteamID(oponentId);
            ItemWeaponAsset oponentWeapon = null;
            
            if (oponent.Player.equipment.asset is ItemWeaponAsset)
            {
                oponentWeapon = (ItemWeaponAsset)oponent.Player.equipment.asset;
            }

            float pen = 0;
            gunPenValues.TryGetValue(oponent.Player.equipment.asset.id, out pen);

            ItemHatAsset hat = player.clothing.hatAsset;
            ItemMaskAsset mask = player.clothing.maskAsset;
            ItemVestAsset vest = player.clothing.vestAsset;
            ItemShirtAsset shirt = player.clothing.shirtAsset;
            ItemPantsAsset pants = player.clothing.pantsAsset;
            float normalizedDamage = 0;



            switch (limb)
            {
                case ELimb.LEFT_ARM:
                case ELimb.RIGHT_ARM:
                case ELimb.LEFT_HAND:
                case ELimb.RIGHT_HAND:
                    if (Configuration.Instance.UseArmorClasses)
                    {
                        if(oponentWeapon != null)
                        {
                            normalizedDamage = damage / oponentWeapon.playerDamageMultiplier.arm;
                        }
                        else
                        {
                            normalizedDamage = damage / 0.6f;
                        }
                        
                        didPenetrate = true; //set penetrate to true if no vest is equiped

                        if (vest != null && vestsProtectingArms.ContainsKey(vest.id))
                        {
                            float armorMulti = 1;
                            vestsProtectingArms.TryGetValue(vest.id, out armorMulti);
                            didPenetrate = penArmor(player, vest, ref damage, ref pen, normalizedDamage, armorMulti);
                        }

                        if (didPenetrate && shirt != null)
                        {
                            didPenetrate = penArmor(player, shirt, ref damage, ref pen, normalizedDamage);
                        }
                    }
                    else
                    {
                        float armorMulti = 1;
                        if (vest != null && vestsProtectingArms.ContainsKey(vest.id))
                            vestsProtectingArms.TryGetValue(vest.id, out armorMulti);

                        armor = calcVanillaArmor(player, vest, shirt, armorMulti);
                    }
                    break;

                case ELimb.LEFT_LEG:
                case ELimb.RIGHT_LEG:
                case ELimb.LEFT_FOOT:
                case ELimb.RIGHT_FOOT:
                    if (Configuration.Instance.UseArmorClasses)
                    {
                        if (oponentWeapon != null)
                        {
                            normalizedDamage = damage / oponentWeapon.playerDamageMultiplier.leg;
                        }
                        else
                        {
                            normalizedDamage = damage / 0.6f;
                        }
                        didPenetrate = true; //set penetrate to true if no vest is equiped

                        if (vest != null && vestsProtectingLegs.ContainsKey(vest.id))
                        {
                            float armorMulti = 1;
                            vestsProtectingLegs.TryGetValue(vest.id,out armorMulti);
                            didPenetrate = penArmor(player, vest, ref damage, ref pen, normalizedDamage, armorMulti);
                        }

                        if (didPenetrate && pants != null)
                        {
                            didPenetrate = penArmor(player, pants, ref damage, ref pen, normalizedDamage);
                        }
                    }
                    else
                    {
                        float armorMulti = 1;
                        if (vest != null && vestsProtectingLegs.ContainsKey(vest.id))
                            vestsProtectingLegs.TryGetValue(vest.id, out armorMulti);

                        armor = calcVanillaArmor(player, vest, pants, armorMulti);
                    }
                    break;

                case ELimb.SKULL:
                    if (Configuration.Instance.UseArmorClasses)
                    {
                        if (oponentWeapon != null)
                        {
                            normalizedDamage = damage / oponentWeapon.playerDamageMultiplier.skull;
                        }
                        else
                        {
                            normalizedDamage = damage / 1.1f;
                        }
                        didPenetrate = true; //set penetrate to true if no vest is equiped

                        if (hat != null)
                        {
                            didPenetrate = penArmor(player, hat, ref damage, ref pen, normalizedDamage);
                        }

                        if (didPenetrate && mask != null)
                        {
                            didPenetrate = penArmor(player, mask, ref damage, ref pen, normalizedDamage);
                        }
                    }
                    else
                    {
                        armor = calcVanillaArmor(player, hat, mask);
                    }
                    break;

                case ELimb.SPINE:
                    if (Configuration.Instance.UseArmorClasses)
                    {
                        if (oponentWeapon != null)
                        {
                            normalizedDamage = damage / oponentWeapon.playerDamageMultiplier.spine;
                        }
                        else
                        {
                            normalizedDamage = damage;
                        }
                        didPenetrate = true; //set penetrate to true if no vest is equiped

                        if (vest != null)
                        {
                            didPenetrate = penArmor(player, vest, ref damage, ref pen, normalizedDamage);
                        }

                        if (didPenetrate && shirt != null)
                        {
                            didPenetrate = penArmor(player, shirt, ref damage, ref pen, normalizedDamage);
                        }
                    }
                    else
                    {
                        armor = calcVanillaArmor(player, vest, shirt);
                    }
                    break;
                default:
                    return;
            }
            if (!Configuration.Instance.UseArmorClasses)
            {
                damage *= armor;
            }
            damage = (float)Math.Round(damage);
        }
        #endregion

        #region BoneBreackCheck
        private void BreakBoneCheck(Player player, ELimb limb, float damage)
        {
            BulletLimbDamageChance boneBreak;
            switch (limb)
            {
                case ELimb.LEFT_ARM:
                case ELimb.RIGHT_ARM:
                    damage = damage / 0.6f;
                    boneBreak = Configuration.Instance.boneBreakingChances.FirstOrDefault(x => x.Limb == "ARM");
                    break;
                case ELimb.LEFT_HAND:
                case ELimb.RIGHT_HAND:
                    damage = damage / 0.6f;
                    boneBreak = Configuration.Instance.boneBreakingChances.FirstOrDefault(x => x.Limb == "HAND");
                    break;
                case ELimb.LEFT_LEG:
                case ELimb.RIGHT_LEG:
                    damage = damage / 0.6f;
                    boneBreak = Configuration.Instance.boneBreakingChances.FirstOrDefault(x => x.Limb == "LEG");
                    break;
                case ELimb.LEFT_FOOT:
                case ELimb.RIGHT_FOOT:
                    damage = damage / 0.6f;
                    boneBreak = Configuration.Instance.boneBreakingChances.FirstOrDefault(x => x.Limb == "FOOT");
                    break;
                case ELimb.SKULL:
                    damage = damage / 1.1f;
                    boneBreak = Configuration.Instance.boneBreakingChances.FirstOrDefault(x => x.Limb == "SKULL");
                    break;
                case ELimb.SPINE:
                    boneBreak = Configuration.Instance.boneBreakingChances.FirstOrDefault(x => x.Limb == "SPINE");
                    break;
                default:
                    return;
            }

            if (boneBreak != null && boneBreak.BreakChanceDamageMax - boneBreak.BreakChanceDamageMin > 0)
            {
                //calculate damage percent in given range
                var damagePercent = (damage - boneBreak.BreakChanceDamageMin) / (boneBreak.BreakChanceDamageMax - boneBreak.BreakChanceDamageMin);
                if(damagePercent > 0) //check if enough damage was done
                {
                    //fit beween 0 and 1
                    damagePercent = damagePercent < 0 ? 0 : damagePercent > 1 ? 1 : damagePercent;
                    //calculate breakChance
                    var breakChance = damagePercent * (boneBreak.BreakChanceMax - boneBreak.BreakChanceMin) + boneBreak.BreakChanceMin;

                    if (rand.Next(0, 101) <= breakChance)
                    {
                        player.life.breakLegs();
                    }
                    if (Configuration.Instance.Debug)
                    {
                        Logger.Log("breakChance: " + breakChance + " Damage: " + damage + "!");
                    }
                }
            }
        }
        #endregion

        #region ArmorDamageCalc
        private byte calcArmorDamage(ref byte armorQuality, float reduction, bool didPenetrate)
        {
            if (armorQuality > 0)
            {
                int reductionCalc = (int)Math.Round(didPenetrate ? reduction * Configuration.Instance.ArmorDamageMultiplierOnPen : reduction);

                if (armorQuality <= reductionCalc)
                {
                    reductionCalc = armorQuality;
                }
                else if (Configuration.Instance.HasDuribility)
                {
                    armorQuality += 0x5;
                }

                return (byte)reductionCalc;
            }
            return 0;
        }

        private void damageArmor(Player player, ItemClothingAsset partToDamage, int armorClassIndex, float normalizedDamage, bool didPenetrate)
        {
            List<ArmorClass> armorClasses = Configuration.Instance.armorClasses;
            ArmorClass armorClass = armorClasses[armorClassIndex];
            PlayerClothing clothing = player.clothing;

            if (clothing != null)
            {
                float armorDamage = 0;
                if (normalizedDamage > armorClass.DamageToDamageArmorMin)
                {
                    armorDamage = armorClass.MaxArmorDamage;
                    if (normalizedDamage < armorClass.DamageToDamageArmorMax && armorClassIndex < armorClasses.Count() - 1)
                    {
                        armorDamage = calcMean(
                            armorClass.DamageToDamageArmorMin, armorClass.DamageToDamageArmorMax,
                            armorClasses[armorClassIndex].Tier, armorClasses[armorClassIndex + 1].Tier, normalizedDamage);
                    }
                }

                if (partToDamage is ItemHatAsset)
                {
                    clothing.hatQuality -= calcArmorDamage(ref clothing.hatQuality, armorDamage, didPenetrate);
                    clothing.sendUpdateHatQuality();
                }
                else if (partToDamage is ItemMaskAsset)
                {
                    clothing.maskQuality -= calcArmorDamage(ref clothing.maskQuality, armorDamage, didPenetrate);
                    clothing.sendUpdateMaskQuality();
                }
                else if (partToDamage is ItemVestAsset)
                {
                    clothing.vestQuality -= calcArmorDamage(ref clothing.vestQuality, armorDamage, didPenetrate);
                    clothing.sendUpdateVestQuality();
                }
                else if (partToDamage is ItemShirtAsset)
                {
                    clothing.shirtQuality -= calcArmorDamage(ref clothing.shirtQuality, armorDamage, didPenetrate);
                    clothing.sendUpdateShirtQuality();
                }
                else if (partToDamage is ItemPantsAsset)
                {
                    clothing.pantsQuality -= calcArmorDamage(ref clothing.pantsQuality, armorDamage, didPenetrate);
                    clothing.sendUpdatePantsQuality();
                }
            }
        }
        #endregion

        #region ArmorCalc
        private int getArmorClassIndex(float armor, out float armorTier)
        {
            armorTier = 0;
            List<ArmorClass> armorClasses = Configuration.Instance.armorClasses;

            for (int i = 0; i < armorClasses.Count(); i++)
            {
                if (armor >= armorClasses[i].Armor)
                {
                    armorTier = armorClasses[i].Tier;

                    if (armor > armorClasses[i].Armor && i > 0)
                    {
                        armorTier = calcMean(
                            armorClasses[i - 1].Armor, armorClasses[i].Armor,
                            armorClasses[i - 1].Tier, armorClasses[i].Tier, armor);

                    }
                    return i;
                }
            }
            return 0;
        }
        private float calcVanillaArmor(Player player, ItemClothingAsset top, ItemClothingAsset bottom, float armorMulty = 1)
        {
            int index = 0;
            float armorTier = 0;
            return calcItemArmor(player, top, out index, out armorTier, true, armorMulty) + calcItemArmor(player, bottom, out index, out armorTier, true);
        }

        private float calcItemArmor(Player player, ItemClothingAsset clothing, out int armorClassIndex, out float armorTier, bool vanilla = false, float armorMulty = 1)
        {
            float defaultReturn = vanilla ? 1 : 0;
            armorTier = 0;
            armorClassIndex = 0;
            float armor = 1 - (1 - clothing.armor) * armorMulty;

            if (clothing != null)
            {
                int quality = 100;
                Type clothingType = clothing.GetType();
                if (clothing is ItemHatAsset)
                {
                    quality = player.clothing.hatQuality;
                }
                else if (clothing is ItemMaskAsset)
                {
                    quality = player.clothing.maskQuality;
                }
                else if (clothing is ItemVestAsset)
                {
                    quality = player.clothing.vestQuality;
                }
                else if (clothing is ItemShirtAsset)
                {
                    quality = player.clothing.shirtQuality;
                }
                else if (clothing is ItemPantsAsset)
                {
                    quality = player.clothing.pantsQuality;
                }

                if (vanilla)
                {
                    return 1 - (1 - armor) * (int)quality / 100;
                }
                else if (quality > 0)
                {
                    armorClassIndex = getArmorClassIndex(armor, out armorTier);

                    return (121 - 5000 / (45 + (int)quality * 2)) * armorTier * 0.1f;
                }

            }
            return defaultReturn;
        }
        #endregion

        #region ArmorPenCalc
        private bool penArmor(Player player, ItemClothingAsset clothingPart, ref float damage, ref float penDamage, float normalizedDamage, float armorMulty = 1)
        {
            float penChance = 1;
            bool didPenetrate = true;
            int armorClassIndex;
            float armorTier;
            float oldPenDamage = penDamage;

            float armor = calcItemArmor(player, clothingPart, out armorClassIndex, out armorTier, false, armorMulty);
            

            if (armor > 0)
            {
                penChance = calcPenChance(armor, penDamage);

                if (penChance > rand.NextDouble())
                {
                    penDamage = calcPenDamage(penDamage, penChance, armorClassIndex);
                    damage = calcDamage(damage, penChance, armorClassIndex);
                }
                else
                {
                    didPenetrate = false;
                    damage *= Configuration.Instance.armorClasses[armorClassIndex].StopDamageMulti;
                }
            }

            damageArmor(player, clothingPart, armorClassIndex, normalizedDamage, didPenetrate);

            if (Configuration.Instance.Debug)
            {
                Logger.Log("penChance: " + penChance + " GunPenetration: " + oldPenDamage + " absDamage: " + normalizedDamage + " calcDamage: " + damage + " Armor: " + clothingPart.name + " [T:" + armorTier + " A:" + armor + "]!");

            }


            return didPenetrate;
        }

        private float calcPenDamage(float penDamage, float penChance, int armorClassIndex)
        {
            ArmorClass armorClass = Configuration.Instance.armorClasses[armorClassIndex];

            float chanceWithDelta = 1 - (1 - penChance) * Configuration.Instance.PenDamgeDelta;
            float fixedChance = chanceWithDelta > 1 ? 1 : penChance;
            float newPenDamage = penDamage * fixedChance - penDamage * armorClass.PenLossMulti;

            if (Configuration.Instance.Debug)
                Logger.Log("newPenDamage: " + newPenDamage + " oldPenDamage: " + penDamage + " penChance: " + fixedChance + " PenLossMulti: " + armorClass.PenLossMulti);
            return newPenDamage;
        }

        private float calcDamage(float damage, float penChance, int armorClassIndex)
        {
            ArmorClass armorClass = Configuration.Instance.armorClasses[armorClassIndex];

            if (penChance >= armorClass.PercentForMaxDamage)
            {
                return damage;
            }
            else if (penChance < armorClass.PercentForNormalDamage)
            {
                return damage * armorClass.DamageMultiplierMin;
            }

            return damage * calcMean(
                armorClass.PercentForNormalDamage, armorClass.PercentForMaxDamage,
                armorClass.DamageMultiplierNormal, 1, penChance);

        }
      
        /**
         * Return Penetration chance from 0-1
         */
        private float calcPenChance(float armor, float penetration)
        {
            float penCalc = armor - penetration - 15;
            return penCalc > 0 ? 0 : (penCalc * penCalc) / 100;
        }
        #endregion
        private float calcMean(float aMin, float aMax, float bMin, float bMax, float aActual)
        {
            float multi = 1 - (aActual - aMax) / (aMin - aMax);
            return bMin + multi * (bMax - bMin);
        }
    }
}
