using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SpeedMann.PvPRework.UI.HealthUIHandler;

namespace SpeedMann.PvPRework.Models.UI
{
    internal class HeathUIState
    {
        internal Dictionary<BodyPart, DamageColor> damageColors = new Dictionary<BodyPart, DamageColor>();

        internal HeathUIState()
        {
            foreach(BodyPart bodyPart in BodyPart.GetValues(typeof(BodyPart)))
            {
                damageColors.Add(bodyPart, DamageColor.Green);
            }
       
        }
    }
}
