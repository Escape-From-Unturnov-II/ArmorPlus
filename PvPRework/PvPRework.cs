using Rocket.API.Collections;
using Rocket.Core.Plugins;
using Rocket.Unturned;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Events;
using Rocket.Unturned.Player;
using SDG.Unturned;
using SpeedMann.PvPRework.Controllers;
using SpeedMann.PvPRework.Helper;
using SpeedMann.PvPRework.Models;
using SpeedMann.PvPRework.Models.Config;
using SpeedMann.PvPRework.UI;
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
        public static string PluginVersion = "1.0.0";
        public static PvPRework Inst;
        public static PVPReworkConfiguration Conf;
        internal static readonly System.Random rand = new System.Random();
        public static bool ModsLoaded = false;

        private static TimeSpan PlayerHitMaxAge = new TimeSpan(0,0,2);

        public override TranslationList DefaultTranslations =>
            new TranslationList
            {
                // Restriction        
                { "item_restricted_nvg", "You are not allowed to wear this helmet in combination with NVG!" },

                // Kill feed
                {"DEATH_BLEEDING", "{0} bled to death near {7}!"},
                {"DEATH_BONES", "{0} fell to their death near {7}!"},
                {"DEATH_FREEZING", "{0} froze to death near {7}!"},
                {"DEATH_BURNING", "{0} burned to death near {7}!"},
                {"DEATH_FOOD", "{0} starved to death near {7}!"},
                {"DEATH_BREATH", "{0} ran out of air near {7}."},
                {"DEATH_WATER", "{0} dehydrated to death near {7}!"},
                {"DEATH_INFECTION", "{0} died of infection near {7}!"},

                {"DEATH_GUN", "{1} [\u2719 {2}] shot {0} in the {3} using a {4}! [{6}m] near {7}"},
                {"DEATH_MELEE", "{1} [\u2719 {2}] meleed {0} in the {3} using a {4} near {7}!"},
                {"DEATH_PUNCH", "{1} [\u2719 {2}] punched {0} in the {3} near {7}!"},
                {"DEATH_ROADKILL", "{1} [\u2719 {2}] ran over {0} using a {5} near {7}!"},
                
                {"DEATH_VEHICLE", "{0} has died due to an explosion of a vehicle near {7}!"},
                {"DEATH_GRENADE", "{0} was obliterated by a grenade near {7}!"},
                {"DEATH_LANDMINE", "{0} was blown up by a landmine near {7}!"},
                {"DEATH_MISSILE", "{0} was annihilated by a missile near {7}!"},
                {"DEATH_CHARGE", "{0} was obliterated by a breaching charge near {7}!"},
                {"DEATH_SPLASH", "{0} was killed by a weak nearby explosion near {7}!"},
                {"DEATH_SHRED", "{0} was shredded to bits near {7}!"},
                {"DEATH_SENTRY", "{0} was shot by a turret near {7}!"},

                {"DEATH_ANIMAL", "{0} got killed by an animal near {7}."},
                {"DEATH_ZOMBIE", "{0} has been mauled by a zombie near {7}!"},
                {"DEATH_ACID", "{0} was melted alive near {7}!"},
                {"DEATH_BOULDER", "{0} was crushed by a boulder near {7}!"},
                {"DEATH_BURNING", "{0} gave a hug to a zombie in flames near {7}!"},
                {"DEATH_SPIT", "{0} died of shame from spits near {7}!"},
                {"DEATH_SPARK", "{0} was shocked to his death near {7}!"},
                {"DEATH_SUICIDE", "{0} killed themselves near {7}!"},
                {"DEATH_KILL", "{0} was killed by a higher force near {7}."},
                {"DEATH_ARENA", "{0} was killed by the arena near {7}."},
            };

        internal static bool HasDuribility;

        internal Dictionary<ushort, Caliber> bulletCalibers;
        internal Dictionary<ushort, GunExtension> gunExtensions;
        internal Dictionary<ushort, VestExtension> vestExtensions;
        internal Dictionary<ushort, HatExtension> hatExtensions;
        internal Dictionary<ushort, GlassesExtension> glassesExtensions;
        internal Dictionary<ushort, ushort> cyclableHelmets;
        internal Dictionary<ushort, ushort> cyclableSights;

        internal static List<PlayerHit> playerHits = new List<PlayerHit>();
        internal List<DamagePlayerParameters> playerPenetrations = new List<DamagePlayerParameters>();

        internal Dictionary<CSteamID, ExtendetHitLocation> lastHit;
        private Dictionary<CSteamID, ushort> hatSwaps;
        private Dictionary<CSteamID, StanceHandler> playerStances;
       
        private List<EquipItem> reequipItems;

        #region Load
        protected override void Load()
        {
            Inst = this;
            Conf = Configuration.Instance;
            PluginVersion = readFileVersion();

            playerHits = new List<PlayerHit>();
            hatSwaps = new Dictionary<CSteamID, ushort>();
            playerStances = new Dictionary<CSteamID, StanceHandler>();
            reequipItems = new List<EquipItem>();
            lastHit = new Dictionary<CSteamID, ExtendetHitLocation>();

            Level.onPreLevelLoaded += OnPreLevelLoaded;

            if (ModsLoaded)
            {
                Init();
            }
        }

        protected override void Unload()
        {
            Level.onPreLevelLoaded -= OnPreLevelLoaded;

            if (ModsLoaded)
            {
                StanceHandler.OnStanceChanged -= OnStanceChanged;

                DamageTool.damagePlayerRequested -= DamagePlayerRequested;
                UnturnedPatches.OnPreDisconnectSave -= OnPrePlayerDisconnect;
                U.Events.OnPlayerDisconnected -= OnPlayerDisconnected;

                // Plugin Keys
                PlayerInput.onPluginKeyTick -= InputHandler.OnPluginKeyDetected;
                InputHandler.OnPluginKeyPressed -= OnPluginKeyPressed;
                UnturnedPatches.OnPreAddItem -= OnAddItem;

                if (Conf.BetterArmor.BetterHitZones.Enabled)
                    UnturnedPatches.OnPostGetInput -= OnGetInput;

                // Cosmetics
                if (Conf.DisableCosmetics)
                {
                    UnturnedPatches.OnPostVisualToggle -= OnVisualToggle;
                }

                UseableConsumeable.onConsumePerformed -= OnConsumed;
                UseableConsumeable.onPerformingAid -= OnAid;

                // UI / preventNVG
                U.Events.OnPlayerConnected -= OnPlayerConnected;
                UnturnedPatches.OnPreChangeHat -= OnHatChanged;
                UnturnedPatches.OnPreChangeGlasses -= OnGlassesChanged;
                UnturnedPatches.OnPreVisionChanged -= OnVisionChanged;
                UnturnedPatches.OnPostPlayerRevive -= OnPlayerRevived;
                PlayerLife.onPlayerDied -= OnPlayerDeath;
                UnturnedPlayerEvents.OnPlayerDead -= OnPlayerDead;

                // health
                UnturnedPatches.OnPrePlayerDamaged -= OnPlayerDamaged;
                PlayerLife.OnTellBroken_Global -= OnBreakBones;
                PlayerLife.OnTellBleeding_Global -= OnStartBleeding;

                UnturnedPatches.Cleanup();
            }
        }
        private void OnPreLevelLoaded(int level)
        {
            Init();
            ModsLoaded = true;
        }
        private void Init()
        {
            UnturnedPrivateFields.Init();
            UnturnedPatches.Init();
            HealthManager.Init(Conf.HealthManager);
            

            Conf.addNames();
            Conf.updateConfig();
            createDictionaries();

            overrideArmorValues();

            linkEvents();
            printPluginInfo();
        }
        #endregion

        #region Events
        private void Update()
        {
            HealthManager.Update();

            while (reequipItems?.Count > 0)
            {
                UnturnedPlayer player = UnturnedPlayer.FromCSteamID(reequipItems[0].steamId);
                if(player != null)
                {
                    player.Player.equipment.tryEquip(reequipItems[0].page, reequipItems[0].x, reequipItems[0].y);
                    reequipItems.RemoveAt(0);
                }
            }
            while(playerPenetrations?.Count > 0)
            {
                DamageTool.damagePlayer(playerPenetrations[0], out EPlayerKill kill);
                playerPenetrations.RemoveAt(0);
            }
        }
        private void OnPlayerConnected(UnturnedPlayer player)
        {
            if (Conf.DisableCosmetics)
            {
                disableCosmethics(player.Player);
            }
            StartCoroutine(playerJoinWaiter(player));

            HealthManager.OnPlayerConnected(player);

            StanceHandler stanceHandler = new StanceHandler(player.Player.stance);
            player.Player.stance.onStanceUpdated += stanceHandler.StanceChangeInvoker;
            playerStances.Add(player.CSteamID, stanceHandler);
        }
        private void OnPrePlayerDisconnect(CSteamID steamID, ref bool shouldAllow)
        {
            HealthManager.OnPrePlayerDisconnected(steamID);
        }
        
        private void OnPlayerDisconnected(UnturnedPlayer player)
        {
            if(playerStances.TryGetValue(player.CSteamID, out StanceHandler handler)){
                player.Player.stance.onStanceUpdated -= handler.StanceChangeInvoker;
                playerStances.Remove(player.CSteamID);
            }
            HealthManager.OnPlayerDisconnected(player);
            InputHandler.removePlayerEntry(player.CSteamID);
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
                        if (sightId == nextSight)
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
        private void OnPlayerDeath(PlayerLife sender, EDeathCause cause, ELimb limb, CSteamID instigator)
        {
            UnturnedPlayer player = UnturnedPlayer.FromPlayer(sender.player);
            if (player == null) return;
            HealthManager.OnPlayerDeath(player);

            if (!Conf.KillFeed.Enabled)
                return;

            Player murderer = PlayerTool.getPlayer(instigator);

            string limbWord;

            if (lastHit.TryGetValue(sender.channel.owner.playerID.steamID, out ExtendetHitLocation hitLocation))
                limbWord = Enum.GetName(typeof(ExtendetHitLocation), hitLocation).Replace('_', ' ').ToLower();
            else
                limbWord = Enum.GetName(typeof(ELimb), limb).Replace('_', ' ').ToLower();

            string deathLocation = LevelNodes.nodes.OfType<LocationNode>()
                .OrderBy(k => Vector3.Distance(k.point, sender.player.transform.position)).FirstOrDefault()?.name ?? "Unknown";

            UnturnedChat.Say(
              Translate(
                $"DEATH_{cause}",
                sender?.channel?.owner?.playerID?.characterName ?? "Someone",
                murderer?.channel?.owner?.playerID?.characterName ?? "Anonymous",
                murderer?.life?.health ?? 0,
                limbWord,
                murderer?.equipment?.asset?.itemName ?? "N/A",
                murderer?.movement?.getVehicle()?.asset?.vehicleName ?? "N/A",
                Math.Round(Vector3.Distance(sender?.player?.transform?.position ?? Vector3.zero, murderer?.transform?.position ?? Vector3.zero)),
                deathLocation),
                UnturnedChat.GetColorFromName(Conf.KillFeed.MessageColor, Color.red)
            );
        }
        private void OnPlayerDead(UnturnedPlayer player, Vector3 position)
        {
            if(player.Player.clothing.hat == 0)
            {
                EffectControler.spawnUI(0, Conf.BetterArmor.HatEffectKey, player.CSteamID);
            }
            if(player.Player.clothing.glasses == 0)
            {
                EffectControler.spawnUI(0, Conf.BetterArmor.GlassesEffectKey, player.CSteamID);
            }
        }
        private void OnPlayerRevived(PlayerLife playerLife)
        {
            UnturnedPlayer player = UnturnedPlayer.FromPlayer(playerLife.player);
            if (player == null) return;
            HealthManager.OnPlayerRevived(player);
        }
        private void OnVisualToggle(PlayerClothing playerClothing, EVisualToggleType type, bool toggle)
        {
            if (toggle)
            {
                disableCosmethics(playerClothing.player);
            }
        }
        private void OnHatChanged(Player player, ushort newHatId, byte quality, byte[] state, ref bool shouldAllow)
        {
            //check preventNVGs
            if (hatExtensions.TryGetValue(newHatId, out HatExtension hatExtension) && hatExtension.PreventNVGs)
            {
                Asset asset = Assets.find(EAssetType.ITEM, player.clothing.glasses);
                if (asset != null && asset is ItemGlassesAsset)
                {
                    ItemGlassesAsset gAsset = (ItemGlassesAsset)asset;
                    if (gAsset.vision == ELightingVision.CIVILIAN || gAsset.vision == ELightingVision.MILITARY && hatExtension.WhitelistedNVGs.Find(x => x.Id == player.clothing.glasses) == null)
                    {
                        shouldAllow = false;
                        UnturnedPlayer uPlayer = UnturnedPlayer.FromPlayer(player);
                        Item item = new Item(newHatId, 1, quality, state);
                        if (!uPlayer.GiveItem(item))
                        {
                            uPlayer.Inventory.forceAddItem(item, false);
                        }
                        notifyIncompatibleHelmet(uPlayer);
                    }
                }
            }
            if(shouldAllow)
                ClothingEffectHandler.checkClothingEffect(hatExtensions, UnturnedPlayer.FromPlayer(player), newHatId);
        }
        private void OnGlassesChanged(Player player, ushort newGlassesId, byte quality, byte[] state, ref bool shouldAllow)
        {
            Asset asset = Assets.find(EAssetType.ITEM, newGlassesId);
            if (asset != null && asset is ItemGlassesAsset)
            {
                ItemGlassesAsset gAsset = (ItemGlassesAsset)asset;
                if (gAsset.vision == ELightingVision.CIVILIAN || gAsset.vision == ELightingVision.MILITARY)
                {
                    if(hatExtensions.TryGetValue(player.clothing.hat, out HatExtension hatExtension) && hatExtension.PreventNVGs && hatExtension.WhitelistedNVGs.Find(x => x.Id == player.clothing.glasses) == null)
                    {
                        shouldAllow = false;
                        UnturnedPlayer uPlayer = UnturnedPlayer.FromPlayer(player);
                        Item item = new Item(newGlassesId, 1, quality, state);
                        if (!uPlayer.GiveItem(item))
                        {
                            uPlayer.Inventory.forceAddItem(item, false);
                        }
                        
                        notifyIncompatibleHelmet(uPlayer);
                    }
                }
            }
        }
        private void OnVisionChanged(Player player, ushort glassesId, bool activate)
        {
            if (activate)
            {
                ClothingEffectHandler.checkClothingEffect(glassesExtensions, UnturnedPlayer.FromPlayer(player), glassesId);
            }
            else
            {
                ClothingEffectHandler.checkClothingEffect(glassesExtensions, UnturnedPlayer.FromPlayer(player), 0);
            }

        }
        private void OnBreakBones(PlayerLife playerLife)
        {
            
            HealthManager.fractureCheck(playerLife);
        }
        private void OnStartBleeding(PlayerLife playerLife)
        {
            HealthManager.bleedCheck(playerLife);
        }
        private void DamagePlayerRequested(ref DamagePlayerParameters parameters, ref bool shouldAllow)
        {
            if (Conf.Debug && !Conf.BetterArmor.Enabled)
                Logger.Log(parameters.player.name + " was damaged in the " + parameters.limb.ToString() + " Cause: " + parameters.cause + "!");

            UnturnedPlayer player = UnturnedPlayer.FromPlayer(parameters.player);
            ExtendetHitLocation hitLocation = ExtendedHitLocations.getExtendetHitlocation(parameters.limb);
            setLastHitLocation(player.CSteamID, hitLocation);


            switch (parameters.cause)
            {
                case EDeathCause.GUN:
                case EDeathCause.MELEE:
                case EDeathCause.PUNCH:
                    if (Conf.BetterArmor.Enabled)
                    {
                        ArmorLogic.ArmorPenCheck(parameters.player, parameters.limb, parameters.cause, parameters.direction, parameters.killer, ref parameters.damage, ref parameters.respectArmor, parameters.applyGlobalArmorMultiplier, out hitLocation);
                    }
                        
                    if (Conf.BreakLegs)
                    {
                        BreakBoneCheck(parameters.player, parameters.limb, parameters.damage);
                    }
                    HealthManager.damageBodyPart(player, hitLocation, (int)Math.Round(parameters.damage), out bool dead);
                    parameters.damage = 1;
                    if (dead)
                    {
                        parameters.damage = 101;
                    }
                    break;
            }
            
        }
        private void OnPlayerDamaged(PlayerLife playerLife, ref byte amount, EDeathCause cause, ref ELimb limb, CSteamID killer, ref bool canCauseBleeding, ref bool shouldAllow)
        {
            UnturnedPlayer player = UnturnedPlayer.FromPlayer(playerLife.player);
            HealthManager.damageCheck(player, ref amount, cause, ref limb, killer, ref canCauseBleeding, ref shouldAllow);
        }
        private void OnGetInput(ref InputInfo inputInfo)
        {
            if (inputInfo != null && inputInfo.type == ERaycastInfoType.PLAYER && inputInfo.player != null && inputInfo.transform != null)
            {
                for(int i=0; i < playerHits.Count; i++)
                {
                    if (playerHits[i].isOlderThan(PlayerHitMaxAge))
                    {
                        InputInfo removedHit = playerHits[i].imputInfo;
                        playerHits.RemoveAt(i--);
                        if (Conf.Debug)
                        {
                            Logger.Log("PlayerHit timedout: " + removedHit.player.name + " in the " + removedHit.limb);
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
        private void OnAid(Player instigator, Player target, ItemConsumeableAsset asset, ref bool shouldAllow)
        {
            HealthManager.OnConsumed(target, instigator, asset);
        }
        private void OnConsumed(Player instigatingPlayer, ItemConsumeableAsset asset)
        {
            HealthManager.OnConsumed(instigatingPlayer, instigatingPlayer, asset);
        }
        private void OnStanceChanged(EPlayerStance oldStance, PlayerStance stance, out EPlayerStance newStance)
        {
            newStance = oldStance;
            if (stance?.player?.life == null) return;
            newStance = stance.stance;

            switch (oldStance)
            {
                case EPlayerStance.PRONE:
                    if(Conf.MovementExtension.PushupStaminaDrain > 0)
                    {
                        if (stance.player.life.stamina < Conf.MovementExtension.PushupStaminaDrain)
                        {
                            stance.stance = EPlayerStance.PRONE;
                            newStance = EPlayerStance.PRONE;
                            //stance.checkStance(newStance, true);
                            //UnturnedPrivateFields.setPalyerStance(stance);
                        }
                        else
                        {
                            stance.player.life.serverModifyStamina(-Conf.MovementExtension.PushupStaminaDrain);
                        }
                    }
                    break;
            }

            switch (stance.stance)
            {
                case EPlayerStance.PRONE:
                    PlayerEquipment equipment = stance.player.equipment;
                    if (equipment?.useable != null && equipment.useable is UseableGun && !equipment.isBusy && oldStance != EPlayerStance.PRONE && oldStance != EPlayerStance.CROUCH && Conf.MovementExtension.ReequipGunsOnProne)
                    {
                        reequipItems.Add(new EquipItem
                        {
                            steamId = UnturnedPlayer.FromPlayer(equipment.player).CSteamID,
                            page = equipment.equippedPage,
                            x = equipment.equipped_x,
                            y = equipment.equipped_y,
                        });
                        equipment.dequip();
                    }
                    break;
            }
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

                    Logger.Log("breakChance: " + breakChance + " Damage: " + damage + "!");
                }
            }
        }
        #endregion


        #region HelperFunctions
        internal void setLastHitLocation(CSteamID steamID, ExtendetHitLocation hitLocation)
        {
            if (!Conf.BetterArmor.BetterHitZones.Enabled)
            {
                return;
            }
            if (!lastHit.ContainsKey(steamID))
                lastHit.Add(steamID, hitLocation);
            else
                lastHit[steamID] = hitLocation;
        }
        private void notifyIncompatibleHelmet(UnturnedPlayer player)
        {
            if (Conf.UseNotificationUI)
            {
                EffectControler.spawnUI(Conf.BetterArmor.NotificationIncompatibleId, Conf.NotificationEffectKey, player.CSteamID);
            }
            else
            {
                UnturnedChat.Say(player, Util.Translate("item_restricted_nvg"), Color.red);
            }
            
        }
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
        internal void getGunStats(Player player, out ItemWeaponAsset weapon, out float penetration, out float fleshDamage, out float armorDamage, out Caliber caliber)
        {
            caliber = null;
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
            fleshDamage = 10;
            armorDamage = 0;
            if (weapon == null) return; // no weapon

            // set asset values
            fleshDamage = weapon.playerDamageMultiplier.damage;
            armorDamage = weapon.barricadeDamage;

            // check Calibers
            if (gunAttachments?.magazineAsset?.calibers != null)
            {
                foreach (ushort magCaliberId in gunAttachments.magazineAsset.calibers)
                {
                    if (bulletCalibers.TryGetValue(magCaliberId, out caliber))
                    {
                        penetration = caliber.Penetration;
                        fleshDamage = caliber.FleshDamage;
                        armorDamage = caliber.ArmorDamage;
                        break;
                    }
                }
            }

            // check gun extensions
            if (gunExtension != null)
            {

                penetration = gunExtension.Penetration >= 0 ? gunExtension.Penetration : penetration * gunExtension.PenetrationMultiplier;
                fleshDamage = gunExtension.FleshDamage >= 0 ? gunExtension.FleshDamage : fleshDamage * gunExtension.FleshDamageMultiplier;
                armorDamage = gunExtension.ArmorDamage >= 0 ? gunExtension.ArmorDamage : armorDamage * gunExtension.ArmorDamageMultiplier;

                // check mag override
                if (gunAttachments != null)
                {                  
                    MagazineExtension magOver = gunExtension.MagazineOverrides.Find(x => x.Id == gunAttachments.magazineID);
                    if (magOver != null)
                    {
                        penetration = magOver.Penetration >= 0 ? magOver.Penetration : penetration;
                        fleshDamage = magOver.FleshDamage >= 0 ? magOver.FleshDamage : fleshDamage;
                        armorDamage = magOver.ArmorDamage >= 0 ? magOver.ArmorDamage : armorDamage;
                    }
                }
            }

            //get barrel damage multie
            if (gunAttachments?.barrelAsset != null)
            {
                penetration *= gunAttachments.barrelAsset.ballisticDamageMultiplier;
                fleshDamage *= gunAttachments.barrelAsset.ballisticDamageMultiplier;
                armorDamage *= gunAttachments.barrelAsset.ballisticDamageMultiplier;
            }
        }     
        internal static float calcMean(float aMin, float aMax, float bMin, float bMax, float aActual)
        {
            float innerMulti = 1 - (aActual - aMax) / (aMin - aMax);
            return bMin + innerMulti * (bMax - bMin);
        }
        internal static Dictionary<ushort, Caliber> createCaliberDictionary(List<Caliber> calibers)
        {
            Dictionary<ushort, Caliber> dict = new Dictionary<ushort, Caliber>();
            foreach(Caliber cal in calibers)
            {
                foreach(ushort magCalId in cal.MagazineCalibers)
                {
                    if (dict.ContainsKey(magCalId))
                    {
                        Logger.LogWarning("MagazineCalliber with Id:" + magCalId + " is used in multiple Calibers!");
                    }
                    else
                    {
                        dict.Add(magCalId, cal);
                    }
                }
            }

            return dict;
        }
        internal static Dictionary<ushort, ushort> createCycleDictionary(List<List<ItemExtension>> cycles)
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
                            if (dict.ContainsKey(cycle[i].Id))
                            {
                                Logger.LogWarning("Cyclic Item with Id:" + cycle[i].Id + " is already used in other cycle!");
                            }
                            else
                            {
                                dict.Add(cycle[i].Id, cycle[i + 1].Id);
                            } 
                        }
                        else
                        {
                            if (dict.ContainsKey(cycle[i].Id))
                            {
                                Logger.LogWarning("Cyclic Item with Id:" + cycle[i].Id + " is already used in other cycle!");
                            }
                            else
                            {
                                dict.Add(cycle[i].Id, cycle[0].Id);
                            }
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
        internal static Dictionary<ushort, T> createDictionaryFromItemExtensions<T>(List<T> itemExtensions) where T : ItemExtension
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
            StanceHandler.OnStanceChanged += OnStanceChanged;

            DamageTool.damagePlayerRequested += DamagePlayerRequested;
            UnturnedPatches.OnPreDisconnectSave += OnPrePlayerDisconnect;
            U.Events.OnPlayerDisconnected += OnPlayerDisconnected;

            // Plugin Keys
            PlayerInput.onPluginKeyTick += InputHandler.OnPluginKeyDetected;
            InputHandler.OnPluginKeyPressed += OnPluginKeyPressed;
            UnturnedPatches.OnPreAddItem += OnAddItem;

            if (Conf.BetterArmor.BetterHitZones.Enabled)
                UnturnedPatches.OnPostGetInput += OnGetInput;

            // Cosmetics
            if (Conf.DisableCosmetics)
            {
                List<SteamPlayer> players = Provider.clients;
                foreach (SteamPlayer player in players)
                {
                    disableCosmethics(player.player);
                }
                UnturnedPatches.OnPostVisualToggle += OnVisualToggle;
            }

            UseableConsumeable.onConsumePerformed += OnConsumed;
            UseableConsumeable.onPerformingAid += OnAid;

            // UI / preventNVG / killfeed
            U.Events.OnPlayerConnected += OnPlayerConnected;
            UnturnedPatches.OnPreChangeHat += OnHatChanged;
            UnturnedPatches.OnPreChangeGlasses += OnGlassesChanged;
            UnturnedPatches.OnPreVisionChanged += OnVisionChanged;
            UnturnedPatches.OnPostPlayerRevive += OnPlayerRevived;
            PlayerLife.onPlayerDied += OnPlayerDeath;
            UnturnedPlayerEvents.OnPlayerDead += OnPlayerDead;

            // health
            UnturnedPatches.OnPrePlayerDamaged += OnPlayerDamaged;
            PlayerLife.OnTellBroken_Global += OnBreakBones;
            PlayerLife.OnTellBleeding_Global += OnStartBleeding;

            if (Conf.ArmorClasses == null || Conf.ArmorClasses.IsEmpty())
            {
                Conf.BetterArmor.UseArmorClasses = false;
            }
            HasDuribility = Provider.modeConfigData.Items.Has_Durability;
        }
        private void createDictionaries()
        {
            //converts lists to dictionarys to increase performance
            bulletCalibers = createCaliberDictionary(Conf.BulletCalibers);
            gunExtensions = createDictionaryFromItemExtensions(Conf.GunExtensions);
            vestExtensions = createDictionaryFromItemExtensions(Conf.VestExtensions);
            hatExtensions = createDictionaryFromItemExtensions(Conf.HatExtensions);
            glassesExtensions = createDictionaryFromItemExtensions(Conf.GlassesExtensions);
            cyclableHelmets = createCycleDictionary(Conf.CyclableHelmets);
            cyclableSights = createCycleDictionary(Conf.CyclableSights);
            
        }
        private void overrideArmorValues()
        {
            overrideArmorValues(Conf.VestExtensions);
            overrideArmorValues(Conf.HatExtensions);
            overrideArmorValues(Conf.MaskExtensions);
        }
        private void overrideArmorValues<T>(List<T> clothingExtensions) where T : ItemClothingExtension
        {
            foreach (ItemClothingExtension clothing in clothingExtensions)
            {
                ItemClothingAsset asset = (ItemClothingAsset)Assets.find(EAssetType.ITEM, clothing.Id);
                //TODO: store original value to restore later
                if(!UnturnedPrivateFields.setClothingArmor(asset, clothing.Armor))
                {
                    Logger.LogError($"Could not modify armor for: {clothing.Id}");
                }
            }
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
                         $"  ArmorDamage: {x.Value.ArmorDamage}\n" +
                         $"  PenetrationMultiplier: {x.Value.PenetrationMultiplier}\n" +
                         $"  FleshDamageMultiplier: {x.Value.FleshDamageMultiplier}\n" +
                         $"  ArmorDamageMultiplier: {x.Value.ArmorDamageMultiplier}" +
                         (x.Value.MagazineOverrides != null && x.Value.MagazineOverrides.Count() > 0 ? String.Join(
                             "", x.Value.MagazineOverrides.Select(
                                 y => $"\n   {Assets.find(EAssetType.ITEM, y.Id)?.name ?? "> INVALID ID <"} [{y.Id}]\n" +
                                 $"   Penetration: {x.Value.Penetration}\n" +
                                 $"   FleshDamage: {x.Value.FleshDamage}\n" +
                                 $"   ArmorDamage: {x.Value.ArmorDamage}"
                             ).ToArray()
                         )+"\n" : "\n")
                    ).ToArray()
                ) + "\n");
            }
            if(Conf.BulletCalibers != null && Conf.BulletCalibers.Count() >= 0)
            {
                Logger.Log("BulletCallibers:\n" + String.Join(
                  "\n", Conf.BulletCalibers.Select(
                       x => $" {x.Name} \n" +
                       $"  Penetration: {x.Penetration}\n" +
                       $"  FleshDamage: {x.FleshDamage}\n" +
                       $"  ArmorDamage: {x.ArmorDamage}" +
                       (x.MagazineCalibers != null && x.MagazineCalibers.Count() > 0 ? "\n  MagazineCalibers:" + String.Join(
                             "", x.MagazineCalibers.Select(
                                    y => $"\n   {y}"
                             ).ToArray()
                         ) + "\n" : "\n")
                  ).ToArray()
              ) + "\n");
            }
            if (hatExtensions != null && hatExtensions.Count() >= 0)
            {
                Logger.Log("HatExtensions:\n" + String.Join(
                    "\n", hatExtensions.Select(
                         x => $" {Assets.find(EAssetType.ITEM, x.Key)?.name ?? "> INVALID ID <"} [{x.Key}]\n" +
                         $"  ProtectFace: {x.Value.ProtectFace} FaceArmor: {x.Value.ArmorFace} \n"+
                         $"  ProtectEars: {x.Value.ProtectEars} EarArmor: {x.Value.ArmorEars} \n" +
                         $"  EquipEffectId: {x.Value.EquipEffectId} UnequipEffectId: {x.Value.UnequipEffectId}\n"
                    ).ToArray()
                ) + "\n");
            }
            if (glassesExtensions != null && glassesExtensions.Count() >= 0)
            {
                Logger.Log("GlassesExtensions:\n" + String.Join(
                    "\n", glassesExtensions.Select(
                         x => $" {Assets.find(EAssetType.ITEM, x.Key)?.name ?? "> INVALID ID <"} [{x.Key}] \n" +
                         $"  EquipEffectId: {x.Value.EquipEffectId} UnequipEffectId: {x.Value.UnequipEffectId}\n"
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
                         + "\n"
                    ).ToArray()
                ) + "\n");
            }
        }
        private IEnumerator playerJoinWaiter(UnturnedPlayer player)
        {
            yield return new WaitForSeconds(2);
            ClothingEffectHandler.checkClothingEffect(hatExtensions, player, player.Player.clothing.hat, true);
            // UI for nvg is automatically enabled
        }
        private static string readFileVersion()
        {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            System.Diagnostics.FileVersionInfo fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location);
            return fvi.FileVersion;
        }
        #endregion
    }
}
