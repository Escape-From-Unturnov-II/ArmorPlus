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
        public static void spawnUI(ushort effectId, short effectKey)
        {
            EffectManager.sendUIEffect(effectId, effectKey, true);
            
        }
        public static void spawnUI(ushort effectId, short effectKey, CSteamID executorID)
        {
            ITransportConnection transportConnection = Provider.findTransportConnection(executorID);
            if (transportConnection == null)
            {
                Logger.LogError("Error in Event UI while trying to show UI (CSteamID not found)");
                return;
            }
            EffectManager.sendUIEffect(effectId, effectKey, transportConnection, true);
        }
        public static void setVisibility(bool visible, short effectKey, string panelName)
        {
            foreach (SteamPlayer player in Provider.clients)
            {
                UnturnedPlayer uPlayer = UnturnedPlayer.FromSteamPlayer(player);
                setVisibility(visible, effectKey, panelName, uPlayer.CSteamID);
            }
        }
        public static void setVisibility(bool visible, short effectKey, string panelName, CSteamID executorID)
        {
            ITransportConnection transportConnection = Provider.findTransportConnection(executorID);
            if (transportConnection == null)
            {
                Logger.LogError("Error in Event UI while trying to hide UI (CSteamID not found)");
                return;
            }
            EffectManager.sendUIEffectVisibility(effectKey, transportConnection, false, panelName, visible);
        }
    }
}
