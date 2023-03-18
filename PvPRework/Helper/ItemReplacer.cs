using Rocket.Unturned.Enumerations;
using SDG.Unturned;
using SpeedMann.PvPRework.Models.Config.ItemExtensions;
using System.Collections.Generic;
using System.Security.Policy;
using UnityEngine;
using Logger = Rocket.Core.Logging.Logger;

namespace SpeedMann.PvPRework.Helper
{
    public class ItemReplacer
    {
        private static Dictionary<ushort, ItemReplaceInfo> Replacement;
        public static void Init(List<ItemReplaceInfo> replacements)
        {
            Replacement = createReplacementDictionary(replacements);
        }
        public static void Cleanup()
        {
            Replacement.Clear();
        }
        public static void checkReplaceItem(Player player, InventoryGroup inventoryGroup, byte inventoryIndex, ItemJar P)
        {
            if (P?.item == null || !Replacement.TryGetValue(P.item.id, out var replaceInfo))
                return;

            player.inventory.removeItem((byte)inventoryGroup, inventoryIndex);

            Item replacement = new Item(replaceInfo.Id,
                getNewAmount(replaceInfo.AmmountReplacementType, P.item.amount, replaceInfo.Id),
                getNewDurability(replaceInfo.DurabilityReplacementType, P.item.durability, replaceInfo.Id));

            InventoryHelper.safeAddItem(player, replacement, (byte)inventoryGroup, P.x, P.y, P.rot);
        }
        public static Item replaceItem(Item item)
        {
            if(item == null || !Replacement.TryGetValue(item.id, out var replaceInfo))
                return item;

            return new Item(replaceInfo.Id, 
                getNewAmount(replaceInfo.AmmountReplacementType, item.amount, replaceInfo.Id), 
                getNewDurability(replaceInfo.DurabilityReplacementType, item.durability, replaceInfo.Id));
        }
        public static byte getNewDurability(ReplaceType replaceType, byte originalDurability, ushort id)
        {
            switch (replaceType)
            {
                case ReplaceType.Empty:
                    return 0;
                case ReplaceType.Keep:
                    return originalDurability;
                case ReplaceType.Full:
                    return 100;
            }
            return originalDurability;
        }
        public static byte getNewAmount(ReplaceType replaceType, byte originalAmount, ushort id)
        {
            switch (replaceType)
            {
                case ReplaceType.Empty:
                    return 0;
                case ReplaceType.Keep:
                    return originalAmount;
                case ReplaceType.Full:
                    var asset = Assets.find(EAssetType.ITEM, id) as ItemAsset;
                    if(asset == null)
                    {
                        Logger.LogError($"Could not find ItemAsset with id {id} for item replace");
                        return originalAmount;
                    }
                    return asset.amount;
            }
            return originalAmount;
        }
        #region Helper Functions
        internal static Dictionary<ushort, ItemReplaceInfo> createReplacementDictionary(List<ItemReplaceInfo> replacements)
        {
            Dictionary<ushort, ushort> existingReplacements = new Dictionary<ushort, ushort>();
            Dictionary<ushort, ItemReplaceInfo> replacementDict = new Dictionary<ushort, ItemReplaceInfo>();
            if (replacements != null)
            {
                foreach (var replace in replacements)
                {
                    if (replace == null || replace.Id == 0)
                    {
                        Logger.LogWarning("Item replacement cant be 0 or null and was skipped");
                        continue;
                    }
                    if (replacementDict.ContainsKey(replace.Id))
                    {
                        Logger.LogWarning($"Cant create replacement to {replace.Id} it is already a replace target");
                        continue;
                    }

                    bool didAdd = false;
                    foreach (var target in replace.ReplaceTargets)
                    {
                        if (target == null || target.Id == 0)
                        {
                            Logger.LogWarning("Item replace target cant be 0 or null and was skipped");
                            continue;
                        }
                        if(target.Id == replace.Id)
                        {
                            Logger.LogWarning($"Cant replace {replace.Id} with itself");
                            continue;
                        }
                        if (existingReplacements.ContainsKey(replace.Id))
                        {
                            Logger.LogWarning($"Cant replace {replace.Id} it is already a replace result");
                            continue;
                        }
                        if (replacementDict.TryGetValue(target.Id, out ItemReplaceInfo currentReplace))
                        {
                            Logger.LogWarning($"Item with Id: {target.Id} cant have to replacements, it is already getting replaced by {currentReplace.Id}!");
                            continue;
                        }
                        replacementDict.Add(target.Id, replace);
                        didAdd = true;
                    }
                    if (didAdd)
                    {
                        existingReplacements.Add(replace.Id, replace.Id);
                    }
                }
            }
            return replacementDict;
        }
        #endregion
    }
}
