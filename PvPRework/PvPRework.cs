using HarmonyLib;
using Rocket.Core.Plugins;
using Rocket.Unturned;
using Rocket.Unturned.Events;
using Rocket.Unturned.Player;
using SDG.Unturned;
using SpeedMann.PvPRework.Models.Config;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Logger = Rocket.Core.Logging.Logger;

namespace SpeedMann.PvPRework
{
    public class PvPRework : RocketPlugin<PVPReworkConfiguration>
    {
        public static string PluginVersion = "1.2.0";
        public static PvPRework Inst;
        public static PVPReworkConfiguration Conf;
        private static readonly System.Random rand = new System.Random();
        private static bool ModsLoaded = false;

        private static TimeSpan PlayerHitMaxAge = new TimeSpan(0,0,2);

        private static bool HasDuribility;

        private Dictionary<ushort, GunExtension> gunExtensions;
        private Dictionary<ushort, VestExtension> vestExtensions;
        private Dictionary<ushort, HatExtension> hatExtensions;
        private Dictionary<ushort, GlassesExtension> glassesExtensions;
        private List<PlayerHit> playerHits;

        #region Load
        protected override void Load()
        {
            Inst = this;
            Conf = Configuration.Instance;

            Conf.updateConfig();

            playerHits = new List<PlayerHit>();
            //converts lists to dictionarys to increase performance
            gunExtensions = createDictionaryFromItemExtensions(Conf.GunExtensions);
            vestExtensions = createDictionaryFromItemExtensions(Conf.VestExtensions);
            hatExtensions = createDictionaryFromItemExtensions(Conf.HatExtensions);
            glassesExtensions = createDictionaryFromItemExtensions(Conf.GlassesExtensions);

            UnturnedPatches.Init();

            Level.onPreLevelLoaded += OnPreLevelLoaded;
            DamageTool.damagePlayerRequested += DamagePlayerRequested;

            if (Conf.BetterArmor.BetterHitZones.Enabled)
                UnturnedPatches.OnPostGetInput += OnGetInput;

            // UI
            U.Events.OnPlayerConnected += OnPlayerConnected;
            UnturnedPatches.OnPreChangeHat += OnPreHatChanged;
            UnturnedPatches.OnPreVisionChanged += OnVisionChanged;
            UnturnedPlayerEvents.OnPlayerDeath += OnPlayerDeath;


            if (Conf.ArmorClasses == null || Conf.ArmorClasses.IsEmpty())
            {
                Conf.BetterArmor.UseArmorClasses = false;
            }
            HasDuribility = Provider.modeConfigData.Items.Has_Durability;

            if (ModsLoaded)
            {
                printPluginInfo();
            }
            /* TODO: Add Plugin for item storage update (clothing keep items)
             * Use metadate to save storage id (check if possible)
             * Use /vault plugin storage to open cloting from ground or inventory (check if mod is possible)
             */
        }
        protected override void Unload()
        {
            Level.onPreLevelLoaded -= OnPreLevelLoaded;
            DamageTool.damagePlayerRequested -= DamagePlayerRequested;

            if (Conf.BetterArmor.BetterHitZones.Enabled)
                UnturnedPatches.OnPostGetInput -= OnGetInput;

            //UI
            U.Events.OnPlayerConnected -= OnPlayerConnected;
            UnturnedPatches.OnPreChangeHat -= OnPreHatChanged;
            UnturnedPlayerEvents.OnPlayerDeath -= OnPlayerDeath;
        }
        private void OnPreLevelLoaded(int level)
        {
            Conf.addNames();
            printPluginInfo();
        }
        #endregion

        private void OnPlayerConnected(UnturnedPlayer player)
        {
            StartCoroutine(waiter(player));
        }
        private void OnPlayerDeath(UnturnedPlayer player, EDeathCause cause, ELimb limb, CSteamID murderer)
        {
            EffectController.checkClothingEffect(hatExtensions, player, 0);
            EffectController.checkClothingEffect(glassesExtensions, player, 0);
        }
        private void OnPreHatChanged(Player player, ushort newHatId)
        {
            EffectController.checkClothingEffect(hatExtensions, UnturnedPlayer.FromPlayer(player), newHatId);
        }
        private void OnVisionChanged(Player player, ushort glassesId, bool activate)
        {
            if (activate)
            {
                EffectController.checkClothingEffect(glassesExtensions, UnturnedPlayer.FromPlayer(player), glassesId);
            }
            else
            {
                EffectController.checkClothingEffect(glassesExtensions, UnturnedPlayer.FromPlayer(player), 0);
            }
            
        }

        private void DamagePlayerRequested(ref DamagePlayerParameters parameters, ref bool shouldAllow)
        {

            if (Conf.Debug && !Conf.BetterArmor.Enabled)
                Logger.Log(parameters.player.name + " was damaged in the " + parameters.limb.ToString() + " Cause: " + parameters.cause + " Times: " + parameters.times + "!");

            switch (parameters.cause)
            {
                case EDeathCause.GUN:
                case EDeathCause.MELEE:
                    if (Conf.BetterArmor.Enabled)
                        ArmorPenCheck(parameters.player, parameters.limb, parameters.killer, ref parameters.damage, ref parameters.respectArmor, parameters.applyGlobalArmorMultiplier);
                    if (Conf.BreakLegs)
                        BreakBoneCheck(parameters.player, parameters.limb, parameters.damage);
                    break;

                default:
                    return;
            }
                       
        }
        private void OnGetInput(ref InputInfo inputInfo)
        {
            if (inputInfo != null && inputInfo.type == ERaycastInfoType.PLAYER && inputInfo.player != null && inputInfo.transform != null)
            {
                while (playerHits.Count > 0)
                {
                    if (playerHits[0].isOlderThan(PlayerHitMaxAge))
                    {
                        InputInfo removedHit = playerHits[0].imputInfo;
                        playerHits.RemoveAt(0);
                        if (Conf.Debug)
                        {
                            Logger.Log("PlayerHit timedout: " + removedHit.player.name +" in the "+ removedHit.limb);
                        }
                    }
                    else
                        break;
                }

                playerHits.Add(new PlayerHit(inputInfo));
            }
        }
        #region ArmorCheck
        private void ArmorPenCheck(Player player, ELimb limb, CSteamID oponentId, ref float damage, ref bool respectArmor, bool applyGlobalArmorMultiplier)
        {
            respectArmor = false;
            bool didPenetrate = true; // set penetrate to true to avoid cancle on no vest or no helmet

            float armor = 1;
            UnturnedPlayer uPlayer = UnturnedPlayer.FromPlayer(player);
            UnturnedPlayer oponent = UnturnedPlayer.FromCSteamID(oponentId);
            ItemWeaponAsset oponentWeapon = null;
            
            if (oponent.Player.equipment.asset is ItemWeaponAsset)
            {
                oponentWeapon = (ItemWeaponAsset)oponent.Player.equipment.asset;
            }

            VestExtension vestExtension = null;
            HatExtension hatExtension = null;
            GunExtension gunExtension = null;
            gunExtensions.TryGetValue(oponent.Player.equipment.asset.id, out gunExtension);
            float pen = gunExtension != null ? gunExtension.Penetration : 0;

            ItemHatAsset hat = player.clothing.hatAsset;
            ItemMaskAsset mask = player.clothing.maskAsset;
            ItemVestAsset vest = player.clothing.vestAsset;
            ItemShirtAsset shirt = player.clothing.shirtAsset;
            ItemPantsAsset pants = player.clothing.pantsAsset;

            float normalizedDamage = 0;

            Vector3 currentlocalHit;
            float armorOverride = -1;
            bool foundHit = tryGetCurrentHit(uPlayer, limb, out currentlocalHit);

            if(Conf.Debug)
            {
                Logger.Log(oponent.CharacterName +" hit " + uPlayer.CharacterName + " in the " + limb + (foundHit ? " ["+currentlocalHit.x+", " + currentlocalHit.y+", "+currentlocalHit.z+"]":""));
            }

            switch (limb)
            {
                #region Arms
                case ELimb.LEFT_ARM:
                case ELimb.RIGHT_ARM:
                case ELimb.LEFT_HAND:
                case ELimb.RIGHT_HAND:
                    bool useOuterArmor = false;
                    if (vest != null && vestExtensions.ContainsKey(vest.id))
                    {
                        
                        vestExtensions.TryGetValue(vest.id, out vestExtension);
                        if (vestExtension != null && vestExtension.ShoulderPlateLength > 0)
                        {
                            useOuterArmor = true;
                            armorOverride = vestExtension.ArmorShoulderPlate;
                            if (foundHit)
                                useOuterArmor = vestExtension.isProtected(limb, currentlocalHit);
                        }
                    }

                    if (Conf.BetterArmor.UseArmorClasses)
                    {
                        normalizedDamage = oponentWeapon != null ? damage / oponentWeapon.playerDamageMultiplier.arm : damage / 0.6f;
                        if (useOuterArmor)
                        {
                            didPenetrate = penArmor(player, vest, ref damage, ref pen, normalizedDamage, armorOverride);
                        }
                        

                        if (didPenetrate && shirt != null)
                        {
                            didPenetrate = penArmor(player, shirt, ref damage, ref pen, normalizedDamage);
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
                    if (vest != null && vestExtensions.ContainsKey(vest.id))
                    {

                        vestExtensions.TryGetValue(vest.id, out vestExtension);
                        if (vestExtension != null && vestExtension.ThighPlateLength > 0)
                        {
                            useOuterArmor = true;
                            armorOverride = vestExtension.ArmorThighPlate;
                            if (foundHit)
                                useOuterArmor = vestExtension.isProtected(limb, currentlocalHit);
                        }
                    }

                    if (Conf.BetterArmor.UseArmorClasses)
                    {

                        normalizedDamage = oponentWeapon != null ? damage / oponentWeapon.playerDamageMultiplier.leg : damage / 0.6f;

                        if (useOuterArmor)
                        {
                            didPenetrate = penArmor(player, vest, ref damage, ref pen, normalizedDamage, armorOverride);
                        }

                        if (didPenetrate && pants != null)
                        {
                            didPenetrate = penArmor(player, pants, ref damage, ref pen, normalizedDamage);
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
                    useOuterArmor = false;
                    bool faceHit = foundHit && isFaceHit(limb, currentlocalHit);

                    if (faceHit && hat != null)
                    {
                        if (hatExtensions.ContainsKey(hat.id))
                        {
                            hatExtensions.TryGetValue(hat.id, out hatExtension);
                            if (hatExtension != null && hatExtension.ProtectFace)
                            {
                                armorOverride = hatExtension.ArmorFace;
                                useOuterArmor = hatExtension.ProtectFace;
                            }
                        }
                        else if (Conf.BetterArmor.BetterHitZones.HatsProtectFace)
                        {
                            useOuterArmor = true;
                        }
                    }

                    if (Conf.BetterArmor.UseArmorClasses)
                    {
                        normalizedDamage = oponentWeapon != null ? damage / oponentWeapon.playerDamageMultiplier.skull : damage / 1.1f;

                        if (hat != null && !faceHit || useOuterArmor)
                        {
                            didPenetrate = penArmor(player, hat, ref damage, ref pen, normalizedDamage, armorOverride);
                        }

                        if (didPenetrate && mask != null)
                        {
                            didPenetrate = penArmor(player, mask, ref damage, ref pen, normalizedDamage);
                        }
                    }
                    else
                    {
                        if (!faceHit || useOuterArmor)
                        {
                            armor = calcVanillaArmor(player, hat, mask, 1);
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
                    
                    if (stomacheHit && vest != null)
                    {
                        if (vestExtensions.ContainsKey(vest.id))
                        {
                            vestExtensions.TryGetValue(vest.id, out vestExtension);
                            if (vestExtension != null)
                            {
                                useOuterArmor = vestExtension.ProtectStomach;
                            }
                        }
                        else if (Conf.BetterArmor.BetterHitZones.VestsProtectStomach)
                        {
                            useOuterArmor = true;
                        }
                    }

                    if (Conf.BetterArmor.UseArmorClasses)
                    {
                        normalizedDamage = oponentWeapon != null ? damage / oponentWeapon.playerDamageMultiplier.spine : damage;

                        if (vest != null && !stomacheHit || useOuterArmor)
                        {
                            didPenetrate = penArmor(player, vest, ref damage, ref pen, normalizedDamage);
                        }

                        if (didPenetrate && shirt != null)
                        {
                            didPenetrate = penArmor(player, shirt, ref damage, ref pen, normalizedDamage);
                        }
                        if (didPenetrate && stomacheHit)
                        {
                            if(Conf.Debug)
                                Logger.Log("Stomach got hit!");
                        }
                    }
                    else
                    {
                        if(!stomacheHit || useOuterArmor)
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
            if (!Conf.BetterArmor.UseArmorClasses)
            {
                if (applyGlobalArmorMultiplier)
                {
                    armor *= Provider.modeConfigData.Players.Armor_Multiplier;
                }
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
                    boneBreak = Conf.BoneBreakingChances.FirstOrDefault(x => x.Limb == "ARM");
                    break;
                case ELimb.LEFT_HAND:
                case ELimb.RIGHT_HAND:
                    damage = damage / 0.6f;
                    boneBreak = Conf.BoneBreakingChances.FirstOrDefault(x => x.Limb == "HAND");
                    break;
                case ELimb.LEFT_LEG:
                case ELimb.RIGHT_LEG:
                    damage = damage / 0.6f;
                    boneBreak = Conf.BoneBreakingChances.FirstOrDefault(x => x.Limb == "LEG");
                    break;
                case ELimb.LEFT_FOOT:
                case ELimb.RIGHT_FOOT:
                    damage = damage / 0.6f;
                    boneBreak = Conf.BoneBreakingChances.FirstOrDefault(x => x.Limb == "FOOT");
                    break;
                case ELimb.SKULL:
                    damage = damage / 1.1f;
                    boneBreak = Conf.BoneBreakingChances.FirstOrDefault(x => x.Limb == "SKULL");
                    break;
                case ELimb.SPINE:
                    boneBreak = Conf.BoneBreakingChances.FirstOrDefault(x => x.Limb == "SPINE");
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
                    if (Conf.Debug)
                    {
                        Logger.Log("breakChance: " + breakChance + " Damage: " + damage + "!");
                    }
                }
            }
        }
        #endregion

        #region ArmorDamageCalc
        private byte calcArmorDamage(ref byte armorQuality, float reduction, bool didPenetrate, bool counterVanillaDamage)
        {
            byte currentQuality = armorQuality;
            byte totalReduction = 0;
            if (armorQuality > 0)
            {
                int reductionCalc = (int)Math.Round(didPenetrate ? reduction * Conf.BetterArmor.ArmorDamageMultiplierOnPen : reduction);

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
            if (Conf.Debug)
                Logger.Log("Armor Damage: " + totalReduction + " Armor Quality: " + currentQuality + (didPenetrate ? " PenMulti.: " + Conf.BetterArmor.ArmorDamageMultiplierOnPen:""));

            return totalReduction;
        }

        private void damageArmor(Player player, ItemClothingAsset partToDamage, int armorClassIndex, float normalizedDamage, bool didPenetrate)
        {
            List<ArmorClass> armorClasses = Conf.ArmorClasses;
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
                    clothing.hatQuality -= calcArmorDamage(ref clothing.hatQuality, armorDamage, didPenetrate, HasDuribility);
                    clothing.sendUpdateHatQuality();
                }
                else if (partToDamage is ItemMaskAsset)
                {
                    clothing.maskQuality -= calcArmorDamage(ref clothing.maskQuality, armorDamage, didPenetrate, false);
                    clothing.sendUpdateMaskQuality();
                }
                else if (partToDamage is ItemVestAsset)
                {
                    clothing.vestQuality -= calcArmorDamage(ref clothing.vestQuality, armorDamage, didPenetrate, HasDuribility);
                    clothing.sendUpdateVestQuality();
                }
                else if (partToDamage is ItemShirtAsset)
                {
                    clothing.shirtQuality -= calcArmorDamage(ref clothing.shirtQuality, armorDamage, didPenetrate, HasDuribility);
                    clothing.sendUpdateShirtQuality();
                }
                else if (partToDamage is ItemPantsAsset)
                {
                    clothing.pantsQuality -= calcArmorDamage(ref clothing.pantsQuality, armorDamage, didPenetrate, HasDuribility);
                    clothing.sendUpdatePantsQuality();
                }
            }
        }
        #endregion

        #region ArmorCalc
        private int getArmorClassIndex(float armor, out float armorTier)
        {
            armorTier = 0;
            List<ArmorClass> armorClasses = Conf.ArmorClasses;

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
        private float calcVanillaArmor(Player player, ItemClothingAsset top, ItemClothingAsset bottom, float armorOverride = 1)
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

        private float calcItemArmor(Player player, ItemClothingAsset clothing, out int armorClassIndex, out float armorTier, bool vanilla = false, float armorOverride = -1)
        {
            float defaultReturn = vanilla ? 1 : 0;
            armorTier = 0;
            armorClassIndex = 0;
            float armor = armorOverride > 0 ? armorOverride : clothing.armor;

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
        private bool penArmor(Player player, ItemClothingAsset clothingPart, ref float damage, ref float penDamage, float normalizedDamage, float armorOverride = -1)
        {
            float penChance = 1;
            bool didPenetrate = true;
            int armorClassIndex;
            float armorTier;
            float oldPenDamage = penDamage;

            float armor = calcItemArmor(player, clothingPart, out armorClassIndex, out armorTier, false, armorOverride);
            

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
                    damage *= Conf.ArmorClasses[armorClassIndex].StopDamageMulti;
                }
            }

            damageArmor(player, clothingPart, armorClassIndex, normalizedDamage, didPenetrate);

            if (Conf.Debug)
            {
                Logger.Log("penChance: " + penChance + " GunPenetration: " + oldPenDamage + " absDamage: " + normalizedDamage + " calcDamage: " + damage + " Armor: " + clothingPart.name + " [T:" + armorTier + " A:" + armor + "]!");

            }


            return didPenetrate;
        }

        private float calcPenDamage(float penDamage, float penChance, int armorClassIndex)
        {
            ArmorClass armorClass = Conf.ArmorClasses[armorClassIndex];

            float chanceWithDelta = 1 - (1 - penChance) * Conf.BetterArmor.PenDamgeDelta;
            float fixedChance = chanceWithDelta > 1 ? 1 : penChance;
            float newPenDamage = penDamage * fixedChance - penDamage * armorClass.PenLossMulti;

            if (Conf.Debug)
                Logger.Log("newPenDamage: " + newPenDamage + " oldPenDamage: " + penDamage + " penChance: " + fixedChance + " PenLossMulti: " + armorClass.PenLossMulti);
            return newPenDamage;
        }

        private float calcDamage(float damage, float penChance, int armorClassIndex)
        {
            ArmorClass armorClass = Conf.ArmorClasses[armorClassIndex];

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

        #region HelperFunctions
        private bool tryGetCurrentHit(UnturnedPlayer uPlayer, ELimb limb, out Vector3 localPoint)
        {
            localPoint = Vector3.zero;
            if (Conf.BetterArmor.BetterHitZones.Enabled)
            {
                foreach (PlayerHit hit in playerHits)
                {
                    if (hit.isCorrectHit(uPlayer.CSteamID, limb))
                    {
                        playerHits.Remove(hit);

                        Transform skeleton = hit.imputInfo.transform.GetChild(0).GetChild(0);

                        if (!getLocalPoint(skeleton, hit.imputInfo.point, hit.imputInfo.limb, out localPoint))
                        {
                            Logger.LogError("Error in BetterHitZones: No localPoint found for " + hit.imputInfo.limb + " of " + hit.imputInfo.transform.name);
                            return false;
                        }

                        return true;
                    }
                }
                Logger.LogError("BetterHitZones is enabled but no hit was found for Player: " + uPlayer.CharacterName + " at: " + limb);
            }
            return false;
        }
        public bool isFaceHit(ELimb limb, Vector3 localPoint)
        {
            if(limb == ELimb.SKULL)
            {
                bool faceHit = localPoint.z > 0.25;
                if (faceHit && Conf.Debug)
                {
                    Logger.Log("Raycast hit in the Face");
                }
                return faceHit;
            }
            return false;
        }
        public bool isStomachHit(ELimb limb, Vector3 localPoint)
        {
            if(limb == ELimb.SPINE)
            {
                bool stomachHit = localPoint.x > -0.23;
                if (stomachHit && Conf.Debug)
                {
                    Logger.Log("Raycast hit in the Stomach");
                }
                return stomachHit;
            }
            return false;
        }
        private bool getLocalPoint(Transform skeleton, Vector3 point, ELimb limb, out Vector3 localPoint)
        {
            Transform limbTransform = null;

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
        private float calcMean(float aMin, float aMax, float bMin, float bMax, float aActual)
        {
            float multi = 1 - (aActual - aMax) / (aMin - aMax);
            return bMin + multi * (bMax - bMin);
        }
        private Dictionary<ushort, T> createDictionaryFromItemExtensions<T>(List<T> itemExtensions) where T : ItemExtension
        {
            Dictionary<ushort, T> itemExtensionsDict = new Dictionary<ushort, T>();
            if(itemExtensions != null)
            {
                foreach (T itemExtension in itemExtensions)
                {
                    if (itemExtension.Id == 0)
                        continue;

                    if (itemExtensionsDict.ContainsKey(itemExtension.Id))
                    {
                        Logger.LogWarning("Item with Id:" + itemExtension.Id +" is a duplicate!");
                    }
                    else
                    {
                        itemExtensionsDict.Add(itemExtension.Id, itemExtension);
                    }
                    
                }
            }
            return itemExtensionsDict;
        }
        private void printPluginInfo()
        {

            Logger.Log("\nArmorPlus by SpeedMann Loaded, ");

            if (Conf.BetterArmor.Enabled)
            {
                BetterArmorConfig betterA = Conf.BetterArmor;
                Logger.Log("Enabled BetterArmor:\n"
                + (betterA.UseArmorClasses ? $" ArmorDamageMultiplierOnPen: {betterA.ArmorDamageMultiplierOnPen} PenDamgeDelta: {betterA.PenDamgeDelta}\n" : "")
                + $" GlassesEffectKey: {betterA.GlassesEffectKey} HatEffectKey: {betterA.HatEffectKey}\n"
                );
            }
            else
            {
                Logger.Log("Disabled BetterArmor:\n");
            }
            if (Conf.BreakLegs && !Conf.BoneBreakingChances.IsEmpty())
            {
                Logger.Log("Enabled BreakLegs:\n" + String.Join(
                    "\n", Conf.BoneBreakingChances.Select(
                        x => $" {x.Limb}: Min {x.BreakChanceMin}% Max {x.BreakChanceMax}% DamageMin {x.BreakChanceDamageMin} DamageMax {x.BreakChanceDamageMax}"
                    ).ToArray()
                ) + "\n");
            }
            else
            {
                Logger.Log("Disabled BreakLegs:\n");
            }
                
            if (Conf.BetterArmor.UseArmorClasses && !Conf.ArmorClasses.IsEmpty())
            {
                Logger.Log("Enabled ArmorClasses:\n" + String.Join(
                    "\n", Conf.ArmorClasses.Select(
                        x => $" Armor {x.Armor}: Tier {x.Tier}\n" +
                        $"  PercentForNormalDamage: {x.PercentForNormalDamage} PercentForMaxDamage: {x.PercentForMaxDamage}\n" +
                        $"  DamageMultiplierMin: {x.DamageMultiplierMin} DamageMultiplierNormal: {x.DamageMultiplierNormal}\n" +
                        $"  MinArmorDamage: {x.MinArmorDamage} MaxArmorDamage: {x.MaxArmorDamage}\n" +
                        $"  DamageToDamageArmorMin: {x.DamageToDamageArmorMin} DamageToDamageArmorMax: {x.DamageToDamageArmorMax}\n" +
                        $"  StopDamageMulti: {x.StopDamageMulti} PenLossMulti: {x.PenLossMulti}"
                    ).ToArray()
                ) + "\n");
            }
            else
            {
                Logger.Log("Disabled ArmorClasses:\n");
            }
               
            if (Conf.BetterArmor.BetterHitZones.Enabled)
            {
                BetterHitZonesConfig bHitZones = Conf.BetterArmor.BetterHitZones;
                Logger.Log("Enabled BetterHitZones:\n" 
                    + (bHitZones.HatsProtectFace ? " All Hats protect the face by default" : " Hats do not protect the face by default") + "\n"
                    + (bHitZones.VestsProtectStomach ? " All Vests protect the stomach by default" : " Vests do not protect the stomach by default") + "\n");
            }
            else
            {
                Logger.Log("Disabled BetterHitZones:\n");
            }

            if (gunExtensions != null && gunExtensions.Count() >= 0)
            {
                Logger.Log("GunExtensions:\n" + String.Join(
                    "\n", gunExtensions.Select(
                         x => $" {Assets.find(EAssetType.ITEM, x.Key)?.name ?? "> INVALID ID <"} [{x.Key}] Penetration: " + x.Value.Penetration
                    ).ToArray()
                ) + "\n");
            }

            if (hatExtensions != null && hatExtensions.Count() >= 0)
            {
                Logger.Log("HatExtensions:\n" + String.Join(
                    "\n", hatExtensions.Select(
                         x => $" {Assets.find(EAssetType.ITEM, x.Key)?.name ?? "> INVALID ID <"} [{x.Key}]\n" +
                         $"  ProtectFace: {x.Value.ProtectFace} FaceArmor: {x.Value.ArmorFace} \n"+
                         $"  EquipEffectId: {x.Value.EquipEffectId} UnequipEffectId: {x.Value.UnequipEffectId}"
                    ).ToArray()
                ) + "\n");
            }
            if (glassesExtensions != null && glassesExtensions.Count() >= 0)
            {
                Logger.Log("GlassesExtensions:\n" + String.Join(
                    "\n", hatExtensions.Select(
                         x => $" {Assets.find(EAssetType.ITEM, x.Key)?.name ?? "> INVALID ID <"} [{x.Key}] \n" +
                         $"  EquipEffectId: {x.Value.EquipEffectId} UnequipEffectId: {x.Value.UnequipEffectId}"
                    ).ToArray()
                ) + "\n");
            }
            if (vestExtensions != null && vestExtensions.Count() >= 0)
            {
                Logger.Log("VestsExtensions:\n" + String.Join(
                    "\n", vestExtensions.Select(
                         x => $" {Assets.find(EAssetType.ITEM, x.Key)?.name ?? "> INVALID ID <"} [{x.Key}]\n"
                         + $"  ProtectsStomach: {x.Value.ProtectStomach}"
                         + (x.Value.ShoulderPlateLength > 0 ? $"\n  ShoulderPlateLength: {x.Value.ShoulderPlateLength} Armor: {x.Value.ArmorShoulderPlate}" : "") 
                         + (x.Value.ThighPlateLength > 0 ? $"\n  ShoulderPlateLength: {x.Value.ThighPlateLength} Armor: {x.Value.ArmorThighPlate}" : "")
                    ).ToArray()
                ) + "\n");
            }

            ModsLoaded = true;
        }
        private IEnumerator waiter(UnturnedPlayer player)
        {
            yield return new WaitForSeconds(2);
            EffectController.checkClothingEffect(hatExtensions, player, player.Player.clothing.hat, true);
            // UI for nvg is automatically enabled
        }
        #endregion
    }
}
