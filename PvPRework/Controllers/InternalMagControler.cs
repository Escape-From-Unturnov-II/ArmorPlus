using Rocket.Unturned.Player;
using SDG.Unturned;
using SpeedMann.PvPRework.Helper;
using SpeedMann.PvPRework.Models;
using SpeedMann.PvPRework.Models.Config;
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
        private static Dictionary<ushort, GunExtension> GunsWithInternalMags;
        private static Dictionary<ushort, ItemExtension> CompatibleAmmo;
        private static Dictionary<CSteamID, InternalMagReloadState> ReloadExtensionStates;
        internal static void Init(List<GunExtension> gunExtensions, List<ItemExtension> compatibleAmmo)
        {
            GunsWithInternalMags = createDictionaryFromInternalMagGuns(gunExtensions);
            CompatibleAmmo = PvPRework.createDictionaryFromItemExtensions(compatibleAmmo);
        }
        internal static void Cleanup()
        {
            GunsWithInternalMags.Clear();
            CompatibleAmmo.Clear();
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

            // save and remove old mag
            Item mag = InventoryHelper.getMagFromGun(player.Player.equipment);
            if (mag != null)
            {
                state.oldMag = mag;
                InventoryHelper.removeMagFromGun(player.Player.equipment);
                Logger.Log("Removed mag from gun");
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
            Logger.Log($"Changed magazine for: {equipment.itemID} old Mag: {(oldItem != null ? oldItem.id.ToString() : "none")} new Mag: {(newItem?.item != null ? newItem.item.id.ToString() : "none")}");

            if (!GunsWithInternalMags.ContainsKey(gun.equippedGunAsset.id))
                return;

            UnturnedPlayer player = UnturnedPlayer.FromPlayer(equipment.player);
            if (!ReloadExtensionStates.TryGetValue(player.CSteamID, out InternalMagReloadState reloadState) || reloadState.newMag == null)
                return;

            // save ammo stack
            Item AmmoStack = new Item(newItem.item.id, newItem.item.amount, newItem.item.quality);
            reloadState.newMag.itemJar = new ItemJar(newItem.x, newItem.y, newItem.rot, AmmoStack);
            reloadState.reloaded = true;
            Logger.Log("reloaded with reloadExtension!");

        }
        internal static void OnPostAttachMag(UseableGun gun)
        {
            UnturnedPlayer player = UnturnedPlayer.FromPlayer(gun.player);
            
            if (!ReloadExtensionStates.TryGetValue(player.CSteamID, out InternalMagReloadState reloadState))
                return;

            if (!GunsWithInternalMags.TryGetValue(gun.equippedGunAsset.id, out GunExtension gunExtension))
                return;

            if (reloadState.reloaded && reloadState.newMag?.itemJar?.item?.amount > 0 && CompatibleAmmo.ContainsKey(reloadState.newMag.itemJar.item.id))
            {
                handleInternalMagazineReload(player, reloadState, gunExtension.InternalMagazineSize);
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
                    Logger.Log("added old mag to inventory");
                }
                return;
            }

            // reset old mag
            InventoryHelper.setMagForGun(player.Player.equipment, reloadState.oldMag);
            Logger.Log("restored old mag");
        }
        #region Helper Functions
        private static void handleInternalMagazineReload(UnturnedPlayer player, InternalMagReloadState reloadState, byte internalMagazineSize)
        {
            ItemJar newMag = reloadState.newMag?.itemJar;
            Item oldMag = reloadState.oldMag;

            int remainder = 0;
            if (oldMag?.id == newMag?.item?.id)
            {
                int totalAmmo = newMag.item.amount + oldMag.amount;
                remainder = InventoryHelper.safeAddItemAmount(newMag.item, totalAmmo, internalMagazineSize);
                remainder = InventoryHelper.safeAddItemAmount(oldMag, remainder);
            }
            else
            {
                remainder = InventoryHelper.safeAddItemAmount(newMag.item, newMag.item.amount, internalMagazineSize);
            }
            
            player.Player.equipment.state[10] = newMag.item.amount;
            player.Player.equipment.sendUpdateState();

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

            Logger.Log("adding remaining ammo to inventory");
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
                Item remaining = new Item(itemJarWrapper.itemJar.item.id, newAmount, itemJarWrapper.itemJar.item.quality);
                InventoryHelper.saveAddItem(player, remaining, itemJarWrapper.itemJar.x, itemJarWrapper.itemJar.y, itemJarWrapper.page, itemJarWrapper.itemJar.rot);
            }
        }
        internal static Dictionary<ushort, GunExtension> createDictionaryFromInternalMagGuns(List<GunExtension> gunExtensions)
        {
            Dictionary<ushort, GunExtension> internalMagGunDict = new Dictionary<ushort, GunExtension>();
            if (gunExtensions != null)
            {
                foreach (GunExtension gunExtension in gunExtensions)
                {
                    if (gunExtension == null || gunExtension.Id == 0)
                    {
                        Logger.LogWarning("Item was null or had Id 0 and was skipped");
                        continue;
                    }


                    if (internalMagGunDict.ContainsKey(gunExtension.Id))
                    {
                        Logger.LogWarning("Item with Id:" + gunExtension.Id + " is a duplicate!"); 
                        continue;
                    }
                    
                    if(gunExtension.InternalMagazineSize > 0)
                    {
                        internalMagGunDict.Add(gunExtension.Id, gunExtension);
                    }
                }
            }
            return internalMagGunDict;
        }
        #endregion
        internal class InternalMagReloadState
        {
            internal bool reloaded = false;
            internal ItemJarWrapper newMag = null;
            internal Item oldMag = null;
        }
    }
}
