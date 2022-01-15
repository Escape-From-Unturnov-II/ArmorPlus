using Rocket.Core.Logging;
using Rocket.Unturned.Player;
using SDG.NetTransport;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using Logger = Rocket.Core.Logging.Logger;
using UnityEngine;

namespace SpeedMann.PvPRework
{


    class EffectController
    {
        //EventManager UI 
        public static short HatEffectKey = 5210;

        public static void spawnUI(ushort effectId)
        {
            EffectManager.sendUIEffect(effectId, HatEffectKey, true);
        }
        public static void spawnUI(ushort effectId, CSteamID executorID)
        {
            ITransportConnection transportConnection = Provider.findTransportConnection(executorID);
            if (transportConnection == null)
            {
                Logger.LogError("Error in Event UI while trying to show UI (CSteamID not found)");
                return;
            }
            EffectManager.sendUIEffect(effectId, HatEffectKey, transportConnection, false);

        }
        public static void setVisibility(bool visible, string panelName)
        {
            foreach (SteamPlayer player in Provider.clients)
            {
                UnturnedPlayer uPlayer = UnturnedPlayer.FromSteamPlayer(player);
                setVisibility(visible, panelName, uPlayer.CSteamID);
            }
        }
        public static void setVisibility(bool visible, string panelName, CSteamID executorID)
        {
            ITransportConnection transportConnection = Provider.findTransportConnection(executorID);
            if (transportConnection == null)
            {
                Logger.LogError("Error in Event UI while trying to hide UI (CSteamID not found)");
                return;
            }
            EffectManager.sendUIEffectVisibility(HatEffectKey, transportConnection, false, panelName, visible);
        }
    }
}
