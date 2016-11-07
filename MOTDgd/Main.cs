using Quobject.SocketIoClientDotNet.Client;
using Rocket.Core.Plugins;
using Rocket.API;
using Rocket.Core.Logging;
using System.IO;
using System;
using Rocket.Unturned;
using Rocket.Unturned.Player;
using Rocket.Unturned.Chat;
using System.Net;
using System.Xml;
using System.Web;
using Steamworks;
using System.Collections.Generic;
using System.Timers;
using Rocket.API.Collections;
using UnityEngine;
using SDG.Unturned;
using Rocket.Core;
using Newtonsoft.Json;

namespace MOTDgd
{
    //Přidat podporu pro Uconomy a translation listy
    public class Main : RocketPlugin<MOTDgdConfiguration>
    {
        //Setting up variables
        public static int Server_ID;
        public static bool Connected;
        public static Dictionary<CSteamID, int> Ad_Views = new Dictionary<CSteamID,int>();
        public static Dictionary<CSteamID, long> Cooldown = new Dictionary<CSteamID, long>();
        public static Dictionary<string, int> Reward_dictionary = new Dictionary<string, int>();
        public static Dictionary<CSteamID, int> Sequence = new Dictionary<CSteamID, int>();
        public static Dictionary<CSteamID, int> Awaiting_command = new Dictionary<CSteamID, int>();
        public static List<CSteamID> Request_players = new List<CSteamID>();
        public static int ads_before_cooldown;
        public static int reminder_delay;
        public static int cooldown_delay;
        public static bool global_messages;
        public static bool Ad_on_join;
        public static bool reapply_join;
        public static bool vid_unavailable;
        public static bool advanced_logging;
        private static string mod_name = "MOTDgdCommandAd for Unturned";
        private static string P_version = "2.0.0";
        private Timer cooldownTimer;
        private Timer reminderTimer;
        public static string User_ID;
        public static Main Instance;
        public static Socket socket;
        public static CSteamID Executor_ID = (CSteamID)0;

        protected override void Load()
        {
            Rocket.Core.Logging.Logger.Log("Loading " + mod_name + " version " + P_version);
            Instance = this;
            if (!parseConfig()) { return; };
            //Creating socket connection
            socket = IO.Socket("http://mcnode.motdgd.com:8080");
            Rocket.Core.Logging.Logger.Log("Connecting to HUB");

            //Logging in to node
            socket.On("connect", () =>
            {
                Rocket.Core.Logging.Logger.Log("Connected to HUB");
                socket.Emit("login", new object[] { Configuration.Instance.User_ID, SDG.Unturned.Provider.ip, SDG.Unturned.Provider.port, SDG.Unturned.Provider.APP_VERSION, mod_name, P_version, "unturned" });
                Connected = true;
            });

            //Reading Server ID
            socket.On("login_response", (arguments) =>
            {
                string login_data = arguments + "";
                int.TryParse(login_data, out Server_ID);
                Rocket.Core.Logging.Logger.Log("Received ID " + Server_ID + " from the HUB");
            });

            //Getting names of people that completed Advertisement
            socket.On("complete_response", (arguments) =>
            {
                string resp_data = arguments + "";
                UnturnedPlayer currentPlayer = getPlayer(resp_data);
                if (currentPlayer != null)
                {
                    if (Request_players.Contains(currentPlayer.CSteamID))
                    {
                        Request_players.Remove(currentPlayer.CSteamID);
                        if (advanced_logging == true)
                        {
                            if (!OnCooldown(currentPlayer))
                            {
                                Rocket.Core.Logging.Logger.Log("User " + currentPlayer.DisplayName + " completed advertisement.");
                            }
                            else
                            {
                                Rocket.Core.Logging.Logger.Log("User " + currentPlayer.DisplayName + " completed advertisement, but is on cooldown");
                            }
                        }

                        if (!OnCooldown(currentPlayer))
                        {
                            GiveReward(currentPlayer);
                        }
                        else
                        {
                            Dictionary<string, Color> translation = getTranslation("COOLDOWN");
                            foreach (var translation_pair in translation)
                            {
                                UnturnedChat.Say((IRocketPlayer)currentPlayer, translation_pair.Key, translation_pair.Value);
                            }
                        }
                    }
                }
                else
                {
                    Rocket.Core.Logging.Logger.LogWarning("Player with CSteamID " + resp_data + " completed advertisement but is not on the server.");
                }
            });

            socket.On("link_response", (args) =>
            {
                Dictionary<string, string> Response = JsonConvert.DeserializeObject<Dictionary<string, string>>(args + "");
                string pid = Response["pid"];
                string link = Response["url"];
                string message = Response["msg"];

                UnturnedPlayer player = getPlayer(pid);
                if (player != null)
                {
                    if (Awaiting_command.ContainsKey(player.CSteamID))
                    {
                        Awaiting_command.Remove(player.CSteamID);
                    }

                    Dictionary<string, Color> translation = getTranslation("LINK_RESPONSE");
                    var translated = "Here's your link";
                    foreach (var translation_pair in translation)
                    {
                        translated = translation_pair.Key;
                    }
                    player.Player.sendBrowserRequest(translated, link);
                }
                else
                {
                    Rocket.Core.Logging.Logger.LogError("Player with CSteamID " + pid + " requested link, but is not on the server");
                }
                
            });

            //Disconnecting from node
            socket.On("disconnect", () =>
            {
                Rocket.Core.Logging.Logger.LogWarning("Disconnected");
                Server_ID = 0;
                Connected = false;
            });

            socket.On("error", (arguments) =>
            {
                Rocket.Core.Logging.Logger.LogError("There was an error with node: " + arguments);
                Server_ID = 0;
                Connected = false;
            });

            socket.On("aderror_response", (arguments) =>
            {
                UnturnedPlayer player = getPlayer(arguments + "");
                if (vid_unavailable)
                {
                    if (player != null)
                    {
                        if (Request_players.Contains(player.CSteamID))
                        {
                            Request_players.Remove(player.CSteamID);
                            if (advanced_logging == true)
                            {
                                if (!OnCooldown(player))
                                {
                                    Rocket.Core.Logging.Logger.Log("User " + player.DisplayName + " completed advertisement.");
                                }
                                else
                                {
                                    Rocket.Core.Logging.Logger.Log("User " + player.DisplayName + " completed advertisement, but is on cooldown");
                                }
                            }

                            if (!OnCooldown(player))
                            {
                                GiveReward(player);
                            }
                            else
                            {
                                Dictionary<string, Color> translation = getTranslation("COOLDOWN");
                                foreach (var translation_pair in translation)
                                {
                                    UnturnedChat.Say((IRocketPlayer)player, translation_pair.Key, translation_pair.Value);
                                }
                            }
                        }
                    }
                    else
                    {
                        Rocket.Core.Logging.Logger.LogWarning("Player with CSteamID " + arguments + " completed advertisement but is not on the server.");
                    }
                }
                else
                {
                    Dictionary<string, Color> translation = getTranslation("COMPLETED_WITHOUT_VIDEO");
                    var translated = "Unfortunately we didn't have any video for you, so you can't receive your reward.";
                    var color = Color.green;
                    foreach (var translation_pair in translation)
                    {
                        translated = translation_pair.Key;
                        color = translation_pair.Value;
                    }
                    UnturnedChat.Say(player, translated, color);                    
                }
            });

            //Telling player about rewards
            U.Events.OnPlayerConnected += Connect_event;
            U.Events.OnPlayerDisconnected += Disconnect_event;


            //Timer checking Cooldown players
            cooldownTimer = new System.Timers.Timer();
            cooldownTimer.Elapsed += new ElapsedEventHandler(timerFunc);
            cooldownTimer.Interval = 2000;
            cooldownTimer.Enabled = true;

            if (reminder_delay != 0)
            {
                reminderTimer = new System.Timers.Timer();
                reminderTimer.Elapsed += new ElapsedEventHandler(reminderFunc);
                reminderTimer.Interval = reminder_delay * 60 * 1000;
                reminderTimer.Enabled = true;
            }
        }

        private void Disconnect_event(UnturnedPlayer player)
        {
            Ad_Views.Remove(player.CSteamID);
            Sequence.Remove(player.CSteamID);
            Awaiting_command.Remove(player.CSteamID);
            Sequence.Remove(player.CSteamID);
        }

        private void Connect_event(UnturnedPlayer player)
        {
            Ad_Views.Add(player.CSteamID, 0);
            Sequence.Add(player.CSteamID, 0);

            if (Configuration.Instance.Join_Commands.Count != 0)
            {
                foreach (string command in Configuration.Instance.Join_Commands)
                {
                    bool success = R.Commands.Execute((IRocketPlayer)UnturnedPlayer.FromCSteamID(Executor_ID), command.Replace("(player)", player.DisplayName.Split(' ')[0]).Replace("(steamid)", player.CSteamID + ""));
                    if (!success)
                    {
                        Rocket.Core.Logging.Logger.LogError("Failed to execute command " + command.Replace("(player)", player.DisplayName.Split(' ')[0]).Replace("(steamid)", player.CSteamID + "") + " while trying to give reward to " + player.DisplayName);
                    }
                }
            };

            if (Connected && !OnCooldown(player) && Ad_on_join)
            {
                request_link(player);
            }
        }

        protected override void Unload()
        {
            if (reminderTimer != null)
            {
                reminderTimer.Enabled = false;
            }
            if (cooldownTimer != null)
            {
                cooldownTimer.Enabled = false;
            }
            U.Events.OnPlayerConnected -= Connect_event;
            if (socket != null)
            {
                socket.Disconnect();
            }
            Server_ID = 0;
            Connected = false;
            Ad_Views.Clear();
            Reward_dictionary.Clear();
            Sequence.Clear();
            Cooldown.Clear();
        }


        //Get player variable from received CSteamID
        public UnturnedPlayer getPlayer(string id)
        {
            CSteamID new_ID = (CSteamID)UInt64.Parse(id);
            UnturnedPlayer player = UnturnedPlayer.FromCSteamID(new_ID);
            return player;
        }

        //Give Reward
        public void GiveReward(UnturnedPlayer player)
        {
            switch (Configuration.Instance.Reward_mode.ToLower().Replace(" ", ""))
            {
                case "all":
                    GiveReward_All(player);
                    break;
                case "sequential":
                    GiveReward_Sequential(player);
                    break;
                case "weighted":
                    GiveReward_Weighted(player);
                    break;
                case "random":
                    GiveReward_Random(player);
                    break;
                default:
                    Rocket.Core.Logging.Logger.LogError("Couldn't determine reward mode. Check your config!");
                    break;
            }
        }

        public void GiveReward_All (UnturnedPlayer player)
        {
            foreach (var pair in Reward_dictionary)
            {
                bool success = R.Commands.Execute((IRocketPlayer)UnturnedPlayer.FromCSteamID(Executor_ID), pair.Key.Replace("(player)", player.DisplayName.Split(' ')[0]).Replace("(steamid)", player.CSteamID + ""));
                if (!success)
                {
                    Rocket.Core.Logging.Logger.LogError("Failed to execute command " + pair.Key + " while trying to give reward to " + player.DisplayName);
                }
            }
            
            Check_Cooldown(player);
        }

        public void GiveReward_Sequential(UnturnedPlayer player)
        {
            List<string> Items = new List<string>();
            foreach (var pair in Reward_dictionary)
            {
                for (int i = 0; i < pair.Value; i++)
                {
                    Items.Add(pair.Key);
                }
            }

            int sequence_number = 0;

            foreach (var pair in Sequence)
            {
                if (pair.Key == player.CSteamID)
                {
                    CSteamID user = pair.Key;
                    sequence_number = pair.Value;
                }
            }

            bool success = R.Commands.Execute((IRocketPlayer)UnturnedPlayer.FromCSteamID(Executor_ID), Items[sequence_number].Replace("(player)", player.DisplayName.Split(' ')[0]).Replace("(steamid)", player.CSteamID + ""));
            if (!success)
            {
                Rocket.Core.Logging.Logger.LogError("Failed to execute command " + Items[sequence_number].Replace("(player)", player.DisplayName.Split(' ')[0]).Replace("(steamid)", player.CSteamID + "") + " while trying to give reward to " + player.DisplayName);
            }

            if (sequence_number >= Reward_dictionary.Keys.Count - 1)
            {
                Sequence.Remove(player.CSteamID);
                Sequence.Add(player.CSteamID, 0);
            }
            else
            {
                Sequence.Remove(player.CSteamID);
                Sequence.Add(player.CSteamID, sequence_number + 1);
            }

            Check_Cooldown(player);
        }

        public void GiveReward_Weighted(UnturnedPlayer player)
        {
            List<string> Rnd_Items = new List<string>();
            foreach (var pair in Reward_dictionary)
            {
                for (int i = 0; i < pair.Value; i++)
                {
                    Rnd_Items.Add(pair.Key);
                }
            }

            System.Random rnd = new System.Random();
            int r = rnd.Next(Rnd_Items.Count);

            bool success = R.Commands.Execute((IRocketPlayer)UnturnedPlayer.FromCSteamID(Executor_ID), Rnd_Items[r].Replace("(player)", player.DisplayName.Split(' ')[0]).Replace("(steamid)", player.CSteamID + ""));
            if (!success)
            {
                Rocket.Core.Logging.Logger.LogError("Failed to execute command " + Rnd_Items[r] + " while trying to give reward to " + player.DisplayName);
            }

            Check_Cooldown(player);
        }

        public void GiveReward_Random(UnturnedPlayer player)
        {
            List<string> Rnd_Items = new List<string>();
            foreach (var pair in Reward_dictionary)
            {
                Rnd_Items.Add(pair.Key);
            }

            System.Random rnd = new System.Random();
            int r = rnd.Next(Rnd_Items.Count);

            bool success = R.Commands.Execute((IRocketPlayer)UnturnedPlayer.FromCSteamID(Executor_ID), Rnd_Items[r].Replace("(player)", player.DisplayName.Split(' ')[0]).Replace("(steamid)", player.CSteamID + ""));
            if (!success)
            {
                Rocket.Core.Logging.Logger.LogError("Failed to execute command " + Rnd_Items[r] + " while trying to give reward to " + player.DisplayName);
            }

            Check_Cooldown(player);
        }

        public void Check_Cooldown(UnturnedPlayer player)
        {
            int done_ads;
            Ad_Views.TryGetValue(player.CSteamID, out done_ads);

            if (global_messages == false)
            {
                if (ads_before_cooldown == 1 || ads_before_cooldown - done_ads == 1)
                {
                    Dictionary<string, Color> translation = getTranslation("EVENT_RECEIVED_REWARD_COOLDOWN", Configuration.Instance.CooldownTime);
                    foreach (var translation_pair in translation)
                    {
                        UnturnedChat.Say((IRocketPlayer)player, translation_pair.Key, translation_pair.Value);
                    }

                    if (Configuration.Instance.CooldownTime != 0) 
                    {
                        var CooldownTime = CurrentTime.Millis + (Configuration.Instance.CooldownTime * 60 * 1000);
                        Cooldown.Add(player.CSteamID, CooldownTime);
                    }
                }
                else
                {
                    int remaining_ads = ads_before_cooldown - done_ads;
                    Dictionary<string, Color> translation = getTranslation("EVENT_RECEIVED_REWARD_ADS_REMAIN", remaining_ads);
                    foreach (var translation_pair in translation)
                    {
                        UnturnedChat.Say((IRocketPlayer)player, translation_pair.Key, translation_pair.Value);
                    }

                    if (Configuration.Instance.CooldownTime != 0)
                    {
                        Ad_Views.Remove(player.CSteamID);
                        Ad_Views.Add(player.CSteamID, done_ads + 1);
                    }
                }
            }
            else
            {
                if (ads_before_cooldown == 1 || ads_before_cooldown - done_ads == 1)
                {
                    Dictionary<string, Color> translation = getTranslation("EVENT_RECEIVED_REWARD_ADS_GLOBAL", player.DisplayName);
                    foreach (var translation_pair in translation)
                    {
                        UnturnedChat.Say(translation_pair.Key, translation_pair.Value);
                    }

                    if (Configuration.Instance.CooldownTime != 0)
                    {
                        var CooldownTime = CurrentTime.Millis + (Configuration.Instance.CooldownTime * 60 * 1000);
                        Cooldown.Add(player.CSteamID, CooldownTime);
                    }
                }
                else
                {
                    int remaining_ads = ads_before_cooldown - done_ads;
                    Dictionary<string, Color> translation = getTranslation("EVENT_RECEIVED_REWARD_ADS_GLOBAL", player.DisplayName);
                    foreach (var translation_pair in translation)
                    {
                        UnturnedChat.Say(translation_pair.Key, translation_pair.Value);
                    }

                    if (Configuration.Instance.CooldownTime != 0)
                    {
                        Ad_Views.Remove(player.CSteamID);
                        Ad_Views.Add(player.CSteamID, done_ads + 1);
                    }
                }
            }
        }

        private void timerFunc(object sender, EventArgs e)
        {
            RemoveCooldownLoop();
            CheckRewardAvailability();
        }

        private void reminderFunc(object sender, EventArgs e)
        {
            if (Configuration.Instance.Join_Commands.Count != 0 && reapply_join)
            {
                foreach (SteamPlayer steamplayer in Provider.clients)
                {
                    UnturnedPlayer player = UnturnedPlayer.FromSteamPlayer(steamplayer);
                    if (!OnCooldown(player))
                    {
                        foreach (string command in Configuration.Instance.Join_Commands)
                        {
                            bool success = R.Commands.Execute((IRocketPlayer)UnturnedPlayer.FromCSteamID(Executor_ID), command.Replace("(player)", player.DisplayName.Split(' ')[0]).Replace("(steamid)", player.CSteamID + ""));
                            if (!success)
                            {
                                Rocket.Core.Logging.Logger.LogError("Failed to execute command " + command.Replace("(player)", player.DisplayName.Split(' ')[0]).Replace("(steamid)", player.CSteamID + "") + " while trying to give reward to " + player.DisplayName);
                            }
                        }
                        Dictionary<string, Color> translation = getTranslation("REMINDER_MESSAGE_JOIN");
                        foreach (var translation_pair in translation)
                        {
                            UnturnedChat.Say(player, translation_pair.Key, translation_pair.Value);
                        }
                    }
                }
            }
            else
            {
                foreach (SteamPlayer steam_player in Provider.clients)
                {
                    UnturnedPlayer player = UnturnedPlayer.FromSteamPlayer(steam_player);
                    if (!OnCooldown(player))
                    {
                        Dictionary<string, Color> translation = getTranslation("REMINDER_MESSAGE");
                        foreach (var translation_pair in translation)
                        {
                            UnturnedChat.Say(player, translation_pair.Key, translation_pair.Value);
                        }
                    }
                }
            }
        }

        //Loop checking cooldown list and removing players after cooldown expiry 
        public void RemoveCooldownLoop()
        {
            foreach (var pair in Cooldown)
            {
                var key = pair.Key;
                var value = pair.Value;
                var currentTime = CurrentTime.Millis;

                if (value <= currentTime)
                {
                    Cooldown.Remove(key);
                    Ad_Views.Remove(key);
                    Ad_Views.Add(key, 0); 
                    UnturnedPlayer player = UnturnedPlayer.FromCSteamID(key);
                    Dictionary<string, Color> translation = getTranslation("COOLDOWN_EXPIRED");
                    foreach (var translation_pair in translation) {
                        UnturnedChat.Say((IRocketPlayer)player, translation_pair.Key, translation_pair.Value);
                    }
                }
            }
        }

        //Find if in Cooldown
        public static bool OnCooldown(UnturnedPlayer player)
        {
            if (Cooldown.ContainsKey(player.CSteamID))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        //Return time in Millis since 1.1.1970
        static class CurrentTime
        {
            private static readonly DateTime Jan1St1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            public static long Millis { get { return (long)((DateTime.UtcNow - Jan1St1970).TotalMilliseconds); } }
        }

        //Return cooldown time
        public static string CooldownTime(UnturnedPlayer player)
        {
            foreach (var pair in Cooldown)
            {
                var key = pair.Key;
                var value = pair.Value;
                var currentTime = CurrentTime.Millis;

                if (key == player.CSteamID)
                {
                    var milTime = value - currentTime;
                    double time = milTime / 1000;
                    
                    var minutes = Math.Truncate(time / 60);
                    var seconds = time - (minutes * 60);
                    
                    return minutes + " minutes and " + seconds + " seconds";
                };
            }
            return "";
        }

        public static void request_link(UnturnedPlayer player) {
            socket.Emit("link", new object[] { player.CSteamID + "", GetIP(player) });
            if (!Awaiting_command.ContainsKey(player.CSteamID))
            {
                Awaiting_command.Add(player.CSteamID, 0);
            }
            Request_players.Add(player.CSteamID);
            Dictionary<string, Color> translation = getTranslation("REQUEST_LINK_MESSAGE");
            foreach (var translation_pair in translation)
            {
                UnturnedChat.Say(player, translation_pair.Key, translation_pair.Value);
            }
        }

        private bool parseConfig()
        {
            /*
             * Parsing rewards to dictionary
             */
            for (int lastIndex = 0; lastIndex < Configuration.Instance.Rewards.Length; lastIndex++)
            {
                MOTDgd.MOTDgdConfiguration.Reward reward = Configuration.Instance.Rewards[lastIndex];
                string command = reward.Command;
                int probability = reward.Probability;

                Reward_dictionary.Add(command, probability);
            }

            /* 
             * Custom executor CSteamID 
             */
            UInt64 int_result;
            if (Configuration.Instance.Executor_CSteamID != null && UInt64.TryParse(Configuration.Instance.Executor_CSteamID, out int_result))
            {
                Executor_ID = (CSteamID)int_result;
            }

            /*
             *  Checking MOTD ID
             */

            if (Configuration.Instance.User_ID == 0)
            {
                Rocket.Core.Logging.Logger.LogError("MOTD ID not set! Unloading plugin now!");
                this.Unload();
                this.UnloadPlugin(PluginState.Failure);
                return false;
            }

            /*
             * Checking reward mode
             */

            if (Configuration.Instance.Reward_mode.ToLower().Replace(" ", "") != "all" && Configuration.Instance.Reward_mode.ToLower().Replace(" ", "") != "weighted" && Configuration.Instance.Reward_mode.ToLower().Replace(" ", "") != "random" && Configuration.Instance.Reward_mode.ToLower().Replace(" ", "") != "sequential")
            {
                Rocket.Core.Logging.Logger.LogError("Reward mode is not set correctly! Unloading plugin now!");
                this.Unload();
                this.UnloadPlugin(PluginState.Failure);
                return false;
            }

            /*
             * Setting up rewards before cooldown
             */

            if (Configuration.Instance.Number_of_ads_before_cooldown == 0)
            {
                ads_before_cooldown = 1;
            }
            else
            {
                ads_before_cooldown = Configuration.Instance.Number_of_ads_before_cooldown;
            }

            /*
             * Setting up reminder delay
             */

            reminder_delay = Configuration.Instance.Reminder_delay;

            /*
             * Setting up global messages 
             */

            

            global_messages = Configuration.Instance.Global_messages;

            /*
             * Setting up ad on join
             */

            
            Ad_on_join = Configuration.Instance.Ad_on_join;

            /*
             * Setting up reapply join command
             */

            reapply_join = Configuration.Instance.Reapply_join_command;

            /*
             * Setting up reward on not watch
             */

            vid_unavailable = Configuration.Instance.Give_reward_when_video_unavailable;

            /*
             * Setting up advanced logging
             */

            advanced_logging = Configuration.Instance.AdvancedLogging;
            return true;
        }
        
        public static Dictionary<string, Color> getTranslation(string identifier, params object[] parameters) {
            string color = "";
            string result = "";
            Color out_color;
            Dictionary<string, Color> args = new Dictionary<string, Color>();

            for (int lastIndex = 0; lastIndex < Main.Instance.Configuration.Instance.Translations.Length; lastIndex++)
            {
                MOTDgd.MOTDgdConfiguration.Translation translation_object = Main.Instance.Configuration.Instance.Translations[lastIndex];
                if (translation_object.Identifier.ToLower() == identifier.ToLower())
                {
                    color = translation_object.Color;
                    result = translation_object.Text;
                    lastIndex = Main.Instance.Configuration.Instance.Translations.Length + 10;
                }
            }

            if (string.IsNullOrEmpty(result))
            {
                args.Add(identifier, Color.green);
                return args;
            }

            if (result.Contains("{") && result.Contains("}") && parameters != null && (int)parameters.Length != 0)
            {
                for (int i = 0; i < (int)parameters.Length; i++)
                {
                    if (parameters[i] == null)
                    {
                        parameters[i] = "NULL";
                    }
                }
                result = string.Format(result, parameters);
            }

            out_color = UnturnedChat.GetColorFromName(color, Color.green);
            
            args.Add(result, out_color);
            return args;
        }

        private static string GetIP(UnturnedPlayer player)
        {
            uint mNRemoteIP;
            P2PSessionState_t p2PSessionStateT;
            CSteamID pid = player.CSteamID;

            if (!SteamGameServerNetworking.GetP2PSessionState(pid, out p2PSessionStateT))
            {
                mNRemoteIP = 0;
            }
            else
            {
                mNRemoteIP = p2PSessionStateT.m_nRemoteIP;
            }

            return Parser.getIPFromUInt32(mNRemoteIP);
        }

        private static void CheckRewardAvailability() {
            foreach (var pair in Awaiting_command) {
                int val = pair.Value;
                CSteamID key = pair.Key;
                if (val >= 30)
                {
                    UnturnedPlayer plr = UnturnedPlayer.FromCSteamID(key);
                    UnturnedChat.Say(plr, "We are sorry, but we couldn't get add for you. Contact server owner for more information.");
                    Request_players.Remove(key);

                    Awaiting_command.Remove(key);
                }
                else
                {
                    Awaiting_command.Remove(key);
                    Awaiting_command.Add(key, val + 2);
                }
            }
        }
    }
}
