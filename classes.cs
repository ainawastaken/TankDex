using Discord;
using Discord.Audio;
using Discord.Audio.Streams;
using Discord.Commands;
using Discord.Commands.Builders;
using Discord.Interactions;
using Discord.Interactions.Builders;
using Discord.Net;
using Discord.Net.Converters;
using Discord.Net.ED25519;
using Discord.Net.ED25519.Ed25519Ref10;
using Discord.Net.Queue;
using Discord.Net.Rest;
using Discord.Net.Udp;
using Discord.Net.WebSockets;
using Discord.Rest;
using Discord.Utils;
using Discord.Webhook;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace TankDex
{
    public class config
    {
        [YamlMember(Alias = "token")]
        public string? Token { get; set; }

        [YamlMember(Alias = "developers")]
        public ulong[]? Developers { get; set; }

        [YamlMember(Alias = "commands")]
        public Dictionary<string, string>? Commands { get; set; }

        public void load(out config cfg, string path = "config.yaml")
        {
            string yamlstr = File.ReadAllText(path);
            var deserializer = new DeserializerBuilder().Build();
            config tokenData = deserializer.Deserialize<config>(yamlstr);
            cfg = tokenData;
        }
        
        public void save(string path = "config.yaml")
        {
            var serializer = new SerializerBuilder().Build();
            string newYamlString = serializer.Serialize(this);
            File.WriteAllText(path, newYamlString);
        }
    }
    public class gldcfg
    {
        public gldcfg(ulong guildid, ulong channelid, bool active)
        {
            this.guildid = guildid;
            this.channelid = channelid;
            this.active = active;
        }
        public override string ToString()
        {
            return $"{this.guildid};{this.channelid};{this.active}";
        }
        public static gldcfg[] load(string path = "guilds.txt")
        {
            List<gldcfg> output = new List<gldcfg>();
            string[] lines = File.ReadAllLines(path);
            foreach (string line in lines)
            {
                string[] a = line.Split(';');
                if (a.Length == 3)
                {
                    output.Add(new gldcfg(ulong.Parse(a[0]), ulong.Parse(a[1]), bool.Parse(a[2])));
                }
            }

            return output.ToArray();
        }
        public static bool contains(gldcfg[] target, ulong guildid)
        {
            foreach (gldcfg gld in target)
            {
                if (gld.guildid == guildid)
                {
                    return true;
                }
            }
            return false;
        }
        public static void write(gldcfg[] list, string path = "guild.txt")
        {
            List<string> strs = new List<string>();
            foreach (gldcfg gld in list)
            {
                strs.Add(gld.ToString());
            }
            File.WriteAllLines(path, strs.ToArray());
        }
        public static int find(gldcfg[] list, ulong? id)
        {
            int ind = 0;
            foreach(gldcfg gld in list)
            {
                if (gld.guildid == id)
                {
                    return ind;
                }
                ind++;
            }
            return -1;
        }

        public ulong guildid { get; set; }
        public ulong channelid { get; set; }
        public bool active { get; set; }
    }
    public class index
    {
        public void load(out index cfg, string path = @"tanks\index.yaml")
        {
            string yamlstr = File.ReadAllText(path);
            var deserializer = new DeserializerBuilder().Build();
            index tokenData = deserializer.Deserialize<index>(yamlstr);
            cfg = tokenData;
        }

        [YamlMember(Alias = "replace")]
        public Dictionary<string, string>? replace { get; set; }
        [YamlMember(Alias = "tanks")]
        public Dictionary<string, tank>? tanks { get; set; }
    }
    public class tank
    {
        [YamlMember(Alias = "names")]
        public List<string>? names { get; set; }
        [YamlMember(Alias = "file")]
        public string? file { get; set; }
    }
    public class activebtn
    {
        public static activebtn find(activebtn[] btns, string id)
        {
            foreach (activebtn b in btns)
            {
                if (b.btn.CustomId == id)
                {
                    return b;
                }
            }
            return null;
        }
        public activebtn(ButtonComponent bc, RestUserMessage rm, ulong ci, ulong gi, string ti, DateTime expirationtime)
        {
            this.btn = bc;
            this.channelid = ci;
            this.guildid = gi;
            this.tankid = ti;
            this.expirationtime = expirationtime;
            this.msg = rm;
        }
        public ButtonComponent btn;
        public RestUserMessage msg;
        public ulong channelid;
        public ulong guildid;
        public string tankid;
        public DateTime expirationtime;
    }
    public class activequestion
    {
        public activequestion(RestUserMessage ms, tank t)
        {
            this.msg = ms;
            this.tank = t;
        }
        public static bool Contains(ulong msid, activequestion[] target)
        {
            foreach (activequestion qst in target)
            {
                if (qst.msg.Id == msid)
                    return true;
            }
            return false;
        }
        public static int Find(ulong msid, activequestion[] target)
        {
            int ind = 0;
            foreach (activequestion qst in target)
            {
                if (qst.msg.Id == msid)
                    return ind;
                else
                    ind++;
            }
            return -1;
        }
        public RestUserMessage? msg;
        public tank? tank;
    }
    public static class util
    {
        private static readonly DateTimeOffset UnixEpoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);

        public static long ToUnixTime(this DateTimeOffset timestamp)
        {
            TimeSpan duration = timestamp - UnixEpoch;
            // There are 10 ticks per microsecond.
            return duration.Ticks;
        }
        public static bool CheckValidity(string guess, tank t, index i)
        {
            foreach (string name in t.names)
            {
                string actual = guess.ToLower();
                foreach (KeyValuePair<string, string> kvp in i.replace)
                {
                    actual = actual.Replace(kvp.Key[0], kvp.Value[0]);
                }
                if (guess == actual)
                    return true;
            }
            return false;
        }
    }
}
