using Rocket.Core.Logging;
using Rocket.Unturned.Player;
using SpeedMann.PvPRework.Models.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeedMann.PvPRework.UI
{
    internal class ClothingEffectHandler
    {
        public static void checkClothingEffect<T>(Dictionary<ushort, T> clothingExtensions, UnturnedPlayer player, ushort clothingId, bool showUnequipEffect = false) where T : ItemUIExtension
        {
            if (player == null)
            {
                Logger.LogError("Clothing effect check for player null");
                return;
            }

            PVPReworkConfiguration conf = PvPRework.Conf;
            T clothingExtension;
            ushort equipedClothingId;
            short effectKey;

            if (typeof(T).Equals(typeof(GlassesExtension)))
            {
                if (conf.BetterArmor.GlassesEffectKey <= 0)
                    return;

                effectKey = conf.BetterArmor.GlassesEffectKey;
                equipedClothingId = player.Player.clothing.glasses;
            }
            else if (typeof(T).Equals(typeof(HatExtension)))
            {
                if (conf.BetterArmor.HatEffectKey <= 0)
                    return;

                effectKey = conf.BetterArmor.HatEffectKey;
                equipedClothingId = player.Player.clothing.hat;
            }
            else
            {
                Logger.LogError("Clothing effect check for unimplemented clothing type");
                return;
            }

            if (!showUnequipEffect && clothingExtensions.TryGetValue(equipedClothingId, out clothingExtension) && clothingExtension.EquipEffectId > 0)
            {
                EffectControler.spawnUI(clothingExtension.UnequipEffectId, effectKey, player.CSteamID);
                if (conf.Debug)
                    Logger.Log($"Clothing UI for Item: {equipedClothingId} disabled with: {clothingExtension.UnequipEffectId}");
            }
            if (clothingExtensions.TryGetValue(clothingId, out clothingExtension) && clothingExtension.EquipEffectId > 0)
            {
                EffectControler.spawnUI(clothingExtension.EquipEffectId, effectKey, player.CSteamID);
                if (conf.Debug)
                    Logger.Log($"Clothing UI for Item: {clothingId} enabled: {clothingExtension.EquipEffectId}");
            }
        }

    }
}
