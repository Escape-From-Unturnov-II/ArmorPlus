using Rocket.Core.Plugins;
using Rocket.Unturned;
using Rocket.Unturned.Events;
using Rocket.Unturned.Player;
using SDG.Unturned;
using SpeedMann.PvPRework.Helper;
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
        public static string PluginVersion = "1.4.0";
        public static PvPRework Inst;
        public static PVPReworkConfiguration Conf;
        private static readonly System.Random rand = new System.Random();
        public static bool ModsLoaded = false;

        private static TimeSpan PlayerHitMaxAge = new TimeSpan(0,0,2);

        private static bool HasDuribility;

        private Dictionary<ushort, GunExtension> gunExtensions;
        private Dictionary<ushort, VestExtension> vestExtensions;
        private Dictionary<ushort, HatExtension> hatExtensions;
        private Dictionary<ushort, GlassesExtension> glassesExtensions;
        private Dictionary<ushort, ushort> cyclableHelmets;
        private Dictionary<ushort, ushort> cyclableSights;

        private List<PlayerHit> playerHits;
        private Dictionary<CSteamID, ushort> hatSwaps;

        #region Load
        protected override void Load()
        {
            Inst = this;
            Conf = Configuration.Instance;

            playerHits = new List<PlayerHit>();
            hatSwaps = new Dictionary<CSteamID, ushort>();

            Level.onPreLevelLoaded += OnPreLevelLoaded;

            if (ModsLoaded)
            {
                Conf.updateConfig();
                createDictionaries();

                linkEvents();

                printPluginInfo();
            }
        }
        protected override void Unload()
        {
            Level.onPreLevelLoaded -= OnPreLevelLoaded;

            if (ModsLoaded)
            {
                UnturnedPlayerEvents.OnPlayerUpdateStance -= OnStanceChanged;
                DamageTool.damagePlayerRequested -= DamagePlayerRequested;
                U.Events.OnPlayerDisconnected -= OnPlayerDisconnected;

                // Plugin Keys
                PlayerInput.onPluginKeyTick -= InpuHandler.OnPluginKeyDetected;
                InpuHandler.OnPluginKeyPressed -= OnPluginKeyPressed;
                UnturnedPatches.OnPreAddItem -= OnAddItem;

                UnturnedPatches.Cleanup();

                if (Conf.BetterArmor.BetterHitZones.Enabled)
                    UnturnedPatches.OnPostGetInput -= OnGetInput;

                // Cosmetics
                if (Conf.DisableCosmetics)
                {
                    UnturnedPatches.OnPostVisualToggle -= OnVisualToggle;
                }

                //UI
                U.Events.OnPlayerConnected -= OnPlayerConnected;
                UnturnedPatches.OnPreChangeHat -= OnPreHatChanged;
                UnturnedPlayerEvents.OnPlayerDead -= OnPlayerDead;
            }
        }
        private void OnPreLevelLoaded(int level)
        {
            Conf.addNames();
            Conf.updateConfig();
            createDictionaries();
            linkEvents();
            printPluginInfo();
            ModsLoaded = true;
        }
        #endregion

        private void OnPlayerConnected(UnturnedPlayer player)
        {
            if (Conf.DisableCosmetics)
            {
                disableCosmethics(player.Player);
            }
            
            StartCoroutine(waiter(player));
        }

        private void OnPlayerDisconnected(UnturnedPlayer player)
        {
            InpuHandler.removePlayerEntry(player.CSteamID);
        }

        private void OnPluginKeyPressed(UnturnedPlayer player, byte key)
        {
            switch (key)
            {
                case 2:
                    PlayerEquipment equipment = player.Player.equipment;
                    if(equipment != null && equipment.asset != null && equipment.asset is ItemGunAsset)
                    {
                        
                        byte[] array = new byte[] { equipment.state[0], equipment.state[1] };
                        ushort sightId = BitConverter.ToUInt16(array, 0);
                        if (cyclableSights.TryGetValue(sightId, out ushort nextSight))
                        {
                            changeSight(equipment, nextSight);
                        }
                        else if(Conf.Debug)
                            Logger.Log($"Sight key pressed but sight {sightId} can't be cycled");
                        if (Conf.Debug)
                            Logger.Log($"Sight changed to: {sightId}");
                    }
                    else if(Conf.Debug)
                        Logger.Log($"Sight key pressed but no gun equiped");

                    break;
                case 3:
                    PlayerClothing clothing = player.Player.clothing;
                    
                    if (cyclableHelmets.TryGetValue(clothing.hat, out ushort nextHelmet))
                    {
                        changeHat(clothing, nextHelmet);
                    }
                    else if (Conf.Debug)
                        Logger.Log($"Helmet key pressed but helmet {clothing.hat} can't be cycled");

                    break;
            }
        }

        private void OnPlayerDead(UnturnedPlayer player, Vector3 position)
        {
            if(player.Player.clothing.hat == 0)
            {
                EffectController.spawnUI(0, Conf.BetterArmor.HatEffectKey, player.CSteamID);
            }
            if(player.Player.clothing.glasses == 0)
            {
                EffectController.spawnUI(0, Conf.BetterArmor.GlassesEffectKey, player.CSteamID);
            }
        }
        
        private void OnVisualToggle(PlayerClothing playerClothing, EVisualToggleType type, bool toggle)
        {
            if (toggle)
            {
                disableCosmethics(playerClothing.player);
            }
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
        private void OnAddItem(UnturnedPlayer player, Items page, Item item, ref bool shouldAllow)
        {
            if (hatSwaps.TryGetValue(player.CSteamID, out ushort oldHelmetId) && oldHelmetId == item.id)
            {
                hatSwaps.Remove(player.CSteamID);
                shouldAllow = false;
            }
        }
        private void OnStanceChanged(UnturnedPlayer player, byte stance)
        {
            Logger.Log($"Changed Stance: {stance}");
        }
       

        #region ArmorCheck
        private void ArmorPenCheck(Player player, ELimb limb, CSteamID oponentId, ref float damage, ref bool respectArmor, bool applyGlobalArmorMultiplier)
        {
            respectArmor = false;
            bool didPenetrate = true; // set penetrate to true to avoid cancle on no vest or no helmet

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

            ItemWeaponAsset oponentWeapon;

            getGunStats(oponent.Player, out oponentWeapon, out float penetration, out float fleshDamage, out float armorDamage);

            Vector3 currentlocalHit;
            float armorOverride = -1;
            float damageMulti = 1;
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
                    fleshDamage = oponentWeapon != null ? fleshDamage * oponentWeapon.playerDamageMultiplier.arm : fleshDamage * 0.6f;

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
                        if (useOuterArmor)
                        {
                            didPenetrate = penArmor(player, vest, ref armorDamage, ref penetration, ref fleshDamage, armorOverride);
                        }
                        

                        if (didPenetrate && shirt != null)
                        {
                            didPenetrate = penArmor(player, shirt, ref armorDamage, ref penetration, ref fleshDamage, damageMulti);
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
                        if (useOuterArmor)
                        {
                            didPenetrate = penArmor(player, vest, ref armorDamage, ref penetration, ref fleshDamage, armorOverride);
                        }

                        if (didPenetrate && pants != null)
                        {
                            didPenetrate = penArmor(player, pants, ref armorDamage, ref penetration, ref fleshDamage);
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

                    if(hat != null)
                    {
                        if (faceHit)
                        {
                            if (hatExtensions.TryGetValue(hat.id, out hatExtension))
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
                                useOuterArmor = Conf.BetterArmor.BetterHitZones.HatsProtectFace;
                            }
                        }
                        if (earHit)
                        {
                            if (hatExtensions.TryGetValue(hat.id, out hatExtension))
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
                                useOuterArmor = Conf.BetterArmor.BetterHitZones.HatsProtectEars;
                            }
                        }
                    }


                    if (Conf.BetterArmor.UseArmorClasses)
                    {
                        if (hat != null && useOuterArmor)
                        {
                            didPenetrate = penArmor(player, hat, ref armorDamage, ref penetration, ref fleshDamage, armorOverride);
                        }

                        if (didPenetrate && mask != null)
                        {
                            didPenetrate = penArmor(player, mask, ref armorDamage, ref penetration, ref fleshDamage);
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
                        if (vest != null && !stomacheHit || useOuterArmor)
                        {
                            didPenetrate = penArmor(player, vest, ref armorDamage, ref penetration, ref fleshDamage);
                        }

                        if (didPenetrate && shirt != null)
                        {
                            didPenetrate = penArmor(player, shirt, ref armorDamage, ref penetration, ref fleshDamage);
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
                damage = fleshDamage * armor;
            }
            damage = (float)Math.Round(fleshDamage);
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
        private bool penArmor(Player player, ItemClothingAsset clothingPart, ref float armorDamage, ref float penPower, ref float fleshDamage, float armorOverride = -1)
        {
            float penChance = 1;
            bool didPenetrate = true;
            int armorClassIndex;
            float armorTier;
            float oldPenDamage = penPower;
            float oldFleshDamage = fleshDamage;

            float armor = calcItemArmor(player, clothingPart, out armorClassIndex, out armorTier, false, armorOverride);
            

            if (armor > 0)
            {
                penChance = calcPenChance(armor, penPower);

                if (penChance > rand.NextDouble())
                {
                    penPower = calcPenDamage(penPower, penChance, armorClassIndex);
                    fleshDamage = calcDamage(fleshDamage, penChance, armorClassIndex);
                }
                else
                {
                    didPenetrate = false;
                    fleshDamage *= Conf.ArmorClasses[armorClassIndex].StopDamageMulti;
                }
            }

            damageArmor(player, clothingPart, armorClassIndex, armorDamage, didPenetrate);

            if (Conf.Debug)
            {
                Logger.Log("penChance: " + penChance + " GunPenetration: " + oldPenDamage + " gunDamage: " + oldFleshDamage + " actualDamage: " + fleshDamage + " Armor: " + clothingPart.name + " [T:" + armorTier + " A:" + armor + "]!");

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
        internal void disableCosmethics(Player player)
        {
            player.clothing.ServerSetVisualToggleState(EVisualToggleType.COSMETIC, false);
            player.clothing.ServerSetVisualToggleState(EVisualToggleType.MYTHIC, false);
        }
        private void changeSight(PlayerEquipment equipment, ushort newSightId)
        {
            byte[] array = BitConverter.GetBytes(newSightId);
            equipment.state[0] = array[0];
            equipment.state[1] = array[1];

            equipment.sendUpdateState();
        }
        private void changeHat(PlayerClothing clothing, ushort newHelmetId)
        {
            CSteamID steamId = UnturnedPlayer.FromPlayer(clothing.player).CSteamID;
            if(!hatSwaps.ContainsKey(steamId))
                hatSwaps.Add(steamId, clothing.hat);
            clothing.askWearHat(newHelmetId, clothing.hatQuality, clothing.hatState, true);
        }
        internal void getGunStats(Player player, out ItemWeaponAsset weapon, out float penetration, out float fleshDamage, out float armorDamage)
        {
            weapon = null;
            Attachments gunAttachments = null;
            GunExtension gunExtension = null;
            
            if (player.equipment?.asset is ItemWeaponAsset)
            {
                weapon = (ItemWeaponAsset)player.equipment.asset;
                if (player.equipment.useable is UseableGun)
                {
                    UseableGun oponentGun = (UseableGun)player.equipment.useable;
                    UnturnedPrivateFields.getGunAttachments(oponentGun, out gunAttachments);
                }
                gunExtensions.TryGetValue(player.equipment.asset.id, out gunExtension);
            }
            
            penetration = 0;
            fleshDamage = weapon.playerDamageMultiplier.damage;
            armorDamage = weapon.barricadeDamage;
            
            if (gunExtension != null)
            {
                penetration = gunExtension.Penetration >= 0 ? gunExtension.Penetration : penetration;
                fleshDamage = gunExtension.FleshDamage >= 0 ? gunExtension.FleshDamage : fleshDamage;
                armorDamage = gunExtension.ArmorDamage >= 0 ? gunExtension.ArmorDamage : armorDamage;

                if (gunAttachments != null && gunExtension.MagazineOverrides?.Count > 0)
                {
                    MagazineOverride magOver = gunExtension.MagazineOverrides.Find(x => x.Id == gunAttachments.magazineID);
                    if (magOver != null)
                    {
                        penetration = magOver.Penetration >= 0 ? magOver.Penetration : penetration;
                        fleshDamage = magOver.FleshDamage >= 0 ? magOver.FleshDamage : fleshDamage;
                        armorDamage = magOver.ArmorDamage >= 0 ? magOver.ArmorDamage : armorDamage;
                    }
                }
            }
        }
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

        public static bool isEarHit(ELimb limb, Vector3 localPoint)
        {
            if (limb == ELimb.SKULL)
            {
                bool earsHit = localPoint.x > -0.5 && (localPoint.y >= 0.2 || localPoint.y <= -0.2) && localPoint.z > -0.05;
                if (earsHit && Conf.Debug)
                {
                    Logger.Log("Raycast hit in the Ears");
                }
                return earsHit;
            }
            return false;
        }
        public static bool isFaceHit(ELimb limb, Vector3 localPoint)
        {
            if(limb == ELimb.SKULL)
            {
                bool faceHit = localPoint.x > -0.55 && localPoint.z >= 0.2;
                if (faceHit && Conf.Debug)
                {
                    Logger.Log("Raycast hit in the Face");
                }
                return faceHit;
            }
            return false;
        }
        public static bool isStomachHit(ELimb limb, Vector3 localPoint)
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
        private static bool getLocalPoint(Transform skeleton, Vector3 point, ELimb limb, out Vector3 localPoint)
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

        private Dictionary<ushort, ushort> createCycleDictionary(List<List<ItemExtension>> cycles)
        {
            Dictionary<ushort, ushort> dict = new Dictionary<ushort, ushort>();
            foreach(List<ItemExtension> cycle in cycles)
            {
                if(cycle != null && cycle.Count > 1)
                {
                    for(int i = 0; i < cycle.Count; i++)
                    {
                        if(i+1 < cycle.Count)
                        {
                            dict.Add(cycle[i].Id, cycle[i+1].Id);
                        }
                        else
                        {
                            dict.Add(cycle[i].Id, cycle[0].Id);
                        }
                    }
                }
                else
                {
                    Logger.LogWarning("Error in cycleable items, empty or only 1 item defined!");
                }
            }
            return dict;
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
        private void linkEvents()
        {
            UnturnedPrivateFields.Init();
            UnturnedPatches.Init();

            UnturnedPlayerEvents.OnPlayerUpdateStance += OnStanceChanged;
            DamageTool.damagePlayerRequested += DamagePlayerRequested;
            U.Events.OnPlayerDisconnected += OnPlayerDisconnected;

            // Plugin Keys
            PlayerInput.onPluginKeyTick += InpuHandler.OnPluginKeyDetected;
            InpuHandler.OnPluginKeyPressed += OnPluginKeyPressed;
            UnturnedPatches.OnPreAddItem += OnAddItem;


            if (Conf.BetterArmor.BetterHitZones.Enabled)
                UnturnedPatches.OnPostGetInput += OnGetInput;

            // Cosmetics
            if (Conf.DisableCosmetics)
            {
                List<object> players = Provider.clients.Cast<object>().ToList();
                foreach (Player player in players)
                {
                    disableCosmethics(player);
                }
                UnturnedPatches.OnPostVisualToggle += OnVisualToggle;

            }

            // UI
            U.Events.OnPlayerConnected += OnPlayerConnected;
            UnturnedPatches.OnPreChangeHat += OnPreHatChanged;
            UnturnedPatches.OnPreVisionChanged += OnVisionChanged;
            UnturnedPlayerEvents.OnPlayerDead += OnPlayerDead;


            if (Conf.ArmorClasses == null || Conf.ArmorClasses.IsEmpty())
            {
                Conf.BetterArmor.UseArmorClasses = false;
            }
            HasDuribility = Provider.modeConfigData.Items.Has_Durability;
        }
        private void createDictionaries()
        {
            //converts lists to dictionarys to increase performance
            gunExtensions = createDictionaryFromItemExtensions(Conf.GunExtensions);
            vestExtensions = createDictionaryFromItemExtensions(Conf.VestExtensions);
            hatExtensions = createDictionaryFromItemExtensions(Conf.HatExtensions);
            glassesExtensions = createDictionaryFromItemExtensions(Conf.GlassesExtensions);
            cyclableHelmets = createCycleDictionary(Conf.CyclableHelmets);
            cyclableSights = createCycleDictionary(Conf.CyclableSights);
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

            Logger.Log($"{(Conf.DisableCosmetics ? "Disabled" : "Allow")} Cosmetics");

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
                         x => $" {Assets.find(EAssetType.ITEM, x.Key)?.name ?? "> INVALID ID <"} [{x.Key}] \n" +
                         $"  Penetration: {x.Value.Penetration}\n" +
                         $"  FleshDamage: {x.Value.FleshDamage}\n" +
                         $"  ArmorDamage: {x.Value.ArmorDamage}" +
                         (x.Value.MagazineOverrides != null && x.Value.MagazineOverrides.Count() > 0 ? "\n"+String.Join(
                             "", x.Value.MagazineOverrides.Select(
                                 y => $"\n   {Assets.find(EAssetType.ITEM, y.Id)?.name ?? "> INVALID ID <"} [{y.Id}] " +
                                 $"   Penetration: {x.Value.Penetration}\n" +
                                 $"   FleshDamage: {x.Value.FleshDamage}\n" +
                                 $"   ArmorDamage: {x.Value.ArmorDamage}\n"
                             ).ToArray()
                         ) : "\n")
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
                    "\n", glassesExtensions.Select(
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
