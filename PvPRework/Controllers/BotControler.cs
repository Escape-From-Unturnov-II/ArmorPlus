using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SpeedMann.PvPRework.Controllers
{
    internal class BotControler
    {
        internal static void createBot(string botName)
        {
            SteamPlayerID playerID = new SteamPlayerID(new CSteamID(10), 0, botName, botName, botName, CSteamID.Nil);
            SteamPending steamPending = new SteamPending(null, playerID, false, 0, 0, 0, Color.gray, Color.black, Color.blue, false, 0, 0, 0, 0, 0, 0, 0, new ulong[0], EPlayerSkillset.POLICE, "English", CSteamID.Nil, EClientPlatform.Windows);
            Provider.pending.Add(steamPending);
            Provider.accept(steamPending);
        }
    }
}
