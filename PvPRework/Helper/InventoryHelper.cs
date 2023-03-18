using System;
using System.Collections.Generic;
using Rocket.API;
using Rocket.Core.Logging;
using Rocket.Core.Plugins;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using Rocket.Unturned.Plugins;
using SDG.Unturned;
using SpeedMann.PvPRework.Models;

namespace SpeedMann.PvPRework.Helper
{
    public class InventoryHelper
    {
        public static ItemJar getItemJarOfEquiped(PlayerEquipment equipment)
        {
            if (equipment != null)
            {
                byte page = equipment.equippedPage;
                byte x = equipment.equipped_x;
                byte y = equipment.equipped_y;
                byte index = equipment.player.inventory.findIndex(page, x, y, out byte found_x, out byte found_y);
                if (index != 255)
                {
                    return equipment.player.inventory.getItem(page, index);
                }
            }
            return null;
        }
        public static void setMagForGun(PlayerEquipment gun, Item mag)
        {
            if (gun != null && gun.state.Length >= 18 && gun.useable is UseableGun)
            {
                byte[] array = BitConverter.GetBytes(mag.id);
                gun.state[8] = array[0];
                gun.state[9] = array[1];
                gun.state[10] = mag.amount;
                gun.state[17] = mag.quality;
                gun.sendUpdateState();
            }
        }
        public static void setMagForGun(Item gun, Item mag)
        {
            if (gun != null && gun.state.Length >= 18)
            {
                byte[] array = BitConverter.GetBytes(mag.id);
                gun.state[8] = array[0];
                gun.state[9] = array[1];
                gun.state[10] = mag.amount;
                gun.state[17] = mag.quality;
            }
        }
        public static Item getMagFromGun(PlayerEquipment gun)
        {
            if (gun != null && gun.state.Length >= 18 && gun.useable is UseableGun)
            {
                byte[] mag = new byte[] { gun.state[8], gun.state[9] };
                ushort itemId = BitConverter.ToUInt16(mag, 0);
                if (itemId > 0)
                {
                    return new Item(itemId, gun.state[10], gun.state[17]);
                }
            }
            return null;
        }
        public static Item getMagFromGun(Item gun)
        {
            if (gun != null && gun.state.Length >= 18)
            {
                byte[] mag = new byte[] { gun.state[8], gun.state[9] };
                ushort itemId = BitConverter.ToUInt16(mag, 0);
                if (itemId > 0)
                {
                    return new Item(itemId, gun.state[10], gun.state[17]);
                }
            }
            return null;
        }
        public static void removeMagFromGun(Item gun)
        {
            if (gun != null && gun.state.Length >= 18)
            {
                gun.state[8] = 0;
                gun.state[9] = 0;
                gun.state[10] = 0;
                gun.state[17] = 0;
            }
        }
        public static void removeMagFromGun(PlayerEquipment gun)
        {
            if (gun != null && gun.state.Length >= 18)
            {
                gun.state[8] = 0;
                gun.state[9] = 0;
                gun.state[10] = 0;
                gun.state[17] = 0;
                gun.sendUpdateState();
            }
        }
        public static void clearInventoryPage(UnturnedPlayer player, byte page)
        {
            if (player.Player.inventory.items[page] == null)
                return;
            while (player.Player.inventory.getItemCount(page) > 0)
            {
                player.Player.inventory.removeItem(page, 0);
            }
        }
        public static bool itemEquality(Item a, Item b)
        {
            bool equals = false;
            ItemAsset itemAssetA = Assets.find(EAssetType.ITEM, a.id) as ItemAsset;
            ItemAsset itemAssetB = Assets.find(EAssetType.ITEM, b.id) as ItemAsset;
            if (itemAssetA is ItemGunAsset && itemAssetB is ItemGunAsset)
            {
                equals = a != null && b != null
                                && a.id == b.id
                                && a.durability == b.durability
                                && a.quality == b.quality
                                && a.amount == b.amount
                                && a.state[0] == b.state[0] // Sight 1
                                && a.state[1] == b.state[1] // Sight 2
                                && a.state[2] == b.state[2] // Tactical 1
                                && a.state[3] == b.state[3] // Tactical 2
                                && a.state[4] == b.state[4] // Grip 1
                                && a.state[5] == b.state[5] // Grip 2
                                && a.state[6] == b.state[6] // Barrel 1
                                && a.state[7] == b.state[7] // Barrel 2
                                && a.state[8] == b.state[8] // Mag 1
                                && a.state[9] == b.state[9] // Mag 2
                                && a.state[10] == b.state[10]; // Mag Ammo
            }
            else
            {
                equals = a != null && b != null
                                && a.id == b.id
                                && a.durability == b.durability
                                && a.quality == b.quality
                                && a.amount == b.amount;
            }

            return equals;


        }
        public static int safeAddItemAmount(Item item, int amount)
        {
            ItemAsset asset = Assets.find(EAssetType.ITEM, item.id) as ItemAsset;
            if(asset == null)
            {
                return amount;
            }

            return safeAddItemAmount(item, amount, asset.amount);
        }
        public static int safeAddItemAmount(Item item, int amount, byte maxAmount)
        {
            int total = amount + item.amount;
            if (total > maxAmount)
            {
                item.amount = maxAmount;
                return total - maxAmount;
            }
            item.amount = (byte)total;
            return 0;
        }
        public static ushort findAmmo(PlayerInventory inventory, ushort itemId, out List<InventorySearch> foundAmmo)
        {
            ushort amount = 0;
            foundAmmo = inventory.search(itemId, false, true);
            foreach (InventorySearch search in foundAmmo)
            {
                amount += search.jar.item.amount;
            }
            return amount;
        }
        public static void safeAddItem(Player player, Item item, byte page, byte x, byte y, byte rot)
        {
            if (!player.inventory.tryAddItem(item, x, y, page, rot))
            {
                player.inventory.forceAddItem(item, false);
            }
        }
        public static bool getInvItems(UnturnedPlayer player, ref List<ItemJarWrapper> foundItems)
        {
            bool returnv = false;
            if (foundItems == null) return returnv;

            try
            {
                for (byte i = 0; i < player.Inventory.items.Length; i++)
                {
                    if (player.Inventory.items[i] == null) continue;

                    for (byte y = 0; y < player.Inventory.items[i].items.Count; y++)
                    {
                        ItemJar itemJ = player.Inventory.items[i].items[y];
                        foundItems.Add(new ItemJarWrapper(itemJ, i, y));
                    }
                    returnv = true;
                }

            }
            catch (Exception e)
            {
                Logger.Log("There was an error getting items from " + player.DisplayName + "'s inventory.  Here is the error.");
                Console.Write(e);
            }
            return returnv;
        }
        public static bool getClothingItems(UnturnedPlayer player, ref List<KeyValuePair<StorageType, Item>> foundItems)
        {
            bool returnv = false;
            if (foundItems == null) return returnv;

            try
            {
                List<ItemClothingAsset> clothing = new List<ItemClothingAsset>();

                clothing.Add(player.Player.clothing.hatAsset);
                clothing.Add(player.Player.clothing.maskAsset);
                clothing.Add(player.Player.clothing.glassesAsset);
                clothing.Add(player.Player.clothing.backpackAsset);
                clothing.Add(player.Player.clothing.vestAsset);
                clothing.Add(player.Player.clothing.shirtAsset);
                clothing.Add(player.Player.clothing.pantsAsset);

                foreach (ItemClothingAsset item in clothing)
                {
                    if (item != null)
                    {
                        byte quality = 0;
                        byte[] state = new byte[0];
                        StorageType type = StorageType.Unknown;

                        switch (item)
                        {
                            case ItemHatAsset hat:
                                quality = player.Player.clothing.hatQuality;
                                state = player.Player.clothing.hatState;
                                type = StorageType.Hat;
                                break;
                            case ItemMaskAsset mask:
                                quality = player.Player.clothing.maskQuality;
                                state = player.Player.clothing.maskState;
                                type = StorageType.Mask;
                                break;
                            case ItemGlassesAsset glasses:
                                quality = player.Player.clothing.glassesQuality;
                                state = player.Player.clothing.glassesState;
                                type = StorageType.Glasses;
                                break;
                            case ItemBackpackAsset backpack:
                                quality = player.Player.clothing.backpackQuality;
                                state = player.Player.clothing.backpackState;
                                type = StorageType.Backpack;
                                break;
                            case ItemVestAsset vest:
                                quality = player.Player.clothing.vestQuality;
                                state = player.Player.clothing.vestState;
                                type = StorageType.Vest;
                                break;
                            case ItemShirtAsset shirt:
                                quality = player.Player.clothing.shirtQuality;
                                state = player.Player.clothing.shirtState;
                                type = StorageType.Shirt;
                                break;
                            case ItemPantsAsset pants:
                                quality = player.Player.clothing.pantsQuality;
                                state = player.Player.clothing.pantsState;
                                type = StorageType.Pants;
                                break;
                        }
                        foundItems.Add(new KeyValuePair<StorageType, Item>(type, new Item(item.id, 1, quality, state)));
                    }
                }
                returnv = true;
            }
            catch (Exception e)
            {
                Logger.Log("There was an error getting clothes from " + player.DisplayName + "'.  Here is the error.");
                Console.Write(e);
            }
            return returnv;
        }
        public static bool clearAll(UnturnedPlayer player)
        {
            return clearInv(player) && clearClothes(player);
        }
        public static bool clearInv(UnturnedPlayer player)
        {
            bool returnv = false;
            try
            {
                player.Player.equipment.dequip();
                for (byte p = 0; p < PlayerInventory.PAGES; p++)
                {
                    if (player.Inventory.items[p] == null) continue;
                    byte itemc = player.Player.inventory.getItemCount(p);
                    if (itemc > 0)
                    {
                        for (byte p1 = 0; p1 < itemc; p1++)
                        {
                            player.Player.inventory.removeItem(p, 0);
                        }
                    }
                }
                // tell slot fix weapons on back and hip
                player.Player.inventory.channel.send("tellSlot", ESteamCall.ALL, ESteamPacket.UPDATE_RELIABLE_BUFFER, new object[]
                {
                    (byte)0,
                    (byte)0,
                    new byte[0]
                });
                player.Player.inventory.channel.send("tellSlot", ESteamCall.ALL, ESteamPacket.UPDATE_RELIABLE_BUFFER, new object[]
                {
                    (byte)1,
                    (byte)0,
                    new byte[0]
                });
                returnv = true;
            }
            catch (Exception e)
            {
                Logger.Log($"There was an error clearing {player.DisplayName}'s inventory.  Here is the error.");
                Console.Write(e);
            }
            return returnv;
        }
        public static bool clearClothes(UnturnedPlayer player)
        {
            bool returnv = false;

            byte oldWidth = player.Inventory.items[2].width;
            byte oldHeight = player.Inventory.items[2].height;

            player.Inventory.items[2].resize(10, 10);

            try
            {
                player.Player.clothing.askWearBackpack(0, 0, new byte[0], true);
                for (byte p2 = 0; p2 < player.Player.inventory.getItemCount(2); p2++)
                {
                    player.Player.inventory.removeItem(2, 0);
                }
                player.Player.clothing.askWearGlasses(0, 0, new byte[0], true);
                for (byte p2 = 0; p2 < player.Player.inventory.getItemCount(2); p2++)
                {
                    player.Player.inventory.removeItem(2, 0);
                }
                player.Player.clothing.askWearHat(0, 0, new byte[0], true);
                for (byte p2 = 0; p2 < player.Player.inventory.getItemCount(2); p2++)
                {
                    player.Player.inventory.removeItem(2, 0);
                }
                player.Player.clothing.askWearMask(0, 0, new byte[0], true);
                for (byte p2 = 0; p2 < player.Player.inventory.getItemCount(2); p2++)
                {
                    player.Player.inventory.removeItem(2, 0);
                }
                player.Player.clothing.askWearPants(0, 0, new byte[0], true);
                for (byte p2 = 0; p2 < player.Player.inventory.getItemCount(2); p2++)
                {
                    player.Player.inventory.removeItem(2, 0);
                }
                player.Player.clothing.askWearShirt(0, 0, new byte[0], true);
                for (byte p2 = 0; p2 < player.Player.inventory.getItemCount(2); p2++)
                {
                    player.Player.inventory.removeItem(2, 0);
                }
                player.Player.clothing.askWearVest(0, 0, new byte[0], true);
                for (byte p2 = 0; p2 < player.Player.inventory.getItemCount(2); p2++)
                {
                    player.Player.inventory.removeItem(2, 0);
                }
                returnv = true;
            }
            catch (Exception e)
            {
                Logger.Log("There was an error clearing " + player.DisplayName + "'s inventory.  Here is the error.");
                Console.Write(e);
            }
            finally
            {
                player.Inventory.items[2].resize(oldWidth, oldHeight);
            }
            return returnv;
        }
        public enum StorageType
        {
            PrimaryWeapon = 0,
            SecondaryWeapon = 1,
            Hands = 2,
            Backpack = 3,
            Vest = 4,
            Shirt = 5,
            Pants = 6,
            Hat = 7,
            Mask = 8,
            Glasses = 9,
            Unknown = 10,
        }
    }
}

