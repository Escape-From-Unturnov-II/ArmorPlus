using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeedMann.PvPRework
{
    public class CommandKit : IRocketCommand
    {
        public string Help
        {
            get { return "ArmorPlus Commands"; }
        }

        public string Name
        {
            get { return "armorplus"; }
        }

        public string Syntax
        {
            get { return "<armorplus>"; }
        }

        public List<string> Aliases
        {
            get { return new List<string>(); }
        }

        public AllowedCaller AllowedCaller
        {
            get { return AllowedCaller.Player; }
        }

        public List<string> Permissions
        {
            get
            {
                return new List<string>() { "ArmorPlus" };
            }
        }

        public void Execute(IRocketPlayer caller, string[] command)
        {
            UnturnedPlayer player = (UnturnedPlayer)caller;
            if (command.Length < 1)
            {
                UnturnedChat.Say(caller, "Invalid! Try /armorplus help", UnityEngine.Color.red);
                return;
            }else if (!PvPRework.ModsLoaded)
            {
                UnturnedChat.Say(caller, "ArmorPlus is still loaded!", UnityEngine.Color.red);
            }
            else
            {
                switch (command[0].ToLower())
                {
                    case "help":
                        UnturnedChat.Say(caller, "These are all commands of the ArmorPlus-Plugin", UnityEngine.Color.cyan);
                        UnturnedChat.Say(caller, "[] indicate optional Parameters <> are essential", UnityEngine.Color.cyan);
                        UnturnedChat.Say(caller, "(1) /armorplus help", UnityEngine.Color.cyan);
                        UnturnedChat.Say(caller, "(2) /armorplus gunstats", UnityEngine.Color.cyan);
                        return;
                    #region commandStart
                    case "gunstats":
                        ItemWeaponAsset weapon;
                        PvPRework.Inst.getGunStats(player.Player, out weapon, out float penetration, out float fleshDamage, out float armorDamage);
                        if(weapon != null)
                        {
                            UnturnedChat.Say(caller, $"The Stats of {weapon.name} are Penetration: {penetration}, FleshDamage: {fleshDamage}, ArmorDamage: {armorDamage}", UnityEngine.Color.red);
                            return;
                        }

                        UnturnedChat.Say(caller, "No Weapon Equiped!", UnityEngine.Color.red);
                        return;
                    #endregion
                    default:
                        UnturnedChat.Say(caller, "Invalid Command parameters", UnityEngine.Color.red);
                        throw new WrongUsageOfCommandException(caller, this);
                }
            }
        }
    }
}

