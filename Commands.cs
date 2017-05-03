using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using Steamworks;
using System.Collections.Generic;
using UnityEngine;


namespace MOTDgd
{
    class CommandAd : IRocketCommand
    {
        public AllowedCaller AllowedCaller
        {
            get { return AllowedCaller.Player; }
        }

        public string Name
        {
            get
            {
                return "ad";
            }
        }

        public string Help
        {
            get
            {
                return "Generates link to advertisement page. After completion gives player reward.";
            }
        }

        public List<string> Aliases
        {
            get
            {
                return new List<string>() {};
            }
        }

        public string Syntax
        {
            get
            {
                return "";
            }
        }

        public List<string> Permissions
        {
            get
            {
                return new List<string>() { "ad" };
            }
        }

        public void Execute(IRocketPlayer caller, string[] command)
        {
            UnturnedPlayer player = (UnturnedPlayer)caller;
            if (player == null)
            {
                Rocket.Core.Logging.Logger.Log("This command cannot be called from the console.");
                return;
            }

            if (!Main.OnCooldown(player) && Main.Connected == true)
            {
                Main.request_link(player);
            }
            else if (Main.OnCooldown(player))
            {
                UnturnedChat.Say((IRocketPlayer)player, Main.Translation_dictionary["COOLDOWN"].Key, Main.Translation_dictionary["COOLDOWN"].Value);
            }
            else if (Main.Connected == false)
            {
                UnturnedChat.Say(player, "There was error while connecting to HUB. Try again later.");
            }
            else
            {
                UnturnedChat.Say(player, "Error while processing your request.");
            }
        }
    }

    class CommandCooldown : IRocketCommand
    {
        public AllowedCaller AllowedCaller
        {
            get { return AllowedCaller.Player; }
        }

        public string Name
        {
            get
            {
                return "cooldown";
            }
        }

        public string Help
        {
            get
            {
                return "Tells player how much time is left before cooldown expiry.";
            }
        }

        public List<string> Aliases
        {
            get
            {
                return new List<string>() {};
            }
        }

        public string Syntax
        {
            get
            {
                return "";
            }
        }

        public List<string> Permissions
        {
            get
            {
                return new List<string>() { "cooldown" };
            }
        }

        public void Execute(IRocketPlayer caller, string[] command)
        {
            UnturnedPlayer player = (UnturnedPlayer)caller;
            if (player == null)
            {
                Rocket.Core.Logging.Logger.Log("This command cannot be called from the console.");
                return;
            }

            var data = Main.CooldownTime(player);
            if (data != "")
            {
                UnturnedChat.Say((IRocketPlayer)player, Main.setTranslationParams(Main.Translation_dictionary["COOLDOWN_REMAINING"].Key, data), Main.Translation_dictionary["COOLDOWN_REMAINING"].Value);
            }
            else
            {
                UnturnedChat.Say((IRocketPlayer)player, Main.Translation_dictionary["NOT_ON_COOLDOWN"].Key, Main.Translation_dictionary["NOT_ON_COOLDOWN"].Value);
            }
        }
    }

    class CommandClearCooldownAll : IRocketCommand
    {
        public AllowedCaller AllowedCaller
        {
            get { return AllowedCaller.Both; }
        }

        public string Name
        {
            get
            {
                return "clearallcooldown";
            }
        }

        public string Help
        {
            get
            {
                return "Clears all cooldowns on the server.";
            }
        }

        public List<string> Aliases
        {
            get
            {
                return new List<string>() { };
            }
        }

        public string Syntax
        {
            get
            {
                return "";
            }
        }

        public List<string> Permissions
        {
            get
            {
                return new List<string>() { "clearall" };
            }
        }

        public void Execute(IRocketPlayer caller, string[] command)
        {
            Main.Cooldown.Clear();
            Main.Ad_Views.Clear();
            Rocket.Core.Logging.Logger.Log("Cooldown list cleared.");
        }
    }

    //Fix 
    class CommandClearCooldownPlayer : IRocketCommand
    {
        public AllowedCaller AllowedCaller
        {
            get { return AllowedCaller.Both; }
        }

        public string Name
        {
            get
            {
                return "clearcooldown";
            }
        }

        public string Help
        {
            get
            {
                return "Clears cooldown for player.";
            }
        }

        public List<string> Aliases
        {
            get
            {
                return new List<string>() { };
            }
        }

        public string Syntax
        {
            get
            {
                return "<player>";
            }
        }

        public List<string> Permissions
        {
            get
            {
                return new List<string>() { "clear" };
            }
        }

        public void Execute(IRocketPlayer caller, string[] command)
        {
            if (command.Length == 1)
            {
                UnturnedPlayer remPlayer = UnturnedPlayer.FromName(command[0]);
                if (remPlayer != null)
                {
                    Main.Cooldown.Remove(remPlayer.CSteamID);
                    Main.Ad_Views.Remove(remPlayer.CSteamID);
                    Rocket.Core.Logging.Logger.Log("Cleared cooldown for player " + remPlayer.DisplayName);
                }
                else
                {
                    Rocket.Core.Logging.Logger.LogWarning("Tried to clear cooldown for " + command[0] + " but he is not on the server!");
                }
            }
            else
            {
                if(caller.ToString() == "Rocket.API.ConsolePlayer")
                {
                    Rocket.Core.Logging.Logger.Log("Wrong syntax of command");
                }
                else
                {
                    UnturnedPlayer player = (UnturnedPlayer)caller;
                    UnturnedChat.Say(player, "Wrong syntax of command");
                }
            }
        }
    }


    class CommandSetRewardMode : IRocketCommand
    {
        public AllowedCaller AllowedCaller
        {
            get { return AllowedCaller.Both; }
        }

        public string Name
        {
            get
            {
                return "setmode";
            }
        }

        public string Help
        {
            get
            {
                return "Set the reward mode until server restart, (to make a permanent change you need to alter the physical config file).";
            }
        }

        public List<string> Aliases
        {
            get
            {
                return new List<string>() { };
            }
        }

        public string Syntax
        {
            get
            {
                return "<ALL | SEQUENTIAL | RANDOM | WEIGHTED>";
            }
        }

        public List<string> Permissions
        {
            get
            {
                return new List<string>() { "setmode" };
            }
        }

        public void Execute(IRocketPlayer caller, string[] command)
        {
            if (command.Length == 1)
            {
                switch (command[0].ToLower())
                {
                    case "all":
                        Main.reward_mode = "all";
                        break;
                    case "sequential":
                        Main.reward_mode = "sequential";
                        break;
                    case "weighted":
                        Main.reward_mode = "weighted";
                        break;
                    case "random":
                        Main.reward_mode = "random";
                        break;
                    default:
                        if (caller.ToString() == "Rocket.API.ConsolePlayer")
                        {
                            Rocket.Core.Logging.Logger.Log("Didn't recognize " + command[0] + " as valid reward mode.");
                        }
                        else
                        {
                            UnturnedPlayer player = (UnturnedPlayer)caller;
                            UnturnedChat.Say(player, "Didn't recognize " + command[0] + " as valid reward mode.");
                        }
                        break;
                }
            }
            else
            {
                if (caller.ToString() == "Rocket.API.ConsolePlayer")
                {
                    Rocket.Core.Logging.Logger.Log("Wrong syntax of command");
                }
                else
                {
                    UnturnedPlayer player = (UnturnedPlayer)caller;
                    UnturnedChat.Say(player, "Wrong syntax of command");
                }
            }
        }
    }

    class CommandGiveReward : IRocketCommand
    {
        public AllowedCaller AllowedCaller
        {
            get { return AllowedCaller.Both; }
        }

        public string Name
        {
            get
            {
                return "givereward";
            }
        }

        public string Help
        {
            get
            {
                return "Give the player a reward manually.";
            }
        }

        public List<string> Aliases
        {
            get
            {
                return new List<string>() { };
            }
        }

        public string Syntax
        {
            get
            {
                return "<player>";
            }
        }

        public List<string> Permissions
        {
            get
            {
                return new List<string>() { "givereward" };
            }
        }

        public void Execute(IRocketPlayer caller, string[] command)
        {
            if (command.Length == 1)
            {
                UnturnedPlayer rew_player = UnturnedPlayer.FromName(command[0]);
                if (rew_player != null)
                {
                    Main inst = new Main();
                    inst.GiveReward(rew_player);
                }
                else
                {
                    if (caller.ToString() == "Rocket.API.ConsolePlayer")
                    {
                        Rocket.Core.Logging.Logger.Log("Cannot find player with name " + command[0]);
                    }
                    else
                    {
                        UnturnedPlayer player = (UnturnedPlayer)caller;
                        UnturnedChat.Say(player, "Cannot find player with name " + command[0]);
                    }
                }
            }
            else
            {
                if (caller.ToString() == "Rocket.API.ConsolePlayer")
                {
                    Rocket.Core.Logging.Logger.Log("Wrong syntax of command");
                }
                else
                {
                    UnturnedPlayer player = (UnturnedPlayer)caller;
                    UnturnedChat.Say(player, "Wrong syntax of command");
                }
            }
        }
    }
}
