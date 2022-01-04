using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeedMann.PvPRework
{
    class PlayerHit
    {
        public DateTime timeStamp;
        public InputInfo imputInfo;
        public PlayerHit(InputInfo imputInfo)
        {
            timeStamp = DateTime.Now;
            this.imputInfo = imputInfo;
        }

        public bool Equals(PlayerHit other)
        {
            return timeStamp.Equals(other.timeStamp) && imputInfo.Equals(other.imputInfo);
        }
        public bool isOlderThan(TimeSpan timeSpan)
        {
            return timeSpan.CompareTo(DateTime.Now.Subtract(timeStamp)) < 0;
        }

        public bool isCorrectHit(CSteamID otherPlayerId, ELimb otherLimb)
        {
            CSteamID currentPalyerId = UnturnedPlayer.FromPlayer(imputInfo.player).CSteamID;
            return currentPalyerId.Equals(otherPlayerId) && imputInfo.limb.Equals(otherLimb);
        }
    }
}
