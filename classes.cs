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
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

#pragma warning disable CS1998
#pragma warning disable CS8600
#pragma warning disable CS8602
#pragma warning disable CS8603
#pragma warning disable CS8604
#pragma warning disable CS8618
#pragma warning disable CS8622
#pragma warning disable CS8629

namespace TankDex
{
    public class data
    {
        public Dictionary<ulong, Dictionary<tank,uint>>? _data;
        public data()
        {
            this._data = new Dictionary<ulong, Dictionary<tank, uint>>();
        }
        public void load(index ind, string path = "data.txt")
        {
            string[] lines = File.ReadAllLines(path);

            foreach (string line in lines)
            {
                try
                {
                    ulong id = ulong.Parse(line.Split(';')[0]);
                    Dictionary<tank, uint> tanks = new Dictionary<tank, uint>();
                    foreach (string tank in line.Split(';')[1].Split('/'))
                    {
                        tanks.Add(ind.fromId(tank.Split('^')[0]), uint.Parse(tank.Split('^')[1]));
                    }
                    this._data.Add(id, tanks);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                }
            }
        }
        public void write(index ind, string path = "data.txt")
        {
            List<string> lines = new List<string>();
            foreach (KeyValuePair<ulong, Dictionary<tank,uint>> kvp in this._data)
            {
                string line = "";
                line += $"{kvp.Key};";
                foreach(KeyValuePair<tank, uint> tank in kvp.Value)
                {
                    line += $"{ind.fromTank(tank.Key)}^{tank.Value}/";
                }
                if (line[line.Length - 1] == '/')
                {
                    line = line.Substring(0, line.Length - 1);
                }
                lines.Add(line);
            }
            File.WriteAllLines(path, lines.ToArray());
        }
        public void add(ulong id, tank t)
        {
            if (this._data.ContainsKey(id))
            {
                Dictionary<tank, uint> tanks = this._data[id];
                if (tanks.ContainsKey(t))
                {
                    tanks[t]++;
                }
                else
                {
                    tanks.Add(t,1);
                }
                this._data[id] = tanks;
            }
            else
            {
                var a = new Dictionary<tank, uint>();
                a.Add(t,1);
                this._data.Add(id, a);
            }
        }
        public void rem(ulong id, tank t)
        {
            if (this._data.ContainsKey(id))
            {
                Dictionary<tank, uint> tanks = this._data[id];
                if (tanks.ContainsKey(t))
                {
                    if (tanks[t] == 1)
                    {
                        tanks.Remove(t);
                    }
                    else
                    {
                        tanks[t]--;
                    }
                }
                else
                {
                    return;
                }
                this._data[id] = tanks;
            }
            else
            {
                return;
            }
        }
        public uint amt(ulong id, tank t)
        {
            if (!this._data.ContainsKey(id)) return 0;
            return _data[id][t];
        }
        public int ama(ulong id)
        {
            if (!this._data.ContainsKey(id)) return 0;
            return _data[id].Count;
        }
        public uint tot(ulong id)
        {
            if (!this._data.ContainsKey(id)) return 0;
            uint amount = 0;
            foreach (var tank in this._data[id])
            {
                amount += tank.Value;
            }
            return amount;
        }
        public uint pow(ulong id)
        {
            if (!this._data.ContainsKey(id)) return 0;
            uint amount = 0;
            foreach (var tank in this._data[id])
            {
                amount += (uint)tank.Key.offence;
            }
            return amount;
        }
        public uint def(ulong id)
        {
            if (!this._data.ContainsKey(id)) return 0;
            uint amount = 0;
            foreach (var tank in this._data[id])
            {
                amount += (uint)tank.Key.defence;
            }
            return amount;
        }
    }
    public class config
    {
        [YamlMember(Alias = "token")]
        public string? Token { get; set; }
        [YamlMember(Alias = "link")]
        public string? Link { get; set; }

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
        public gldcfg(ulong guildid, ulong channelid, bool active, ulong cdnid, ulong cdnchid)
        {
            this.guildid = guildid;
            this.channelid = channelid;
            this.active = active;
            this.cdnid = cdnid;
            this.cdnchid = cdnchid;
        }
        public override string ToString()
        {
            return $"{this.guildid};{this.channelid};{this.active};{this.cdnid};{this.cdnchid}";
        }
        public static gldcfg[] load(string path = "guilds.txt")
        {
            List<gldcfg> output = new List<gldcfg>();
            string[] lines = File.ReadAllLines(path);
            foreach (string line in lines)
            {
                string[] a = line.Split(';');
                if (a.Length == 5)
                {
                    output.Add(new gldcfg(ulong.Parse(a[0]), ulong.Parse(a[1]), bool.Parse(a[2]), ulong.Parse(a[3]), ulong.Parse(a[4])));
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
        public static void write(gldcfg[] list, string path = "guilds.txt")
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
        public ulong cdnid { get; set; }
        public ulong cdnchid { get; set; }
    }
    public class index
    {
        public tank fromId(string id)
        {
            foreach (KeyValuePair<string, tank> kvp in this.tanks)
            {
                if (kvp.Key == id)
                {
                    return kvp.Value;
                }
            }
            return null;
        }
        public string fromTank(tank t)
        {
            foreach (KeyValuePair<string, tank> kvp in this.tanks)
            {
                if (kvp.Value == t)
                {
                    return kvp.Key;
                }
            }
            return null;
        }
        public string relativeImagePath { get; set; }
        public Dictionary<string, string> replace { get; set; }
        public Dictionary<string, tank> tanks { get; set; }

        public static string Serialize(index obj)
        {
            return JsonConvert.SerializeObject(obj, Formatting.Indented);
        }
        public static index Deserialize(string json)
        {
            return JsonConvert.DeserializeObject<index>(json);
        }
    }
    public class tank
    {
        public int? offence { get; set; }
        public int? defence { get; set; }
        public List<string> names { get; set; }
        public string file { get; set; }
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
        public activebtn(RestUserMessage rm, ulong ci, ulong gi, string ti, DateTime expirationtime)
        {
            this.channelid = ci;
            this.guildid = gi;
            this.tankid = ti;
            this.expirationtime = expirationtime;
            this.msg = rm;
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
        public activequestion(RestUserMessage ms, tank t, activebtn btn)
        {
            this.msg = ms;
            this.tank = t;
            this.btn = btn;
        }
        public activequestion(RestUserMessage ms, tank t, activebtn btn, string tkey)
        {
            this.msg = ms;
            this.tank = t;
            this.btn = btn;
            this.tkey = tkey;
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
        public activebtn? btn;
        public string? tkey;
    }
    public class activequery
    {
        public int page;
        public ulong? userid;
        public ulong? channelid;
        public ulong? guildid;
        public RestUserMessage msg;
        public DateTime expirationtime;
    }
    public static class util
    {
        public static int CalculateItemsOnPage(int totalItems, int itemsPerPage, int pageNumber)
        {
            // Calculate the total pages needed
            int totalPages = totalItems / itemsPerPage;

            // If there are remaining items, add one more page
            if (totalItems % itemsPerPage != 0)
            {
                totalPages++;
            }

            // Ensure pageNumber is within valid range
            pageNumber = Math.Max(1, Math.Min(totalPages, pageNumber));

            // Calculate the number of items on the specified page
            int itemsOnPage = Math.Min(itemsPerPage, totalItems - (pageNumber - 1) * itemsPerPage);

            return itemsOnPage;
        }
        public static int CalculatePagesNeeded(int totalItems, int itemsPerPage)
        {
            // Calculate the total pages needed
            int pagesNeeded = totalItems / itemsPerPage;

            // If there are remaining items, add one more page
            if (totalItems % itemsPerPage != 0)
            {
                pagesNeeded++;
            }

            return pagesNeeded;
        }
        private static readonly DateTimeOffset UnixEpoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);

        public static long ToUnixTime(this DateTimeOffset timestamp)
        {
            TimeSpan duration = timestamp - UnixEpoch;
            // There are 10 ticks per microsecond.
            return duration.Ticks;
        }
        public static bool CheckValidity(string guess, tank t, index i, activequestion qst)
        {
            foreach (string name in t.names)
            {
                if (DateTime.Compare(qst.btn.expirationtime, DateTime.UtcNow) < 0)
                    return false;

                string actualg = name.ToLower();
                foreach (KeyValuePair<string, string> kvp in i.replace)
                    actualg = actualg.Replace(kvp.Key, kvp.Value);
                string guessg = guess.ToLower();
                foreach (KeyValuePair<string, string> kvp in i.replace)
                    guessg = guessg.Replace(kvp.Key, kvp.Value);
                if (guessg == actualg)
                    return true;
            }
            return false;
        }
        public static bool isAdmin(SocketGuildUser user)
        {
            return user.GuildPermissions.Administrator;
        }
        public static string cdnget(string file, ulong cdngld, ulong channelid, DiscordSocketClient client)
        {
            SocketGuild cdn = client.GetGuild(cdngld);
            SocketTextChannel cdnchn = cdn.GetTextChannel(channelid);
            string temp_path = Path.Combine(Directory.GetParent(file).ToString(), $"tank{Path.GetExtension(file)}");
            File.Copy(file, temp_path);
            RestUserMessage msg = cdnchn.SendFileAsync(temp_path).Result;
            File.Delete(temp_path);
            return msg.Attachments.ToArray()[0].Url;
        }
    }
}
