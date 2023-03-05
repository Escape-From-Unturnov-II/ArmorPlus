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
        internal static void ArmorPenCheck(Player player, ELimb limb, EDeathCause cause, Vector3 direction, CSteamID oponentId, ref float damage, ref bool respectArmor, bool applyGlobalArmorMultiplier, out ExtendetHitLocation currentHitLocation)
        {
            respectArmor = false;
            bool didPenetrate = true; // set penetrate to true to avoid cancle on no vest or no helmet

            float penReducationMulti = 1;
            float armor = 1;
            UnturnedPlayer uPlayer = UnturnedPlayer.FromPlayer(player);
            UnturnedPlayer oponent = UnturnedPlayer.FromCSteamID(oponentId);

            PvPRework.Inst.getGunStats(oponent.Player, out ItemWeaponAsset oponentWeapon, out float penetration, out float fleshDamage, out float armorDamage, out Caliber caliber);

            Vector3 currentlocalHit = Vector3.zero;
            currentHitLocation = ExtendedHitLocations.getExtendetHitlocation(limb);
            bool foundHit = false;

            if (tryGetCurrentHit(uPlayer, limb, out PlayerHit currentHit))
            {
                if (tryGetLocalHitLocation(currentHit, out currentlocalHit))
                {
                    foundHit = true;
                    currentHitLocation = ExtendedHitLocations.getExtendetHitlocation(limb, currentlocalHit);
                }
                
                if (currentHit.penCount > 0)
                {
                    penetration = currentHit.penetrationOverride;
                }
            }

            // Damage / Penn Falloff
            if (oponentWeapon is ItemGunAsset)
            {
                calcDamageFallOff(oponentWeapon as ItemGunAsset, oponent.Position, uPlayer.Position, ref penetration, ref fleshDamage, ref armorDamage);
            }

            if (PvPRework.Conf.Debug)
            {
                Logger.Log($"{oponent.CharacterName} hit {uPlayer.CharacterName} in the {limb} direction: {direction} {(foundHit ? "location: [" + currentlocalHit.x + ", " + currentlocalHit.y + ", " + currentlocalHit.z + "]" : "")}");
            }

            switch (currentHitLocation)
            {
                case ExtendetHitLocation.LEFT_ARM:
                case ExtendetHitLocation.RIGHT_ARM:
                case ExtendetHitLocation.LEFT_HAND:
                case ExtendetHitLocation.RIGHT_HAND:
                    // adapt incomming damage to the damaged body part
                    fleshDamage = oponentWeapon != null ? fleshDamage * oponentWeapon.playerDamageMultiplier.arm : fleshDamage * 0.6f;
                    didPenetrate = checkArmHit(player, currentHitLocation, foundHit, currentlocalHit, ref fleshDamage, ref armorDamage, ref penetration, ref penReducationMulti, ref armor);
                    break;
                case ExtendetHitLocation.LEFT_LEG:
                case ExtendetHitLocation.RIGHT_LEG:
                case ExtendetHitLocation.LEFT_FOOT:
                case ExtendetHitLocation.RIGHT_FOOT:
                    fleshDamage = oponentWeapon != null ? fleshDamage * oponentWeapon.playerDamageMultiplier.leg : fleshDamage * 0.6f;
                    didPenetrate = checkLegHit(player, currentHitLocation, foundHit, currentlocalHit, ref fleshDamage, ref armorDamage, ref penetration, ref penReducationMulti, ref armor);
                    break;
                case ExtendetHitLocation.SKULL:
                case ExtendetHitLocation.FACE:
                case ExtendetHitLocation.EARS:
                    fleshDamage = oponentWeapon != null ? fleshDamage * oponentWeapon.playerDamageMultiplier.skull : fleshDamage * 1.1f;
                    didPenetrate = checkSkullHit(player, currentHitLocation, foundHit, currentlocalHit, ref fleshDamage, ref armorDamage, ref penetration, ref penReducationMulti, ref armor);
                    break;
                case ExtendetHitLocation.SPINE:
                case ExtendetHitLocation.STOMACH:
                    fleshDamage = oponentWeapon != null ? fleshDamage * oponentWeapon.playerDamageMultiplier.spine : fleshDamage;
                    didPenetrate = checkLegHit(player, currentHitLocation, foundHit, currentlocalHit, ref fleshDamage, ref armorDamage, ref penetration, ref penReducationMulti, ref armor);
                    break;
                default:
                    return;
            }
            if (didPenetrate && PvPRework.Conf.BetterArmor.BetterHitZones.PlayerPenetration.Enabled)
            {
                PlayerPenetration.TryPenetratePlayer(currentHit, oponent, oponentWeapon, cause, currentHitLocation, penetration);
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

                    if (i > 0 && armor > armorClasses[i - 1].Armor)
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
        private static bool checkArmHit(Player player, ExtendetHitLocation hitLocation, bool foundHit, Vector3 localHit, ref float fleshDamage, ref float armorDamage, ref float penetration, ref float penReducationMulti, ref float armor)
        {
            bool didPenetrate = true;
            float armorOverride = -1;
            ItemVestAsset vest = player.clothing.vestAsset;
            ItemShirtAsset shirt = player.clothing.shirtAsset;

            bool useOuterArmor = false;

            if (vest != null && PvPRework.Inst.vestExtensions.ContainsKey(vest.id))
            {

                PvPRework.Inst.vestExtensions.TryGetValue(vest.id, out VestExtension vestExtension);
                if (vestExtension != null && vestExtension.ShoulderPlateLength > 0)
                {
                    useOuterArmor = true;
                    armorOverride = vestExtension.ArmorShoulderPlate;
                    if (foundHit)
                    {
                        useOuterArmor = vestExtension.isProtected(hitLocation, localHit);
                    }
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
                return didPenetrate;
            }
            armor = calcVanillaArmor(player, vest, shirt, (useOuterArmor ? armorOverride : 1));
            return false;
        }
        private static bool checkLegHit(Player player, ExtendetHitLocation hitLocation, bool foundHit, Vector3 localHit, ref float fleshDamage, ref float armorDamage, ref float penetration, ref float penReducationMulti, ref float armor)
        {
            bool didPenetrate = true;
            float armorOverride = -1;
            ItemVestAsset vest = player.clothing.vestAsset;
            ItemPantsAsset pants = player.clothing.pantsAsset;

            bool useOuterArmor = false;

            if (vest != null && PvPRework.Inst.vestExtensions.ContainsKey(vest.id))
            {

                PvPRework.Inst.vestExtensions.TryGetValue(vest.id, out VestExtension vestExtension);
                if (vestExtension != null && vestExtension.ThighPlateLength > 0)
                {
                    useOuterArmor = true;
                    armorOverride = vestExtension.ArmorThighPlate;
                    if (foundHit)
                        useOuterArmor = vestExtension.isProtected(hitLocation, localHit);
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
                return didPenetrate;
            }
            armor = calcVanillaArmor(player, vest, pants, (useOuterArmor ? armorOverride : 1));
            return false;
        }
        private static bool checkBodyHit(Player player, ExtendetHitLocation hitLocation, bool foundHit, Vector3 localHit, ref float fleshDamage, ref float armorDamage, ref float penetration, ref float penReducationMulti, ref float armor)
        {
            bool didPenetrate = true;
            ItemVestAsset vest = player.clothing.vestAsset;
            ItemShirtAsset shirt = player.clothing.shirtAsset;

            bool useOuterArmor = false;
            if (hitLocation == ExtendetHitLocation.STOMACH)
            {
                if (vest != null)
                {
                    useOuterArmor = PvPRework.Conf.BetterArmor.BetterHitZones.VestsProtectStomach;

                    if (PvPRework.Inst.vestExtensions.TryGetValue(vest.id, out VestExtension vestExtension) && vestExtension != null)
                    {
                        useOuterArmor = vestExtension.ProtectStomach;
                    }
                }
            }

            if (PvPRework.Conf.BetterArmor.UseArmorClasses)
            {
                if (vest != null && useOuterArmor)
                {
                    didPenetrate = penArmor(player, vest, ref armorDamage, ref penetration, ref fleshDamage, ref penReducationMulti);
                }
                if (didPenetrate && shirt != null)
                {
                    didPenetrate = penArmor(player, shirt, ref armorDamage, ref penetration, ref fleshDamage, ref penReducationMulti);
                }
                if (didPenetrate && hitLocation == ExtendetHitLocation.STOMACH)
                {
                    if (PvPRework.Conf.Debug)
                        Logger.Log("Stomach got hit!");
                }
                return didPenetrate;
            }
            if (useOuterArmor)
            {
                armor = calcVanillaArmor(player, vest, shirt, -1);
                return false;
            }
            armor = calcVanillaArmor(player, vest, shirt);
            return false;
        }
        private static bool checkSkullHit(Player player, ExtendetHitLocation hitLocation, bool foundHit, Vector3 localHit, ref float fleshDamage, ref float armorDamage, ref float penetration, ref float penReducationMulti, ref float armor)
        {
            bool didPenetrate = true;
            float armorOverride = -1;
            ItemHatAsset hat = player.clothing.hatAsset;
            ItemMaskAsset mask = player.clothing.maskAsset;

            bool useOuterArmor = false;

            if(hat != null)
            {
                PvPRework.Inst.hatExtensions.TryGetValue(hat.id, out HatExtension hatExtension);

                if (hitLocation == ExtendetHitLocation.FACE)
                {
                    useOuterArmor = PvPRework.Conf.BetterArmor.BetterHitZones.HatsProtectFace;
                    if (hatExtension != null && hatExtension.ProtectFace)
                    {
                        useOuterArmor = true;
                        armorOverride = hatExtension.ArmorFace;
                    }
                }
                else if (hitLocation == ExtendetHitLocation.EARS)
                {
                    useOuterArmor = PvPRework.Conf.BetterArmor.BetterHitZones.HatsProtectEars;
                    if (hatExtension != null && hatExtension.ProtectEars)
                    {
                        useOuterArmor = true;
                        armorOverride = hatExtension.ArmorEars;
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
                return didPenetrate;
            }
            if (hat != null && useOuterArmor)
            {
                armor = calcVanillaArmor(player, hat, mask, armorOverride);
                return false;
            }
            armor = calcVanillaArmor(player, hat, mask);
            return false;
        }
        internal static void calcDamageFallOff(ItemGunAsset oponentGun, Vector3 pos1, Vector3 pos2, ref float penetration, ref float fleshDamage, ref float armorDamage)
        {
            //TODO: tweak 
            float distance = Vector3.Distance(pos1, pos2);

            float t = Mathf.InverseLerp(oponentGun.range * oponentGun.damageFalloffRange, oponentGun.range, distance);
            float falloffMulti = Mathf.Lerp(1f, oponentGun.damageFalloffMultiplier, t);

            penetration *= falloffMulti;
            fleshDamage *= falloffMulti;
            armorDamage *= falloffMulti;
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
