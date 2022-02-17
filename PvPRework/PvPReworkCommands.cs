using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;
using SpeedMann.PvPRework.Helper;
using SpeedMann.PvPRework.Models.Config;
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
                        UnturnedChat.Say(caller, "(2) /armorplus veststats", UnityEngine.Color.cyan);
                        return;
                    case "gunstats":
                        ItemWeaponAsset weapon;
                        PvPRework.Inst.getGunStats(player.Player, out weapon, out float penetration, out float fleshDamage, out float armorDamage, out Caliber caliber);
                        if(weapon != null)
                        {
                            UnturnedChat.Say(caller, $"The Stats of {weapon.name} [{weapon.id}] are\n {(caliber != null ? "Ammo: "+caliber.Name: "")} Penetration: {penetration}, FleshDamage: {fleshDamage}, ArmorDamage: {armorDamage}", UnityEngine.Color.cyan);
                            return;
                        }

                        UnturnedChat.Say(caller, "No Weapon Equiped!", UnityEngine.Color.red);
                        return;
                    case "veststats":

                        Asset asset = Assets.find(EAssetType.ITEM, player.Player.clothing.vest);
                        if(asset != null && asset is ItemVestAsset)
                        {
                            ItemVestAsset vest = (ItemVestAsset)asset;

                            float armor = ArmorLogic.calcItemArmor(player.Player, vest, out int armorClassIndex, out float armorTier, PvPRework.Conf.BetterArmor.Enabled && PvPRework.Conf.BetterArmor.UseArmorClasses, -1);

                            bool protectStomach = true;
                            bool protectArms = false;
                            float armorArms = vest.armor;
                            bool protectLegs = false;
                            float armorLegs = vest.armor;


                            if (PvPRework.Conf.BetterArmor.BetterHitZones.Enabled)
                            {
                                protectStomach = PvPRework.Conf.BetterArmor.BetterHitZones.VestsProtectStomach;
                            }
                            
                            if(PvPRework.Inst.vestExtensions.TryGetValue(vest.id, out VestExtension vestExtension))
                            {
                                protectStomach = vestExtension.ProtectStomach;
                                protectArms = vestExtension.ShoulderPlateLength > 0;
                                protectLegs = vestExtension.ThighPlateLength > 0;
                                armorArms = vestExtension.ArmorShoulderPlate > 0 ? vestExtension.ArmorShoulderPlate : armorArms;
                                armorLegs = vestExtension.ArmorThighPlate > 0 ? vestExtension.ArmorThighPlate : armorLegs;
                            }

                            UnturnedChat.Say(caller, $"The Stats of {vest.name} [{vest.id}] are\n Armor: {vest.armor}, Tier: {armorTier}, ProtectStomach: {protectStomach}", UnityEngine.Color.cyan);
                            return;
                        }

                        UnturnedChat.Say(caller, "No Vest Equiped!", UnityEngine.Color.red);
                        return;
                    default:
                        UnturnedChat.Say(caller, "Invalid Command parameters", UnityEngine.Color.red);
                        throw new WrongUsageOfCommandException(caller, this);
                }
            }
        }
    }
}

