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

        public delegate void StanceChanged(EPlayerStance oldStance, PlayerStance stance);
        public static event StanceChanged OnStanceChanged;
        internal void StanceChangeInvoker()
        {
            EPlayerStance prevStance = oldStance;
            oldStance = stance.stance;
            OnStanceChanged?.Invoke(prevStance, stance);
        }
        internal StanceHandler(PlayerStance stance)
        {
            this.stance = stance;
            oldStance = stance.stance;
        }
    }
}
