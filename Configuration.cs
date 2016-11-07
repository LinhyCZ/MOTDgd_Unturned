using Rocket.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace MOTDgd
{
    public class MOTDgdConfiguration : IRocketPluginConfiguration
    {
        public int User_ID; 
        public int Reminder_delay; 
        public int Number_of_ads_before_cooldown; 
        public string Reward_mode; 
        public int CooldownTime; 
        public bool Global_messages; 
        public bool Ad_on_join; 
        public bool Reapply_join_command;
        public bool Give_reward_when_video_unavailable;
        public bool AdvancedLogging; 
        public string Executor_CSteamID;
        public List<string> Join_Commands;

        public sealed class Reward
        {
            [XmlAttribute("Command")]
            public string Command;

            [XmlAttribute("Probability")]
            public int Probability;

            public Reward(string command, int probability)
            {
                Command = command;
                Probability = probability;
            }

            public Reward()
            {
                Command = "";
                Probability = 1;
            }
        }

        public sealed class Translation
        {
            [XmlAttribute("Identifier")]
            public string Identifier;

            [XmlAttribute("Text")]
            public string Text;

            [XmlAttribute("Color")]
            public string Color;

            public Translation(string identifier, string text, string color)
            {
                Identifier = identifier;
                Text = text;
                Color = color;
            }

            public Translation()
            {
                Identifier = "";
                Text = "";
                Color = "";
            }
        }
        [XmlArrayItem("Reward")]
        [XmlArray(ElementName = "Rewards")]
        public Reward[] Rewards;

        [XmlArrayItem("Translation")]
        [XmlArray(ElementName = "Translations")]
        public Translation[] Translations;

        public void LoadDefaults()
        {
            User_ID = 0;
            Rewards = new Reward[]{
                new Reward("Reward",1)
            };
            Join_Commands = new List<string>() { "broadcast (player) connected to the server!" };
            Reminder_delay = 5; 
            Number_of_ads_before_cooldown = 1;
            Reward_mode = "ALL";
            CooldownTime = 15;
            Global_messages = true;
            Ad_on_join = false;
            Give_reward_when_video_unavailable = false;
            Executor_CSteamID = "";
            AdvancedLogging = false;

            Translations = new Translation[]{
                new Translation("EVENT_RECEIVED_REWARD_COOLDOWN", "You got your reward! Now you are on cooldown for {0} minutes", "default"),
                new Translation("EVENT_RECEIVED_REWARD_ADS_REMAIN", "You got your reward! You can watch {0} more ads before receiving cooldown.", "default"),
                new Translation("LINK_RESPONSE", "Here's your link!", "!not_required!"),
                new Translation("EVENT_RECEIVED_REWARD_ADS_GLOBAL", "{0} received his reward by watching ad. Get your reward with /ad command", "default"),
                new Translation("REMINDER_MESSAGE", "Get your reward by using /ad command!", "default"),
                new Translation("REMINDER_MESSAGE_JOIN", "oin commands have been reapplied!. Get your reward by using /ad command!", "default"),
                new Translation("COOLDOWN_EXPIRED", "Your cooldown has just expired!", "default"),
                new Translation("COOLDOWN", "You already received reward and now are on cooldown!", "default"),
                new Translation("COOLDOWN_REMAINING", "You can receive a new reward in {0} minutes!", "default"),
                new Translation("NOT_ON_COOLDOWN", "You can watch ad right now!", "default"),
                new Translation("REQUEST_LINK_MESSAGE", "Requesting your ad.", "default"),
                new Translation("COMPLETED_WITHOUT_VIDEO", "Unfortunately we didn't have any video for you, so you can't receive your reward.", "default")
            };
        }

    }
}
