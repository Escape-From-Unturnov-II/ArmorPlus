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
        public static void Init()
        {
            try
            {
                Harmony harmony = new Harmony("SpeedMann.PvPRework");
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
                Logger.LogError($"EventLoad: {e.Message}");
            }
        }
        #region Events
        public delegate void PostGetInput(ref InputInfo inputInfo);

        public static event PostGetInput OnPostGetInput;
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
        #endregion
    }
}
