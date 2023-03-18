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
            if (!GunsWithInternalMags.ContainsKey(gun.equippedGunAsset.id))
                return;

            UnturnedPlayer player = UnturnedPlayer.FromPlayer(equipment.player);
            if (!ReloadExtensionStates.TryGetValue(player.CSteamID, out InternalMagReloadState reloadState) || reloadState.newMag == null)
                return;
            
            reloadState.reloaded = true;

            if (newItem == null)
                return;

            ushort newId = newItem.item.id;
            if (InternalMagAmmoToGunDict.TryGetValue(newItem.item.id, out var compatibleGunsDict))
            {
                if(compatibleGunsDict.TryGetValue(gun.equippedGunAsset.id, out ushort magId))
                {
                    // set state and replace ammo stack id with the coresponding mag id of the gun
                    reloadState.internalMagReload = true;
                    newId = magId;
                }
            }
            // save ammo
            Item newMag = new Item(newId, newItem.item.amount, newItem.item.quality);
            reloadState.newMag.itemJar = new ItemJar(newItem.x, newItem.y, newItem.rot, newMag);
            if (Debug)
                Logger.Log($"Reloaded {equipment.itemID} with reloadExtension old Mag: {(oldItem != null ? oldItem.id.ToString() : "none")} new Mag: {(newItem?.item != null ? newItem.item.id.ToString() : "none")}");

        }
        internal static void OnPostAttachMag(UseableGun gun)
        {
            UnturnedPlayer player = UnturnedPlayer.FromPlayer(gun.player);
            
            if (!ReloadExtensionStates.TryGetValue(player.CSteamID, out InternalMagReloadState reloadState))
                return;

            if (reloadState.internalMagReload && reloadState.newMag?.itemJar?.item?.amount > 0)
            {
                ItemMagazineAsset magAsset = Assets.find(EAssetType.ITEM, reloadState.newMag.itemJar.item.id) as ItemMagazineAsset;
                if(magAsset != null)
                {
                    handleInternalMagazineReload(player, reloadState, magAsset.amount);
                }
            }
            
            if (reloadState.oldMag == null)
                return;

            // give old mag
            if (reloadState.reloaded)
            {
                ItemMagazineAsset magAsset = Assets.find(EAssetType.ITEM, reloadState.oldMag.id) as ItemMagazineAsset;
                if (reloadState.oldMag.amount > 0 || (!gun.equippedGunAsset.shouldDeleteEmptyMagazines && (magAsset == null || !magAsset.deleteEmpty)))
                {
                    player.GiveItem(reloadState.oldMag);
                    if(Debug)
                        Logger.Log($"added old mag to inventory id: {reloadState.oldMag.id} amount: {reloadState.oldMag.amount}");
                }
                else
                {
                    Logger.Log($"Empty old mag {reloadState.oldMag.id} amount: {reloadState.oldMag.amount} was removed");
                }
                return;
            }

            // reset old mag
            InventoryHelper.setMagForGun(player.Player.equipment, reloadState.oldMag);
            if (Debug)
                Logger.Log($"restored old mag id: {reloadState.oldMag.id} amount: {reloadState.oldMag.amount}");
        }
        #region Helper Functions
        private static void handleInternalMagazineReload(UnturnedPlayer player, InternalMagReloadState reloadState, byte internalMagazineSize)
        {
            ItemJar newMag = reloadState.newMag?.itemJar;
            Item oldMag = reloadState.oldMag;

            int remainder = 0;
            if(newMag?.item != null)
            {
                if (oldMag?.id == newMag.item.id)
                {
                    int totalAmmo = newMag.item.amount + oldMag.amount;
                    if (Debug)
                        Logger.Log($"Total ammo = {totalAmmo}");
                    newMag.item.amount = 0;
                    oldMag.amount = 0;
                    remainder = InventoryHelper.safeAddItemAmount(newMag.item, totalAmmo, internalMagazineSize);
                    remainder = InventoryHelper.safeAddItemAmount(oldMag, remainder);
                }
                else
                {
                    byte oldAmmo = newMag.item.amount;
                    newMag.item.amount = 0;
                    remainder = InventoryHelper.safeAddItemAmount(newMag.item, oldAmmo, internalMagazineSize);
                }
            }
            if (Debug)
                Logger.Log($"Loaded ammo = {newMag.item.amount} Remaining ammo = {remainder}");

            InventoryHelper.setMagForGun(player.Player.equipment, newMag.item);

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
            // check for replace to fill valid ammo stacks
            Item item = ItemReplacer.replaceItem(itemJarWrapper.itemJar.item);
            if(item == null)
            {
                item = itemJarWrapper.itemJar.item;
            }
            ItemAsset asset = Assets.find(EAssetType.ITEM, item.id) as ItemAsset;
            if (asset == null)
            {
                Logger.LogError($"Error: Could not give remaining ammo ({remainder}) because ItemAsset for item id: {item.id} could not be found!");
                return;
            }
            if (Debug)
                Logger.Log($"adding remaining ammo to inventory id: {item.id} amount: {remainder}");

            // refill ammo stacks
            InventoryHelper.findAmmo(player.Inventory, item.id, out List<InventorySearch> searchResult);
            foreach (var result in searchResult)
            {
                if (remainder <= 0)
                    break;
                remainder = InventoryHelper.safeAddItemAmount(player.Player, result.jar, result.page, remainder, asset.amount);
            }
            while (remainder > 0)
            {
                // give remaining ammo
                byte newAmount;
                if (remainder > asset.amount)
                {
                    newAmount = asset.amount;
                    remainder -= asset.amount;
                }
                else
                {
                    newAmount = (byte)remainder;
                    remainder = 0;
                }
                Item remainingItem = new Item(item.id, newAmount, item.quality);
                InventoryHelper.safeAddItem(player.Player, remainingItem, itemJarWrapper.page, itemJarWrapper.itemJar.x, itemJarWrapper.itemJar.y, itemJarWrapper.itemJar.rot);
            }
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
            internal bool reloaded = false;
            internal bool internalMagReload = false;
            internal ItemJarWrapper newMag = null;
            internal Item oldMag = null;
        }
    }
}
