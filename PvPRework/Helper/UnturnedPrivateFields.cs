using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SpeedMann.PvPRework.Helper
{
    class UnturnedPrivateFields
    {
        private static FieldInfo Attachments;
        public static bool getGunAttachments(UseableGun gun, out Attachments result)
        {
            if (Attachments != null)
            {
                result = (Attachments)Attachments.GetValue(gun);
                return true;
            }
            result = null;
            return false;
        }

        public static void Init()
        {
            Type type;

            type = typeof(UseableGun);
            Attachments = type.GetField("thirdAttachments", BindingFlags.NonPublic | BindingFlags.Instance);
        }
    }
}
