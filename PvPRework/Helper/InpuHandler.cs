using Rocket.Core.Logging;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeedMann.PvPRework.Helper
{
    public class InpuHandler
    {
        public delegate void PluginKeyPressed(UnturnedPlayer player, byte key);
        public static event PluginKeyPressed OnPluginKeyPressed;

        private static Dictionary<CSteamID, KeyInfo> KeyInfos = new Dictionary<CSteamID, KeyInfo>();
        private static int KeyHeldDelay = 6;

        internal static void OnPluginKeyDetected(Player player, uint simulation, byte key, bool state)
        {
            if (player == null || key < 0 || key > 4) return;
            UnturnedPlayer uPlayer = UnturnedPlayer.FromPlayer(player);


            if(!KeyInfos.TryGetValue(uPlayer.CSteamID, out KeyInfo keyInfo))
            {
                keyInfo = new KeyInfo();
                KeyInfos.Add(uPlayer.CSteamID, keyInfo);
            }

            if (state)
            {
                if (keyInfo.PluginKeys[key] > KeyHeldDelay)
                {
                    Logger.Log($"Plugin key held: {key}");
                }
                else
                {
                    keyInfo.PluginKeys[key]++;
                }
            }
            else if(keyInfo.PluginKeys[key] > 0)
            {
                if (keyInfo.PluginKeys[key] <= KeyHeldDelay)
                {
                    OnPluginKeyPressed?.Invoke(uPlayer, key);
                }
                keyInfo.PluginKeys[key] = 0;
            }
        }

        public static void removePlayerEntry(CSteamID steamId)
        {
            if (KeyInfos.ContainsKey(steamId))
            {
                KeyInfos.Remove(steamId);
            }
        }
    }
    class KeyInfo
    {
        internal List<int> PluginKeys = new List<int>();
        internal KeyInfo()
        {
            PluginKeys = new List<int>
            {
                0,
                0,
                0,
                0,
                0,
            };
        }
    }
}
