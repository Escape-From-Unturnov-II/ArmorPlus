using HarmonyLib;
using Rocket.Unturned.Enumerations;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
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
        public delegate void PrePreAddItem(UnturnedPlayer player, Items page, Item item, ref bool shouldAllow);
        public static event PrePreAddItem OnPreAddItem;

        public delegate void PostCheckHeightClearance(PlayerStance stance, ref bool shouldAllow);
        public static event PostCheckHeightClearance OnPostCheckHeightClearance;

        public delegate void PostGetInput(ref InputInfo inputInfo);
        public static event PostGetInput OnPostGetInput;

        public delegate void PostVisualToggle(PlayerClothing playerClothing, EVisualToggleType type, bool toggle);
        public static event PostVisualToggle OnPostVisualToggle;

        public delegate void PreWearHat(Player player, ushort newHatId, byte quality, byte[] state, ref bool shouldAllow);
        public static event PreWearHat OnPreChangeHat;

        public delegate void PreWearGlasses(Player player, ushort newGlassesId, byte quality, byte[] state, ref bool shouldAllow);
        public static event PreWearGlasses OnPreChangeGlasses;

        public delegate void PreVisionChanged(Player player, ushort glassesId, bool ativate);
        public static event PreVisionChanged OnPreVisionChanged;

        public delegate void PostPlayerRevive(PlayerLife playerLife);
        public static event PostPlayerRevive OnPostPlayerRevive;

        public delegate void PrePlayerDamaged(PlayerLife playerLife, ref byte amount, EDeathCause cause, ref ELimb limb, CSteamID killer, ref bool canCauseBleeding, ref bool shouldAllow);
        public static event PrePlayerDamaged OnPrePlayerDamaged;

        public delegate void PreDisconnectSave(CSteamID steamID, ref bool shouldAllow);
        public static event PreDisconnectSave OnPreDisconnectSave;
        #endregion

        #region Patches
        [HarmonyPatch(typeof(PlayerLife), "ReceiveRevive")]
        class PlayerRevive
        {
            [HarmonyPrefix]
            internal static void OnPreReviveInvoker(PlayerLife __instance, out PlayerLife __state)
            {
                __state = __instance;
            }
            [HarmonyPostfix]
            internal static void OnPosReviveInvoker(PlayerLife __state)
            {
                OnPostPlayerRevive?.Invoke(__state);
            }
        }
        // Movement Patch
        [HarmonyPatch(typeof(PlayerStance), nameof(PlayerStance.hasHeightClearanceAtPosition))]
        class HeightClearancePatch{
            [HarmonyPrefix]
            internal static void OnPostCheckHeightClearanceInvoker(PlayerStance __instance, out PlayerStance __state)
            {
                __state = __instance;
            }
            [HarmonyPostfix]
            internal static void OnPostCheckHeightClearanceInvoker(PlayerStance __state, ref bool __result)
            {
                OnPostCheckHeightClearance?.Invoke(__state, ref __result);
            }
        }
        // Hat cycling
        [HarmonyPatch(typeof(Items), nameof(Items.tryAddItem), new Type[] { typeof(Item), typeof(bool) })]
        class AddItemPatch
        {
            [HarmonyPrefix]
            internal static bool OnPreItemsAddItemInvoker(Items __instance, Item item, ref bool __result)
            {
                bool shouldAllow = true;
                object target = __instance.onStateUpdated.Target;
                if (target is PlayerInventory)
                {
                    UnturnedPlayer uPlayer = UnturnedPlayer.FromPlayer(((PlayerInventory)target).player);
                    if (uPlayer != null)
                    {
                        
                        OnPreAddItem?.Invoke(uPlayer, __instance, item, ref shouldAllow);
                        if(shouldAllow == false)
                        {
                            __result = true;
                        }
                    }
                }
                return shouldAllow;
            }
        }
        [HarmonyPatch(typeof(PlayerLife), "doDamage")]
        class GotDamaged
        {
            [HarmonyPrefix]
            internal static bool OnPreDoDamageInvoker(PlayerLife __instance, ref byte amount, EDeathCause newCause, ref ELimb newLimb, CSteamID newKiller, ref bool canCauseBleeding)
            {
                bool shouldAllow = true;
                OnPrePlayerDamaged?.Invoke(__instance, ref amount, newCause, ref newLimb, newKiller, ref canCauseBleeding, ref shouldAllow);
                return shouldAllow;
            }
        }
        // Hit Zones
        [HarmonyPatch(typeof(PlayerInput), nameof(PlayerInput.getInput), new Type[] { typeof(bool), typeof(ERaycastInfoUsage) })]
        class PlayerInputPatch
        {
            [HarmonyPostfix]
            internal static void OnPostGetInputInvoker(ref InputInfo __result)
            {
                OnPostGetInput?.Invoke(ref __result);
            }
        }
        // Cosmetics
        internal class VisualToggleState
        {
            internal PlayerClothing playerClothing;
            internal EVisualToggleType type;
            internal bool toggle;
        }
        [HarmonyPatch(typeof(PlayerClothing), nameof(PlayerClothing.ReceiveVisualToggleState))]
        class VisualToggelPatch
        {
            [HarmonyPrefix]
            internal static bool OnPreVisualToggleInvoker(PlayerClothing __instance, EVisualToggleType type, bool toggle, out VisualToggleState __state)
            {
                __state = new VisualToggleState
                {
                    playerClothing = __instance,
                    type = type,
                    toggle = toggle,
                };
                return true;
            }

            [HarmonyPostfix]
            internal static void OnPostVisualToggleInvoker(VisualToggleState __state)
            {
                OnPostVisualToggle?.Invoke(__state.playerClothing, __state.type, __state.toggle);
            }
        }

        [HarmonyPatch(typeof(SaveManager), "onServerDisconnected")]
        class DisconnectSave
        {
            [HarmonyPrefix]
            internal static bool OnPreDisconnectSaveInvoker(CSteamID steamID)
            {
                var shouldAllow = true;
                OnPreDisconnectSave?.Invoke(steamID, ref shouldAllow);
                return shouldAllow;
            }
        }

        #region UI Patches
        [HarmonyPatch(typeof(PlayerClothing), nameof(PlayerClothing.ReceiveWearHat), new Type[] { typeof(Guid), typeof(byte), typeof(byte[]), typeof(bool) })]
        class PlayerWearHatPatch
        {
            [HarmonyPrefix]
            internal static bool OnPreWearHatInvoker(PlayerClothing __instance, Guid id, byte quality, byte[] state)
            {
                bool shouldAllow = true;
                Asset asset = Assets.find(id);
                if (asset != null)
                {
                    OnPreChangeHat?.Invoke(__instance.player, asset.id, quality, state, ref shouldAllow);
                }

                return shouldAllow;
            }
        }

        [HarmonyPatch(typeof(PlayerClothing), nameof(PlayerClothing.askWearHat), new Type[] { typeof(ushort), typeof(byte), typeof(byte[]), typeof(bool) })]
        class PlayerWearHatPatch2
        {
            [HarmonyPrefix]
            internal static bool OnPreWearHatInvoker(PlayerClothing __instance, ushort id, byte quality, byte[] state)
            {
                bool shouldAllow = true;
                OnPreChangeHat?.Invoke(__instance.player, id, quality, state, ref shouldAllow);
                return shouldAllow;
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
        [HarmonyPatch(typeof(PlayerClothing), nameof(PlayerClothing.ReceiveWearGlasses), new Type[] { typeof(Guid), typeof(byte), typeof(byte[]), typeof(bool) })]
        class PlayerWearGlassesPatch
        {
            [HarmonyPrefix]
            internal static bool OnPreWearGlassesInvoker(PlayerClothing __instance, Guid id, byte quality, byte[] state)
            {
                bool shouldAllow = true;
                Asset asset = Assets.find(id);
                if (asset != null)
                {
                    OnPreChangeGlasses?.Invoke(__instance.player, asset.id, quality, state, ref shouldAllow);
                }

                return shouldAllow;
            }
        }
        [HarmonyPatch(typeof(PlayerClothing), nameof(PlayerClothing.askWearGlasses), new Type[] { typeof(ushort), typeof(byte), typeof(byte[]), typeof(bool) })]
        class PlayerWearGlassesPatch2
        {
            [HarmonyPrefix]
            internal static bool OnPreWearGlassesInvoker(PlayerClothing __instance, ushort id, byte quality, byte[] state)
            {
                bool shouldAllow = true;
                OnPreChangeGlasses?.Invoke(__instance.player, id, quality, state, ref shouldAllow);
                if (shouldAllow)
                    OnPreVisionChanged?.Invoke(__instance.player, __instance.player.clothing.glasses, false);
                return shouldAllow;
            }
        }
        #endregion

        #endregion
    }
}
