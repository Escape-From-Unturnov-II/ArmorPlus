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
        private static Dictionary<ushort, bool> ExistingReplacementsResults;
        private static Dictionary<ushort, ItemReplaceInfo> Replacement;
        public static void Init(List<ItemReplaceInfo> replacements)
        {
            Replacement = createReplacementDictionary(replacements, out var existingReplacements);
            ExistingReplacementsResults = existingReplacements;
        }
        public static void Cleanup()
        {
            Replacement.Clear();
            ExistingReplacementsResults.Clear();
        }
        public static bool tryAddReplacement(ushort replaceTragetId, ushort replaceResultId, ReplaceType amountReplace, ReplaceType durabilityReplace)
        {
            if(replaceTragetId == 0 || 
                replaceResultId == 0 ||
                replaceResultId == replaceTragetId ||
                Replacement.ContainsKey(replaceTragetId) ||
                ExistingReplacementsResults.ContainsKey(replaceTragetId))
            {
                return false;
            }
            
            Replacement.Add(replaceTragetId, new ItemReplaceInfo(replaceResultId, amountReplace, durabilityReplace));
            return true;
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
        internal static Dictionary<ushort, ItemReplaceInfo> createReplacementDictionary(List<ItemReplaceInfo> replacements, out Dictionary<ushort, bool> existingReplacementResults)
        {
            existingReplacementResults = new Dictionary<ushort, bool>();
            Dictionary<ushort, ItemReplaceInfo> replacementDict = new Dictionary<ushort, ItemReplaceInfo>();
            if (replacements != null)
            {
                foreach (var replaceResult in replacements)
                {
                    if (replaceResult == null || replaceResult.Id == 0)
                    {
                        Logger.LogWarning("Item replace result can not be 0 or null and was skipped");
                        continue;
                    }
                    if (replacementDict.ContainsKey(replaceResult.Id))
                    {
                        Logger.LogWarning($"Can not create replace result {replaceResult.Id}, it is already a replace target");
                        continue;
                    }

                    bool didAdd = false;
                    foreach (var target in replaceResult.ReplaceTargets)
                    {
                        if (target == null || target.Id == 0)
                        {
                            Logger.LogWarning("Item replace target can not be 0 or null, it was skipped");
                            continue;
                        }
                        if(target.Id == replaceResult.Id)
                        {
                            Logger.LogWarning($"Can not replace {target.Id} with itself, it was skipped");
                            continue;
                        }
                        if (existingReplacementResults.ContainsKey(replaceResult.Id))
                        {
                            Logger.LogWarning($"Can not set {replaceResult.Id} as replace target, it is already a replace result and was skipped");
                            continue;
                        }
                        if (replacementDict.TryGetValue(target.Id, out ItemReplaceInfo currentReplace))
                        {
                            Logger.LogWarning($"Item with Id: {target.Id} is already a replace target for {currentReplace.Id}, it can not have two replace results and was skipped");
                            continue;
                        }
                        replacementDict.Add(target.Id, replaceResult);
                        didAdd = true;
                    }
                    if (didAdd)
                    {
                        existingReplacementResults.Add(replaceResult.Id, true);
                    }
                }
            }
            return replacementDict;
        }
        #endregion
    }
}
