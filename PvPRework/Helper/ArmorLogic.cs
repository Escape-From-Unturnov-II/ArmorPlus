using Rocket.Core.Logging;
using Rocket.Unturned.Player;
using SDG.Unturned;
using SpeedMann.PvPRework.Models;
using SpeedMann.PvPRework.Models.Config;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Logger = Rocket.Core.Logging.Logger;

namespace SpeedMann.PvPRework.Helper
{
    public static class ArmorLogic
    {

        #region ArmorCheck
        internal static void ArmorPenCheck(Player player, ELimb limb, EDeathCause cause, Vector3 direction, CSteamID oponentId, ref float damage, ref bool respectArmor, bool applyGlobalArmorMultiplier)
        {
            respectArmor = false;
            bool didPenetrate = true; // set penetrate to true to avoid cancle on no vest or no helmet

            float penReducationMulti = 1;
            float armor = 1;
            UnturnedPlayer uPlayer = UnturnedPlayer.FromPlayer(player);
            UnturnedPlayer oponent = UnturnedPlayer.FromCSteamID(oponentId);

            VestExtension vestExtension = null;
            HatExtension hatExtension = null;

            ItemHatAsset hat = player.clothing.hatAsset;
            ItemMaskAsset mask = player.clothing.maskAsset;
            ItemVestAsset vest = player.clothing.vestAsset;
            ItemShirtAsset shirt = player.clothing.shirtAsset;
            ItemPantsAsset pants = player.clothing.pantsAsset;

            PvPRework.Inst.getGunStats(oponent.Player, out ItemWeaponAsset oponentWeapon, out float penetration, out float fleshDamage, out float armorDamage, out Caliber caliber);

            Vector3 currentlocalHit = Vector3.zero;
            ExtendetHitLocation currentHitLocation = ExtendetHitLocations.getExtendetHitlocation(limb);
            float armorOverride = -1;
            bool foundHit = false;

            if (tryGetCurrentHit(uPlayer, limb, out PlayerHit currentHit))
            {
                foundHit = tryGetLocalHitLocation(currentHit, out currentlocalHit);
                if(currentHit.penCount > 0)
                {
                    penetration = currentHit.penetrationOverride;
                }
            }

           

            if (PvPRework.Conf.Debug)
            {
                Logger.Log($"{oponent.CharacterName} hit { uPlayer.CharacterName} in the {limb} direction: {direction} {(foundHit ? "location: [" + currentlocalHit.x + ", " + currentlocalHit.y + ", " + currentlocalHit.z + "]" : "")}");
            }
            
            switch (limb)
            {
                #region Arms
                case ELimb.LEFT_ARM:
                case ELimb.RIGHT_ARM:
                case ELimb.LEFT_HAND:
                case ELimb.RIGHT_HAND:
                    bool useOuterArmor = false;
                    fleshDamage = oponentWeapon != null ? fleshDamage * oponentWeapon.playerDamageMultiplier.arm : fleshDamage * 0.6f;

                    if (vest != null && PvPRework.Inst.vestExtensions.ContainsKey(vest.id))
                    {

                        PvPRework.Inst.vestExtensions.TryGetValue(vest.id, out vestExtension);
                        if (vestExtension != null && vestExtension.ShoulderPlateLength > 0)
                        {
                            useOuterArmor = true;
                            armorOverride = vestExtension.ArmorShoulderPlate;
                            if (foundHit)
                                useOuterArmor = vestExtension.isProtected(limb, currentlocalHit);
                        }
                    }

                    if (PvPRework.Conf.BetterArmor.UseArmorClasses)
                    {
                        if (useOuterArmor)
                        {
                            didPenetrate = penArmor(player, vest, ref armorDamage, ref penetration, ref fleshDamage, ref penReducationMulti, armorOverride);
                        }


                        if (didPenetrate && shirt != null)
                        {
                            didPenetrate = penArmor(player, shirt, ref armorDamage, ref penetration, ref fleshDamage, ref penReducationMulti);
                        }
                        if (didPenetrate && PvPRework.Conf.BetterArmor.BetterHitZones.PlayerPenetration.Enabled)
                        {
                            TryPenetratePlayer(currentHit, oponent, oponentWeapon, cause, currentHitLocation, penetration);
                        }
                    }
                    else
                    {
                        armor = calcVanillaArmor(player, vest, shirt, (useOuterArmor ? armorOverride : 1));
                    }
                    break;
                #endregion
                #region Legs
                case ELimb.LEFT_LEG:
                case ELimb.RIGHT_LEG:
                case ELimb.LEFT_FOOT:
                case ELimb.RIGHT_FOOT:
                    useOuterArmor = false;
                    fleshDamage = oponentWeapon != null ? fleshDamage * oponentWeapon.playerDamageMultiplier.leg : fleshDamage * 0.6f;

                    if (vest != null && PvPRework.Inst.vestExtensions.ContainsKey(vest.id))
                    {

                        PvPRework.Inst.vestExtensions.TryGetValue(vest.id, out vestExtension);
                        if (vestExtension != null && vestExtension.ThighPlateLength > 0)
                        {
                            useOuterArmor = true;
                            armorOverride = vestExtension.ArmorThighPlate;
                            if (foundHit)
                                useOuterArmor = vestExtension.isProtected(limb, currentlocalHit);
                        }
                    }

                    if (PvPRework.Conf.BetterArmor.UseArmorClasses)
                    {
                        if (useOuterArmor)
                        {
                            didPenetrate = penArmor(player, vest, ref armorDamage, ref penetration, ref fleshDamage, ref penReducationMulti, armorOverride);
                        }

                        if (didPenetrate && pants != null)
                        {
                            didPenetrate = penArmor(player, pants, ref armorDamage, ref penetration, ref fleshDamage, ref penReducationMulti);
                        }
                        if (didPenetrate && PvPRework.Conf.BetterArmor.BetterHitZones.PlayerPenetration.Enabled)
                        {
                            TryPenetratePlayer(currentHit, oponent, oponentWeapon, cause, currentHitLocation, penetration);
                        }
                    }
                    else
                    {
                        armor = calcVanillaArmor(player, vest, pants, (useOuterArmor ? armorOverride : 1));
                    }
                    break;
                #endregion
                #region Skull
                case ELimb.SKULL:
                    useOuterArmor = true;
                    bool faceHit = foundHit && isFaceHit(limb, currentlocalHit);
                    bool earHit = foundHit && isEarHit(limb, currentlocalHit);
                    fleshDamage = oponentWeapon != null ? fleshDamage * oponentWeapon.playerDamageMultiplier.skull : fleshDamage * 1.1f;

                    if (faceHit)
                    {
                        currentHitLocation = ExtendetHitLocation.FACE;
                        if (hat != null)
                        {
                            if (PvPRework.Inst.hatExtensions.TryGetValue(hat.id, out hatExtension))
                            {
                                if (hatExtension != null)
                                {
                                    useOuterArmor = hatExtension.ProtectFace;
                                    if (useOuterArmor)
                                    {
                                        armorOverride = hatExtension.ArmorFace;
                                    }
                                }
                            }
                            else
                            {
                                useOuterArmor = PvPRework.Conf.BetterArmor.BetterHitZones.HatsProtectFace;
                            }
                        }
                    }
                    if (earHit)
                    {
                        currentHitLocation = ExtendetHitLocation.EARS;
                        if (hat != null)
                        {
                            if (PvPRework.Inst.hatExtensions.TryGetValue(hat.id, out hatExtension))
                            {
                                if (hatExtension != null)
                                {
                                    useOuterArmor = hatExtension.ProtectEars;
                                    if (useOuterArmor)
                                    {
                                        armorOverride = hatExtension.ArmorEars;
                                    }
                                }
                            }
                            else
                            {
                                useOuterArmor = PvPRework.Conf.BetterArmor.BetterHitZones.HatsProtectEars;
                            }
                        }
                    }


                    if (PvPRework.Conf.BetterArmor.UseArmorClasses)
                    {
                        if (hat != null && useOuterArmor)
                        {
                           
                            //doesRicochet(currentHit);

                            didPenetrate = penArmor(player, hat, ref armorDamage, ref penetration, ref fleshDamage, ref penReducationMulti, armorOverride);
                        }

                        if (didPenetrate && mask != null)
                        {
                            didPenetrate = penArmor(player, mask, ref armorDamage, ref penetration, ref fleshDamage, ref penReducationMulti);
                        }
                        if (didPenetrate && PvPRework.Conf.BetterArmor.BetterHitZones.PlayerPenetration.Enabled)
                        {
                            TryPenetratePlayer(currentHit, oponent, oponentWeapon, cause, currentHitLocation, penetration);
                        }
                    }
                    else
                    {
                        if (hat != null && useOuterArmor)
                        {
                            armor = calcVanillaArmor(player, hat, mask, armorOverride);
                        }
                        else
                        {
                            armor = calcVanillaArmor(player, hat, mask);
                        }

                    }
                    break;
                #endregion
                #region Body
                case ELimb.SPINE:
                    useOuterArmor = false;
                    bool stomacheHit = foundHit && isStomachHit(limb, currentlocalHit);
                    fleshDamage = oponentWeapon != null ? fleshDamage * oponentWeapon.playerDamageMultiplier.spine : fleshDamage;

                    if (stomacheHit)
                    {
                        currentHitLocation = ExtendetHitLocation.STOMACH;
                        if (vest != null)
                        {
                            if (PvPRework.Inst.vestExtensions.ContainsKey(vest.id))
                            {
                                PvPRework.Inst.vestExtensions.TryGetValue(vest.id, out vestExtension);
                                if (vestExtension != null)
                                {
                                    useOuterArmor = vestExtension.ProtectStomach;
                                }
                            }
                            else if (PvPRework.Conf.BetterArmor.BetterHitZones.VestsProtectStomach)
                            {
                                useOuterArmor = true;
                            }
                        }
                    }

                    if (PvPRework.Conf.BetterArmor.UseArmorClasses)
                    {
                        if (vest != null && !stomacheHit || useOuterArmor)
                        {
                            didPenetrate = penArmor(player, vest, ref armorDamage, ref penetration, ref fleshDamage, ref penReducationMulti);
                        }

                        if (didPenetrate && shirt != null)
                        {
                            didPenetrate = penArmor(player, shirt, ref armorDamage, ref penetration, ref fleshDamage, ref penReducationMulti);
                        }
                        if (didPenetrate && stomacheHit)
                        {
                            if (PvPRework.Conf.Debug)
                                Logger.Log("Stomach got hit!");
                        }
                        if (didPenetrate && PvPRework.Conf.BetterArmor.BetterHitZones.PlayerPenetration.Enabled)
                        {
                            TryPenetratePlayer(currentHit, oponent, oponentWeapon, cause, currentHitLocation, penetration);
                        }
                    }
                    else
                    {
                        if (!stomacheHit || useOuterArmor)
                        {
                            armor = calcVanillaArmor(player, vest, shirt, -1);
                        }
                        else
                        {
                            armor = calcVanillaArmor(player, vest, shirt);
                        }

                    }
                    break;
                #endregion
                default:
                    return;
            }

            PvPRework.Inst.setLastHitLocation(uPlayer.CSteamID, currentHitLocation);

            if (!PvPRework.Conf.BetterArmor.UseArmorClasses)
            {
                if (applyGlobalArmorMultiplier)
                {
                    armor *= Provider.modeConfigData.Players.Armor_Multiplier;
                }
                fleshDamage = fleshDamage * armor;
            }

            damage = (float)Math.Round(fleshDamage);
        }
        #endregion

        #region ArmorDamageCalc
        internal static byte calcArmorDamage(ref byte armorQuality, float reduction, bool didPenetrate, bool counterVanillaDamage)
        {
            byte currentQuality = armorQuality;
            byte totalReduction = 0;
            if (armorQuality > 0)
            {
                int reductionCalc = (int)Math.Round(didPenetrate ? reduction * PvPRework.Conf.BetterArmor.ArmorDamageMultiplierOnPen : reduction);

                if (armorQuality <= reductionCalc)
                {
                    reductionCalc = armorQuality;
                }
                else if (counterVanillaDamage)
                {
                    armorQuality += 0x5;
                }

                totalReduction = (byte)reductionCalc;
            }
            if (PvPRework.Conf.Debug)
                Logger.Log("Armor Damage: " + totalReduction + " Armor Quality: " + currentQuality + (didPenetrate ? " PenMulti.: " + PvPRework.Conf.BetterArmor.ArmorDamageMultiplierOnPen : ""));

            return totalReduction;
        }

        internal static void damageArmor(Player player, ItemClothingAsset partToDamage, int armorClassIndex, float armorDamageIn, bool didPenetrate)
        {
            List<ArmorClass> armorClasses = PvPRework.Conf.ArmorClasses;
            ArmorClass armorClass = armorClasses[armorClassIndex];
            PlayerClothing clothing = player.clothing;

            if (clothing != null)
            {
                float trueArmorDamage = 0;
                if (armorDamageIn >= armorClass.DamageToDamageArmorMin)
                {
                    trueArmorDamage = armorClass.MaxArmorDamage;
                    if (armorDamageIn < armorClass.DamageToDamageArmorMax)
                    {
                        trueArmorDamage = PvPRework.calcMean(
                            armorClass.DamageToDamageArmorMin, armorClass.DamageToDamageArmorMax,
                            armorClass.MinArmorDamage, armorClass.MaxArmorDamage, armorDamageIn);
                    }
                }

                // round armor damage with chance
                if (trueArmorDamage % 0 > PvPRework.rand.NextDouble())
                {
                    trueArmorDamage++;
                }
                trueArmorDamage = (float)Math.Floor(trueArmorDamage);

                // damage armor
                if (partToDamage is ItemHatAsset)
                {
                    clothing.hatQuality -= calcArmorDamage(ref clothing.hatQuality, trueArmorDamage, didPenetrate, PvPRework.HasDuribility);
                    clothing.sendUpdateHatQuality();
                }
                else if (partToDamage is ItemMaskAsset)
                {
                    clothing.maskQuality -= calcArmorDamage(ref clothing.maskQuality, trueArmorDamage, didPenetrate, false);
                    clothing.sendUpdateMaskQuality();
                }
                else if (partToDamage is ItemVestAsset)
                {
                    clothing.vestQuality -= calcArmorDamage(ref clothing.vestQuality, trueArmorDamage, didPenetrate, PvPRework.HasDuribility);
                    clothing.sendUpdateVestQuality();
                }
                else if (partToDamage is ItemShirtAsset)
                {
                    clothing.shirtQuality -= calcArmorDamage(ref clothing.shirtQuality, trueArmorDamage, didPenetrate, PvPRework.HasDuribility);
                    clothing.sendUpdateShirtQuality();
                }
                else if (partToDamage is ItemPantsAsset)
                {
                    clothing.pantsQuality -= calcArmorDamage(ref clothing.pantsQuality, trueArmorDamage, didPenetrate, PvPRework.HasDuribility);
                    clothing.sendUpdatePantsQuality();
                }
            }
        }
        #endregion

        #region ArmorCalc
        internal static int getArmorClassIndex(float armor, out float armorTier)
        {

            armorTier = 0;
            List<ArmorClass> armorClasses = PvPRework.Conf.ArmorClasses;
            
            for (int i = 0; i < armorClasses.Count(); i++)
            {
                if (armor >= armorClasses[i].Armor)
                {
                    armorTier = armorClasses[i].Tier;

                    if (i > 0 && armor > armorClasses[i-1].Armor)
                    {
                        armorTier = PvPRework.calcMean(
                            armorClasses[i - 1].Armor, armorClasses[i].Armor,
                            armorClasses[i - 1].Tier, armorClasses[i].Tier, armor);

                    }
                    return i;
                }
            }

            armorTier = armorClasses[armorClasses.Count() - 1].Tier;
            return armorClasses.Count() - 1;
        }
        internal static float calcVanillaArmor(Player player, ItemClothingAsset top, ItemClothingAsset bottom, float armorOverride = 1)
        {
            int index = 0;
            float armorTier = 0;
            float calcRes = calcItemArmor(player, bottom, out index, out armorTier, true);
            if (armorOverride != 1)
            {
                calcRes += calcItemArmor(player, top, out index, out armorTier, true, armorOverride);
            }
            return calcRes;
        }

        internal static float calcItemArmor(Player player, ItemClothingAsset clothing, out int armorClassIndex, out float armorTier, bool vanilla = false, float armorOverride = -1)
        {
            float defaultReturn = vanilla ? 1 : 0;
            armorTier = 0;
            armorClassIndex = 0;
            float armor = armorOverride > 0 ? armorOverride : clothing.armor;

            if (clothing != null)
            {
                int quality = 100;
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
                if (quality > 0)
                {
                    armorClassIndex = getArmorClassIndex(armor, out armorTier);

                    return (121 - 5000 / (45 + (int)quality * 2)) * armorTier * 0.1f;
                }

            }
            return defaultReturn;
        }
        #endregion

        #region ArmorPenCalc
        internal static bool penArmor(Player player, ItemClothingAsset clothingPart, ref float armorDamage, ref float penetartion, ref float fleshDamage, ref float penReducation, float armorOverride = -1)
        {
            float penChance = 1;         
            bool didPenetrate = true;
            float oldPenetation = penetartion;
            float oldFleshDamage = fleshDamage;

            float armor = calcItemArmor(player, clothingPart, out int armorClassIndex, out float armorTier, false, armorOverride);


            if (armor > 0)
            {
                penChance = calcPenChance(armor, penetartion);

                if (penChance > PvPRework.rand.NextDouble())
                {
                    penetartion = calcPenDamage(penetartion, penChance, armorClassIndex);
                    penReducation = penReducation + oldPenetation / penetartion;
                    fleshDamage = calcDamage(fleshDamage, penChance, armorClassIndex);
                }
                else
                {
                    didPenetrate = false;
                    fleshDamage *= PvPRework.Conf.ArmorClasses[armorClassIndex].StopDamageMulti;
                }
            }

            damageArmor(player, clothingPart, armorClassIndex, armorDamage, didPenetrate);

            if (PvPRework.Conf.Debug)
            {
                Logger.Log("penChance: " + penChance + " GunPenetration: " + oldPenetation + " gunDamage: " + oldFleshDamage + " actualDamage: " + fleshDamage + " Armor: " + clothingPart.name + " [T:" + armorTier + " A:" + armor + "]!");

            }


            return didPenetrate;
        }

        internal static float calcPenDamage(float penetration, float penChance, int armorClassIndex)
        {
            ArmorClass armorClass = PvPRework.Conf.ArmorClasses[armorClassIndex];

            float chanceWithDelta = 1 - (1 - penChance) * PvPRework.Conf.BetterArmor.PenDamgeDelta;
            float fixedChance = chanceWithDelta > 1 ? 1 : penChance;
            float newPenetration = penetration * fixedChance - penetration * armorClass.PenLossMulti;

            if (PvPRework.Conf.Debug)
                Logger.Log("newPenDamage: " + newPenetration + " oldPenDamage: " + penetration + " penChance: " + fixedChance + " PenLossMulti: " + armorClass.PenLossMulti);
            return newPenetration;
        }

        internal static float calcDamage(float damage, float penChance, int armorClassIndex)
        {
            ArmorClass armorClass = PvPRework.Conf.ArmorClasses[armorClassIndex];

            if (penChance >= armorClass.PercentForMaxDamage)
            {
                return damage;
            }
            else if (penChance < armorClass.PercentForNormalDamage)
            {
                return damage * armorClass.DamageMultiplierMin;
            }

            return damage * PvPRework.calcMean(
                armorClass.PercentForNormalDamage, armorClass.PercentForMaxDamage,
                armorClass.DamageMultiplierNormal, 1, penChance);

        }

        /**
         * Return Penetration chance from 0-1
         */
        internal static float calcPenChance(float armor, float penetration)
        {
            float penCalc = armor - penetration - 15;
            return penCalc > 0 ? 0 : (penCalc * penCalc) / 100;
        }
        #endregion
        internal static void TryPenetratePlayer(PlayerHit currentHit, UnturnedPlayer shooter, ItemWeaponAsset weapon, EDeathCause cause, ExtendetHitLocation hitBodypart, float penetration)
        {
            if(currentHit == null || shooter == null)
            {
                Logger.LogError($"Could not find shooter or hit not found");
                return;
            }
            PenResistence penResistance = null;
            switch (hitBodypart)
            {
                case ExtendetHitLocation.EARS:
                case ExtendetHitLocation.FACE:
                case ExtendetHitLocation.SKULL:
                    penResistance = PvPRework.Conf.BetterArmor.BetterHitZones.PlayerPenetration.Skull;
                    break;
                case ExtendetHitLocation.RIGHT_ARM:
                case ExtendetHitLocation.LEFT_ARM:
                    penResistance = PvPRework.Conf.BetterArmor.BetterHitZones.PlayerPenetration.Arm;
                    break;
                case ExtendetHitLocation.RIGHT_LEG:
                case ExtendetHitLocation.LEFT_LEG:
                    penResistance = PvPRework.Conf.BetterArmor.BetterHitZones.PlayerPenetration.Leg;
                    break;
                case ExtendetHitLocation.SPINE:
                    penResistance = PvPRework.Conf.BetterArmor.BetterHitZones.PlayerPenetration.Spine;
                    break;
                case ExtendetHitLocation.STOMACH:
                    penResistance = PvPRework.Conf.BetterArmor.BetterHitZones.PlayerPenetration.Stomach;
                    break;
            }
            
            if (penResistance == null)
            {
                Logger.LogError($"Could not find penResistance for {hitBodypart}");
                return;
            }

            if (currentHit.penCount <= PvPRework.Conf.BetterArmor.BetterHitZones.PlayerPenetration.MaxPenetrations && penetration >= penResistance.RequiredPenetration)
            {

                Vector3 newStartpoint = currentHit.imputInfo.point + (currentHit.imputInfo.direction.normalized * 0.1f);
                RaycastInfo info = DamageTool.raycast(new Ray(newStartpoint, currentHit.imputInfo.direction), 512f, RayMasks.DAMAGE_CLIENT, null);

                if (info?.player != null)
                {

                    // calc pen reduction
                    float penReduction = PvPRework.calcMean(penResistance.RequiredPenetration, penResistance.PenetrationForMinReduction, penResistance.MaxPenReduction, penResistance.MinPenReduction, penetration);
                    penReduction = penReduction > 1 ? 1 : penReduction < 0 ? 0 : penReduction;
                    float remainingPenetration = penReduction * penetration;


                    Logger.Log($"Penetrated and hit {info.player.name} in the {info.limb} penReduction: {penReduction}");
                    PvPRework.playerHits.Add(new PlayerHit(new InputInfo
                    {
                        type = ERaycastInfoType.PLAYER,
                        player = info.player,
                        transform = info.transform,
                        point = info.point,
                        limb = info.limb,
                    }, currentHit.penCount+1, remainingPenetration));

                    DamagePlayerParameters damageparam = new DamagePlayerParameters
                    {
                        player = info.player,
                        cause = cause,
                        limb = info.limb,
                        direction = currentHit.imputInfo.direction,
                        damage = weapon.playerDamageMultiplier.multiply(info.limb),
                        times = 1,
                        killer = shooter.CSteamID,
                    };
                    PvPRework.Inst.playerPenetrations.Add(damageparam);
                }
            }
        }
        internal static bool doesRicochet(PlayerHit currentHit)
        {
            Transform skeleton = currentHit?.imputInfo?.player?.transform?.GetChild(0)?.GetChild(0);
            float radius = 1f;
            Vector3 correction = Vector3.zero;
            Vector3 center = Vector3.zero;

            switch (currentHit.imputInfo.limb)
            {
                case ELimb.SKULL:
                    radius = 0.35f;
                    correction = new Vector3(0.8f, 0.36f, 0.36f);
                    // create center defined as circle with radius
                    center = new Vector3(radius, radius, radius);
                    break;
                case ELimb.SPINE:
                case ELimb.LEFT_ARM:
                case ELimb.RIGHT_ARM:
                case ELimb.LEFT_LEG:
                case ELimb.RIGHT_LEG:
                    Logger.LogWarning($"Ricochet is not implemented for {currentHit.imputInfo.limb}");
                    return false;
            }
            
            
            
            if (skeleton != null && getLocalPoint(skeleton, currentHit.imputInfo.point, currentHit.imputInfo.limb, out Vector3 localPoint, out Transform limbTransform))
            {
                // correct point to enable simpler center
                Vector3 correctedPoint = new Vector3(localPoint.x + correction.x, localPoint.y + correction.y, localPoint.z + correction.z);
                Vector3 extendedHitPoint = localPoint + limbTransform.TransformDirection(currentHit.imputInfo.direction.normalized) * Vector3.Distance(correctedPoint, center);

                float distance = Vector3.Distance(center, extendedHitPoint);
                if (PvPRework.Conf.Debug)
                {
                    Logger.Log($"center: {center} extendedhitLocal: {extendedHitPoint} distance: {distance}");
                }
                
                return true;
            }
            return false;
        }
        #region Helper Functions
        private static bool tryGetLocalHitLocation(PlayerHit hit, out Vector3 localPoint)
        {
            localPoint = Vector3.zero;
            Transform skeleton = hit?.imputInfo?.player?.transform?.GetChild(0)?.GetChild(0);
            if (skeleton != null && getLocalPoint(skeleton, hit.imputInfo.point, hit.imputInfo.limb, out localPoint, out Transform limbTransform))
            {
                return true;
            }
            Logger.LogError("Error in BetterHitZones: No localPoint found for " + hit.imputInfo.limb + " of " + hit.imputInfo.transform.name);
            return false;
        }
        private static bool tryGetCurrentHit(UnturnedPlayer uPlayer, ELimb limb, out PlayerHit playerHit)
        {
            playerHit = null;
            if (PvPRework.Conf.BetterArmor.BetterHitZones.Enabled)
            {
                foreach (PlayerHit hit in PvPRework.playerHits)
                {
                    if (hit.isCorrectHit(uPlayer.CSteamID, limb))
                    {
                        playerHit = hit;
                        PvPRework.playerHits.Remove(hit);
                        return true;
                    }
                }
                Logger.LogError("BetterHitZones is enabled but no hit was found for Player: " + uPlayer.CharacterName + " at: " + limb);
            }
            return false;
        }

        public static bool isEarHit(ELimb limb, Vector3 localPoint)
        {
            if (limb == ELimb.SKULL)
            {
                bool earsHit = localPoint.x > -0.5 && (localPoint.y >= 0.2 || localPoint.y <= -0.2) && localPoint.z > -0.05;
                if (earsHit && PvPRework.Conf.Debug)
                {
                    Logger.Log("Raycast hit in the Ears");
                }
                return earsHit;
            }
            return false;
        }
        public static bool isFaceHit(ELimb limb, Vector3 localPoint)
        {
            if (limb == ELimb.SKULL)
            {
                bool faceHit = localPoint.x > -0.55 && localPoint.z >= 0.2;
                if (faceHit && PvPRework.Conf.Debug)
                {
                    Logger.Log("Raycast hit in the Face");
                }
                return faceHit;
            }
            return false;
        }
        public static bool isStomachHit(ELimb limb, Vector3 localPoint)
        {
            if (limb == ELimb.SPINE)
            {
                bool stomachHit = localPoint.x > -0.23;
                if (stomachHit && PvPRework.Conf.Debug)
                {
                    Logger.Log("Raycast hit in the Stomach");
                }
                return stomachHit;
            }
            return false;
        }
        private static bool getLocalPoint(Transform skeleton, Vector3 point, ELimb limb, out Vector3 localPoint, out Transform limbTransform)
        {
            limbTransform = null;

            switch (limb)
            {
                case ELimb.SKULL:
                    limbTransform = skeleton.Find("Spine").Find("Skull");
                    break;
                case ELimb.SPINE:
                    limbTransform = skeleton.Find("Spine");
                    break;
                case ELimb.LEFT_ARM:
                    limbTransform = skeleton.Find("Spine").Find("Left_Shoulder").Find("Left_Arm");
                    break;
                case ELimb.RIGHT_ARM:
                    limbTransform = skeleton.Find("Spine").Find("Right_Shoulder").Find("Right_Arm");
                    break;
                case ELimb.LEFT_LEG:
                    limbTransform = skeleton.Find("Left_Hip").Find("Left_Leg");
                    break;
                case ELimb.RIGHT_LEG:
                    limbTransform = skeleton.Find("Right_Hip").Find("Right_Leg");
                    break;
            }
            if (limbTransform != null)
            {
                localPoint = limbTransform.InverseTransformPoint(point);
                return true;
            }
            localPoint = Vector3.zero;
            return false;
        }
        #endregion
    }
}
