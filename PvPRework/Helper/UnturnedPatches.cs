using HarmonyLib;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Logger = Rocket.Core.Logging.Logger;

namespace SpeedMann.PvPRework
{
    class UnturnedPatches
    {
        private static Harmony harmony;
        private static string harmonyId = "SpeedMann.PvPRework";
        public static void Init()
        {
            try
            {
                harmony = new Harmony(harmonyId);
                harmony.PatchAll();
                if (PvPRework.Conf.Debug)
                {
                    var myOriginalMethods = harmony.GetPatchedMethods();
                    Logger.Log("Patched Methods:");
                    foreach (var method in myOriginalMethods)
                    {
                        Logger.Log(" " + method.ToString());
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogError($"ArmorPlus patches: {e.Message}");
            }
        }
        public static void Cleanup()
        {
            try
            {
                harmony.UnpatchAll(harmonyId);

                if (PvPRework.Conf.Debug)
                {
                    var myOriginalMethods = harmony.GetPatchedMethods();
                    Logger.Log("Patched Methods:");
                    foreach (var method in myOriginalMethods)
                    {
                        Logger.Log(" " + method.ToString());
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogError($"ArmorPlus patches: {e.Message}");
            }
        }
        #region Events
        public delegate void PostGetInput(ref InputInfo inputInfo);
        public static event PostGetInput OnPostGetInput;

        public delegate void PreWearHat(Player player, ushort newHatId);
        public static event PreWearHat OnPreChangeHat;

        public delegate void PreVisionChanged(Player player, ushort glassesId, bool ativate);
        public static event PreVisionChanged OnPreVisionChanged;
        #endregion

        #region Patches
        [HarmonyPatch(typeof(PlayerInput), nameof(PlayerInput.getInput), new Type[] { typeof(bool), typeof(ERaycastInfoUsage) })]
        class PlayerInputPatch
        {
            [HarmonyPostfix]
            internal static void OnPostGetInputInvoker(ref InputInfo __result)
            {
                OnPostGetInput?.Invoke(ref __result);
            }
        }


        [HarmonyPatch(typeof(PlayerClothing), nameof(PlayerClothing.ReceiveWearHat), new Type[] { typeof(Guid), typeof(byte), typeof(byte[]) })]
        class PlayerWearHatPatch
        {
            [HarmonyPrefix]
            internal static bool OnPreWearHatInvoker(PlayerClothing __instance, Guid id)
            {
                Asset asset = Assets.find(id);
                if (asset != null)
                {
                    OnPreChangeHat?.Invoke(__instance.player, asset.id);
                }

                return true;
            }
        }
        [HarmonyPatch(typeof(PlayerClothing), nameof(PlayerClothing.askWearHat), new Type[] { typeof(ushort), typeof(byte), typeof(byte[]), typeof(bool) })]
        class PlayerWearHatPatch2
        {
            [HarmonyPrefix]
            internal static bool OnPreWearHatInvoker(PlayerClothing __instance, ushort id)
            {
                OnPreChangeHat?.Invoke(__instance.player, id);
                return true;
            }
        }
        [HarmonyPatch(typeof(Player), nameof(Player.updateGlassesLights))]
        class EquipmentToggleVision
        {
            [HarmonyPrefix]
            internal static bool OnPreChangeVisionInvoker(Player __instance, bool on)
            {
                OnPreVisionChanged?.Invoke(__instance, __instance.clothing.glasses, on);
                return true;
            }
        }
        [HarmonyPatch(typeof(PlayerClothing), nameof(PlayerClothing.askWearGlasses), new Type[] { typeof(ushort), typeof(byte), typeof(byte[]), typeof(bool) })]
        class PlayerWearGlassesPatch
        {
            [HarmonyPrefix]
            internal static bool OnPreWearGlassesInvoker(PlayerClothing __instance, ushort id)
            {
                OnPreVisionChanged?.Invoke(__instance.player, __instance.player.clothing.glasses, false);
                return true;
            }
        }
        #endregion
    }
}
