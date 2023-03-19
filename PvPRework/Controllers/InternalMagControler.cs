using Rocket.Core.Assets;
using Rocket.Unturned.Player;
using SDG.Unturned;
using SpeedMann.PvPRework.Helper;
using SpeedMann.PvPRework.Models;
using SpeedMann.PvPRework.Models.Config;
using SpeedMann.PvPRework.Models.Config.ItemExtensions;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;
using Logger = Rocket.Core.Logging.Logger;

namespace SpeedMann.PvPRework.Controllers
{
    internal class InternalMagControler
    {
        private static bool Debug = true;
        private static Dictionary<ushort, Dictionary<ushort, ushort>> InternalMagAmmoToGunDict;
        private static Dictionary<ushort, bool> GunsWithInternalMags;
        private static Dictionary<CSteamID, InternalMagReloadState> ReloadExtensionStates;
        internal static void Init(InternalMagConfig config)
        {
            Debug = config.Debug;
            InternalMagAmmoToGunDict = createDictionaryForInternalMagAmmoToGun(config.InternalMagAmmoStacks, out var gunsWithInternalMags);
            GunsWithInternalMags = gunsWithInternalMags;
            ReloadExtensionStates = new Dictionary<CSteamID, InternalMagReloadState>();

            foreach (var ammoStackEntry in InternalMagAmmoToGunDict)
            {
                foreach (var gunMag in ammoStackEntry.Value.Values)
                {
                    ItemReplacer.tryAddReplacement(gunMag, ammoStackEntry.Key, ReplaceType.Keep, ReplaceType.Keep);
                }
            }
        }
        internal static void Cleanup()
        {
            InternalMagAmmoToGunDict.Clear();
            GunsWithInternalMags.Clear();
            ReloadExtensionStates.Clear();
        }
        internal static void OnPlayerDisconnected(UnturnedPlayer player)
        {
            ReloadExtensionStates.Remove(player.CSteamID);
        }
        internal static void OnPreAttachMag(UseableGun gun, byte page, byte x, byte y, byte[] hash)
        {
            ItemGunAsset asset = gun?.equippedGunAsset;
            if (asset == null || !GunsWithInternalMags.ContainsKey(asset.id))
                return;

            // save page of new mag
            UnturnedPlayer player = UnturnedPlayer.FromPlayer(gun.player);
            InternalMagReloadState state = new InternalMagReloadState { newMag = new ItemJarWrapper { page = page }, };

            // save and remove old mag from gun
            Item mag = InventoryHelper.getMagFromGun(player.Player.equipment);
            if (mag != null)
            {
                state.oldMag = mag;
                InventoryHelper.removeMagFromGun(player.Player.equipment);
                if(Debug)
                    Logger.Log($"Removed mag {mag.id} amount {mag.amount} from gun ({asset.id})");
            }

            if (ReloadExtensionStates.ContainsKey(player.CSteamID))
            {
                ReloadExtensionStates[player.CSteamID] = state;
            }
            else
            {
                ReloadExtensionStates.Add(player.CSteamID, state);
            }
        }
        internal static void OnChangeMagazine(PlayerEquipment equipment, UseableGun gun, Item oldItem, ItemJar newItem, ref bool shouldAllow)
        {
            UnturnedPlayer player = UnturnedPlayer.FromPlayer(equipment.player);
            if (!ReloadExtensionStates.TryGetValue(player.CSteamID, out InternalMagReloadState reloadState) || reloadState.newMag == null)
                return;
            // gun was successfully reloaded
            reloadState.magChanged = true;

            if (newItem == null)
                return;

            reloadState.wasUnload = false;
            // set new mag
            reloadState.newMag.itemJar = newItem;
            if (Debug)
                Logger.Log($"Reloaded {equipment.itemID} with reloadExtension old Mag: {(reloadState.oldMag != null ? reloadState.oldMag.id.ToString() : "none")} new Mag: {(newItem?.item != null ? newItem.item.id.ToString() : "none")}");

        }
        internal static void OnPostAttachMag(UseableGun gun)
        {
            UnturnedPlayer player = UnturnedPlayer.FromPlayer(gun.player);
            
            if (!ReloadExtensionStates.TryGetValue(player.CSteamID, out InternalMagReloadState reloadState))
                return;
            ReloadExtensionStates.Remove(player.CSteamID);

            if (!GunsWithInternalMags.ContainsKey(gun.equippedGunAsset.id))
                return;

            if (reloadState.magChanged)
            {
                checkInternalMagReload(player, gun, reloadState);
            }

            if (reloadState.oldMag == null)
                return;

            handleOldMag(player.Player, gun, reloadState.oldMag, reloadState.magChanged, reloadState.wasUnload);
        }
        #region Helper Functions
        private static void checkInternalMagReload(UnturnedPlayer player, UseableGun gun, InternalMagReloadState reloadState)
        {
            if (reloadState.newMag?.itemJar?.item == null || 
                reloadState.newMag.itemJar.item.amount <= 0 || 
                !tryConvertAmmoStackToMag(gun, reloadState.newMag.itemJar.item, out Item compatibleMag))
            {
                // no internal mag reload
                return;
            }

            ItemMagazineAsset magAsset = Assets.find(EAssetType.ITEM, compatibleMag.id) as ItemMagazineAsset;
            if (magAsset == null)
            {
                Logger.LogWarning($"Could not find ItemMagazineAsset for {compatibleMag.id}");
                return;
            }

            byte internalMagSize = magAsset.amount;
            Item oldMag = reloadState.oldMag;

            int remainder;
            if (oldMag?.id == compatibleMag.id)
            {
                int totalAmmo = compatibleMag.amount + oldMag.amount;
                if (Debug)
                    Logger.Log($"Total ammo = {totalAmmo}");
                compatibleMag.amount = 0;
                oldMag.amount = 0;
                remainder = InventoryHelper.safeAddItemAmount(compatibleMag, totalAmmo, internalMagSize);
                remainder = InventoryHelper.safeAddItemAmount(oldMag, remainder, internalMagSize);
            }
            else
            {
                byte currentAmount = compatibleMag.amount;
                compatibleMag.amount = 0;
                remainder = InventoryHelper.safeAddItemAmount(compatibleMag, currentAmount, internalMagSize);
            }

            if (Debug)
                Logger.Log($"Loaded ammo = {compatibleMag.amount} Remaining ammo = {remainder}");

            InventoryHelper.setMagForGun(player.Player.equipment, compatibleMag);

            addRemainingAmmoToInventory(player, remainder, reloadState.newMag);
        }
        private static void addRemainingAmmoToInventory(UnturnedPlayer player, int remainder, ItemJarWrapper itemJarWrapper)
        {
            if (remainder <= 0)
                return;
            
            if(itemJarWrapper?.itemJar?.item == null)
            {
                Logger.LogError($"Error: Could not give remaining ammo ({remainder}) because ItemJarWrapper or objects in it where null!");
                return;
            }

            ItemAsset asset = Assets.find(EAssetType.ITEM, itemJarWrapper.itemJar.item.id) as ItemAsset;
            if (asset == null)
            {
                Logger.LogError($"Error: Could not give remaining ammo ({remainder}) because ItemAsset for item id: {itemJarWrapper.itemJar.item.id} could not be found!");
                return;
            }
            if (Debug)
                Logger.Log($"Adding remaining ammo id: {itemJarWrapper.itemJar.item.id} amount: {remainder} to inventory ");

            InventoryHelper.safeAddItemAmountWithStacking(player.Player, itemJarWrapper.itemJar, itemJarWrapper.page, remainder, asset.amount);
        }
        private static void handleOldMag(Player player, UseableGun gun, Item oldMag, bool didReload, bool wasUnload)
        {

            if (!didReload)
            {
                // reset old mag as no reload was performed
                InventoryHelper.setMagForGun(player.equipment, oldMag);
                if (Debug)
                    Logger.Log($"Restored old mag id: {oldMag.id} amount: {oldMag.amount}");
                return;
            }

            // remove empty mag check
            ItemMagazineAsset magAsset = Assets.find(EAssetType.ITEM, oldMag.id) as ItemMagazineAsset;
            if (oldMag.amount <= 0 && 
                (gun.equippedGunAsset.shouldDeleteEmptyMagazines || (magAsset == null || magAsset.deleteEmpty)))
            {
                if (Debug)
                    Logger.Log($"Empty old mag {oldMag.id} amount: {oldMag.amount} was removed");
                return;
            }

            // converts mag to coresponding AmmoStack
            Item ammoStack = ItemReplacer.replaceItem(oldMag);
            if (ammoStack != null)
            {
                // add AmmoStack without stacking on unload
                if (wasUnload)
                {
                    player.inventory.forceAddItem(ammoStack, false);
                    if (Debug)
                        Logger.Log($"Unloaded ammo stack id: {ammoStack.id} amount: {ammoStack.amount} of old mag to inventory");
                    return;
                }
                // try unlaod with stacking
                if(InventoryHelper.trySafeAddItemAmountWithStacking(player, ammoStack, ammoStack.amount))
                {
                    if (Debug)
                        Logger.Log($"Added ammo stack id: {ammoStack.id} amount: {ammoStack.amount} of old mag with ammo stacking");
                    return;
                }
            }

            // backup if relacement failed
            player.inventory.forceAddItem(oldMag, false);
            if (Debug)
                Logger.Log($"Added old mag to inventory id: {oldMag.id} amount: {oldMag.amount}");
        }
        private static bool tryConvertAmmoStackToMag(UseableGun gun, Item ammoStack, out Item mag)
        {
            mag = null;
            if (InternalMagAmmoToGunDict.TryGetValue(ammoStack.id, out var compatibleGunsDict))
            {
                if (compatibleGunsDict.TryGetValue(gun.equippedGunAsset.id, out ushort magId))
                {
                    mag = new Item(magId, ammoStack.amount, ammoStack.durability);
                    return true;
                }
            }
            return false;
        }
        internal static Dictionary<ushort, Dictionary<ushort, ushort>> createDictionaryForInternalMagAmmoToGun(List<InternalMagAmmoStack> ammoStacks, out Dictionary<ushort, bool> gunsWithInternalMags)
        {
            var ammoStacktoGunDict = new Dictionary<ushort, Dictionary<ushort, ushort>>();
            gunsWithInternalMags = new Dictionary<ushort, bool>();
            if (ammoStacks != null)
            {
                foreach (var ammoStack in ammoStacks)
                {
                    if (ammoStack == null || ammoStack.Id == 0)
                    {
                        Logger.LogWarning("InternalMagAmmoStack was null or had Id 0 and was skipped");
                        continue;
                    }

                    if (ammoStacktoGunDict.ContainsKey(ammoStack.Id))
                    {
                        Logger.LogWarning($"InternalMagAmmoStack with Id: {ammoStack.Id} is a duplicate!"); 
                        continue;
                    }

                    ammoStacktoGunDict.Add(ammoStack.Id, new Dictionary<ushort, ushort>());
                    foreach (var gun in ammoStack.CompatibleGuns)
                    {
                        if (gun == null || gun.Id == 0)
                        {
                            Logger.LogWarning("InternalMagGun was null or had Id 0 and was skipped");
                            continue;
                        }
                        if (gun.InternalMagazine == null || gun.InternalMagazine.Id == 0)
                        {
                            Logger.LogWarning($"InternalMagazine of {gun.Id} in {ammoStack.Id} was null or had Id 0 and was skipped");
                            continue;
                        }
                        if (ammoStacktoGunDict[ammoStack.Id].ContainsKey(gun.Id))
                        {
                            Logger.LogWarning($"InternalMagGun with Id: {gun.Id} is a duplicate in {ammoStack.Id}!");
                            continue;
                        }
                        if (!gunsWithInternalMags.ContainsKey(gun.Id))
                        {
                            gunsWithInternalMags.Add(gun.Id, true);
                        }
                        ammoStacktoGunDict[ammoStack.Id].Add(gun.Id, gun.InternalMagazine.Id);
                    }
                }
            }
            return ammoStacktoGunDict;
        }
        #endregion
        internal class InternalMagReloadState
        {
            internal bool magChanged = false;
            internal bool wasUnload = true;
            internal ItemJarWrapper newMag = null;
            internal Item oldMag = null;
        }
    }
}
