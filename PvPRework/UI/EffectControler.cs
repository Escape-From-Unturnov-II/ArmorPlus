using Rocket.Core.Logging;
using Rocket.Unturned.Player;
using SDG.NetTransport;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using Logger = Rocket.Core.Logging.Logger;
using UnityEngine;
using SpeedMann.PvPRework.Models.Config;

namespace SpeedMann.PvPRework
{


    public class EffectControler
    {
        class UI_Status
        {
            internal short Key = 0;
            internal ushort EffectId = 0;
        }

        static Dictionary<ulong, List<UI_Status>> PlayerUIStatus;

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
            if (panelName == "") return;

            ITransportConnection transportConnection = Provider.findTransportConnection(executorID);
            if (transportConnection == null)
            {
                Logger.LogError("Error in Event UI while trying to hide UI (CSteamID not found)");
                return;
            }
            EffectManager.sendUIEffectVisibility(effectKey, transportConnection, true, panelName, visible);
        }
        public static void setUIValue(short effectKey, string childName, string value)
        {
            if (childName == "") return;

            foreach (SteamPlayer player in Provider.clients)
            {
                UnturnedPlayer uPlayer = UnturnedPlayer.FromSteamPlayer(player);
                setUIValue(effectKey, uPlayer.CSteamID, childName, value);
            }
        }
        public static void setUIValue(short effectKey, CSteamID executorID, string childName, string value)
        {
            if (childName == "") return;

            ITransportConnection transportConnection = Provider.findTransportConnection(executorID);
            if (transportConnection == null)
            {
                Logger.LogError("Error in Event UI while trying to hide UI (CSteamID not found)");
                return;
            }
            EffectManager.sendUIEffectText(effectKey, transportConnection, true, childName, value);
        }
        private static UI_Status getUIStatus(ulong steamId, short key)
        {
            if(PlayerUIStatus == null)
            {
                PlayerUIStatus = new Dictionary<ulong, List<UI_Status>>();
            }
            List<UI_Status> UI_Statuses;
            if (!PlayerUIStatus.TryGetValue(steamId, out UI_Statuses))
            {
                UI_Statuses = new List<UI_Status>();
                PlayerUIStatus.Add(steamId, UI_Statuses);
            }
            UI_Status status = UI_Statuses.Find( x => x.Key == key);
            if (status == null)
            {
                status = new UI_Status { Key = key, EffectId = 0 };
                UI_Statuses.Add(status);
            }
            return status;
        }
        public static string getLanguageOfPlayer(CSteamID playerID)
        {
           SteamPlayer steamPlayer = PlayerTool.getSteamPlayer(playerID);
           if(steamPlayer == null)
           {
                return "";
           }

            return steamPlayer.language;
        }
    }
}
