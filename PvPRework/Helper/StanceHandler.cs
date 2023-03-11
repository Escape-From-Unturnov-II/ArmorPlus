using Rocket.Core.Logging;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeedMann.PvPRework.Helper
{
    internal class StanceHandler
    {
        private PlayerStance stance;
        private EPlayerStance oldStance;

        public delegate void PreStanceChanged(EPlayerStance oldStance, PlayerStance stance, ref EPlayerStance newStance);
        public static event PreStanceChanged OnPreStanceChange;
        public delegate void StanceChanged(EPlayerStance newStance);
        public static event StanceChanged OnPostStanceChange;
        internal void StanceChangeInvoker()
        {
            EPlayerStance newStance = stance.stance;
            OnPreStanceChange?.Invoke(oldStance, stance, ref newStance);
            oldStance = newStance;
            OnPostStanceChange?.Invoke(newStance);
        }
        internal StanceHandler(Player player)
        {
            stance = player.stance;
            oldStance = stance.stance;
            player.stance.onStanceUpdated += StanceChangeInvoker;
        }
    }
}
