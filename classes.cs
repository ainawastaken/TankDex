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
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using System.Security.Cryptography;
using System.Runtime.CompilerServices;

#pragma warning disable 

namespace TankDex
{
    public class bindreact
    {
        public bindreact(RestUserMessage m, ReactionMetadata r, Emoji em, DateTime e, ulong[] usrid)
        {
            this.msg = m;
            this.userid = usrid;
            this.rct = new KeyValuePair<Emoji, ReactionMetadata?>[] 
            { 
                new KeyValuePair<Emoji, ReactionMetadata?>(em,r) 
            };
            this.expirationtime = e;
            Int32 unixTimestamp = (int)(e.ToUniversalTime().Subtract(new DateTime(1970, 1, 1)).TotalSeconds);
            this.unixEpiryTime = unixTimestamp;
        }
        public bindreact(RestUserMessage m, KeyValuePair<Emoji, ReactionMetadata?> r, DateTime e, ulong[] usrid)
        {
            this.msg = m;
            this.rct = new KeyValuePair<Emoji, ReactionMetadata?>[]
            {
                r
            };
            this.expirationtime = e;
            this.userid = usrid;
            Int32 unixTimestamp = (int)e.ToUniversalTime().Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
            this.unixEpiryTime = unixTimestamp;
        }
        public bindreact(RestUserMessage m, KeyValuePair<Emoji, ReactionMetadata?>[] r, DateTime e, ulong[] usrid)
        {
            this.msg = m;
            this.rct = r;
            this.userid = usrid;
            this.expirationtime = e;
            Int32 unixTimestamp = (int)e.ToUniversalTime().Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
            this.unixEpiryTime = unixTimestamp;
        }
        public void Fire(RestUserMessage m, KeyValuePair<Emoji, ReactionMetadata?> r)
        {
            this.func.Invoke(m, r, this);
        }
        public bool Check()
        {
            Int32 unixTimestamp = (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
            if (this.unixEpiryTime == 0) return false;
            if (this.unixEpiryTime < unixTimestamp) return true; else return false;
        }
        public Action<RestUserMessage, KeyValuePair<Emoji, ReactionMetadata?>, bindreact>? func;
        public RestUserMessage msg;
        public KeyValuePair<Emoji, ReactionMetadata?>[] rct;
        public DateTime? expirationtime;
        public readonly Int32? unixEpiryTime;
        public ulong[]? userid;
    }
    public class data
    {
        public Dictionary<ulong, Dictionary<tank,uint>>? _data;
        public data()
        {
            this._data = new Dictionary<ulong, Dictionary<tank, uint>>();
        }
        /// <Summary>
        /// Loads the data from a file in to this object.
        /// </Summary>
        /// <param name="ind">The index object for the bot</param>
        /// <param name="path">The path to the data file. By default its data.txt</param>
        /// <returns>void</returns>
        public void load(index ind, string path = "data.txt")
        {
            string[] lines = File.ReadAllLines(path);

            foreach (string line in lines)
            {
                ulong id = ulong.Parse(line.Split(';')[0]);
                //Console.WriteLine(id);
                Dictionary<tank, uint> tanks = new Dictionary<tank, uint>();
                foreach (string tank in line.Split(';')[1].Split('/'))
                {
                    if (!string.IsNullOrEmpty(tank))
                    {
                        try
                        {
                            tanks.Add(ind.fromId(tank.Split('^')[0]), uint.Parse(tank.Split('^')[1]));
                        }
                        catch
                        {
                            Console.WriteLine($"Couldnt parse \"{tank}\"");
                        }
                    }
                }
                this._data.Add(id, tanks);
            }
        }
        /// <Summary>
        /// Writes the data to the data file
        /// </Summary>
        /// <param name="ind">The index object for the bot</param>
        /// <param name="path">The path to the data file. By default its data.txt</param>
        /// <returns>void</returns>
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
                if (line[^1] == '/')
                {
                    line = line[..^1];
                }
                lines.Add(line);
            }
            File.WriteAllLines(path, lines.ToArray());
        }
        /// <Summary>
        /// Adds tank to the specified user ID
        /// </Summary>
        /// <param name="id">The ID of the target user</param>
        /// <param name="t">The tank object you are about to add</param>
        /// <returns>void</returns>
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
                var a = new Dictionary<tank, uint>
                {
                    { t, 1 }
                };
                this._data.Add(id, a);
            }
        }
        /// <Summary>
        /// Removes a tank from the specified user ID
        /// </Summary>
        /// <param name="id">The ID of the target user</param>
        /// <param name="t">The tank object you are about to remove</param>
        /// <returns>void</returns>
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
        /// <Summary>
        /// Gets the amount of a certain tank owned by the specified user
        /// </Summary>
        /// <param name="id">The ID of the target user</param>
        /// <param name="t">The tank object you are about to check</param>
        /// <returns>The amount of tanks in UINT</returns>
        public uint amt(ulong id, tank t)
        {
            if (!this._data.ContainsKey(id)) return 0;
            return _data[id][t];
        }
        /// <Summary>
        /// Gets the amount of tanks owned by the specified user
        /// </Summary>
        /// <param name="id">The ID of the target user</param>
        /// <returns>The amount of owned tanks in INT</returns>
        public int ama(ulong id)
        {
            if (!this._data.ContainsKey(id)) return 0;
            return _data[id].Count;
        }
        /// <Summary>
        /// Gets the total amount of tanks owned by the specified user
        /// </Summary>
        /// <param name="id">The ID of the target user</param>
        /// <returns>The total amount of owned tanks in UINT</returns>
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
        /// <Summary>
        /// Gets the total offence power of the specified user
        /// </Summary>
        /// <param name="id">The ID of the target user</param>
        /// <returns>The total offence power of owned tanks in UINT</returns>
        public uint pow(ulong id)
        {
            if (!this._data.ContainsKey(id)) return 0;
            uint amount = 0;
            foreach (var tank in this._data[id])
            {
                amount += (uint)tank.Key.offence * tank.Value;
            }
            return amount;
        }
        /// <Summary>
        /// Gets the total defence power of the specified user
        /// </Summary>
        /// <param name="id">The ID of the target user</param>
        /// <returns>The total power of owned tanks in UINT</returns>
        public uint def(ulong id)
        {
            if (!this._data.ContainsKey(id)) return 0;
            uint amount = 0;
            foreach (var tank in this._data[id])
            {
                amount += (uint)tank.Key.defence * tank.Value;
            }
            return amount;
        }
        /// <Summary>
        /// Returns true if user owns a certain tank
        /// </Summary>
        /// <param name="id">The ID of the target user</param>
        /// <param name="t">The tank to check</param>
        /// <returns>True if the tank is owned</returns>
        public bool has(ulong id, tank t)
        {
            if (this._data[id].ContainsKey(t)) return true;
            else return false;
        }
        /// <Summary>
        /// Returns the tank completion
        /// </Summary>
        /// <param name="id">The ID of the target user</param>
        /// <param name="ind">The target index</param>
        /// <returns>The completion in%<returns>
        public float cml(ulong id, index ind)
        {
            string[] allIds = ind.tanks.Keys.ToArray();
            List<string> _ownedIds = new List<string>();
            foreach (tank t in _data[id].Keys)
            {
                string _id = ind.fromTank(t);
                _ownedIds.Add(_id);
                if (!allIds.Contains(_id))
                {
                    Console.WriteLine(_id);
                }
            }
            string[] ownedIds = _ownedIds.ToArray();
            // Check for empty arrays or null references
            if (allIds == null || allIds.Length == 0 || ownedIds == null || ownedIds.Length == 0)
            {
                throw new ArgumentException("Array of IDs cannot be null or empty.");
            }

            // Convert arrays to HashSet for faster lookup
            HashSet<string> allIdsSet = new HashSet<string>(allIds);
            HashSet<string> ownedIdsSet = new HashSet<string>(ownedIds);

            // Count the number of owned IDs that are also valid
            int ownedValidCount = ownedIdsSet.Count(id => allIdsSet.Contains(id));

            // Calculate percentage
            float percentage = (float)Math.Round(((float)ownedValidCount / allIdsSet.Count) * 100,1);



            return percentage;
        }
    }
    public class config
    {
        [YamlMember(Alias = "token")]
        public string? Token { get; set; }
        [YamlMember(Alias = "link")]
        public string? Link { get; set; }

        [YamlMember(Alias = "master")]
        public ulong? Master { get; set; }

        [YamlMember(Alias = "developers")]
        public ulong[]? Developers { get; set; }
        [YamlMember(Alias = "windows")]
        public bool? Windows { get; set; }

        [YamlMember(Alias = "commands")]
        public Dictionary<string, string>? Commands { get; set; }
        [YamlMember(Alias = "devtoken")]
        public string? DevToken { get; set; }
        [YamlMember(Alias = "devlink")]
        public string? DevLink { get; set; }
        [YamlMember(Alias = "devmode")]
        public bool? DevMode { get; set; }
        [YamlMember(Alias = "VERSION")]
        public float? VERSION { get; set; }

        /// <Summary>
        /// Loads the config file
        /// </Summary>
        /// <param name="cfg">The target config object</param>
        /// <param name="path">The target config file path</param>
        /// <returns>void</returns>
        public static void load(out config cfg, string path = "config.yaml")
        {
            string yamlstr = File.ReadAllText(path);
            var deserializer = new DeserializerBuilder().Build();
            config tokenData = deserializer.Deserialize<config>(yamlstr);
            cfg = tokenData;
        }
        /// <Summary>
        /// Saves the config file
        /// </Summary>
        /// <param name="path">The target config file path</param>
        /// <returns>void</returns>
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
        /// <Summary>
        /// Loads the configs for the guilds
        /// </Summary>
        /// <param name="path">The target config file path. By default its guilds.txt</param>
        /// <returns>Array of all the guild configs</returns>
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
        /// <Summary>
        /// Checks if array of guilds contain certain guild ID
        /// </Summary>
        /// <param name="target">The target guild config array</param>
        /// <param name="guildid">The guild ID to search for</param>
        /// <returns>True if found</returns>
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
        /// <Summary>
        /// Writes guild config to target file
        /// </Summary>
        /// <param name="list">The target list of guild configs to write</param>
        /// <param name="path">Path to the target file. By default its guild.txt</param>
        /// <returns>void</returns>
        public static void write(gldcfg[] list, string path = "guilds.txt")
        {
            List<string> strs = new List<string>();
            foreach (gldcfg gld in list)
            {
                strs.Add(gld.ToString());
            }
            File.WriteAllLines(path, strs.ToArray());
        }
        /// <Summary>
        /// Locates index of guild config you are looking for
        /// </Summary>
        /// <param name="list">The target list of guild configs to search in</param>
        /// <param name="id">The ID to look for in the list of guild configs.</param>
        /// <returns>The index of the located config in INT</returns>
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
            Console.WriteLine(id);
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
                Console.WriteLine($"    {msid}\n    {qst.msg.Id}");
                if (qst.msg.Id == msid)
                {
                    Console.WriteLine("    yes");
                    return true;
                }
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
        public bool IsOwner(ulong usrid)
        {
            if (usrid == this.userid) return true;
            return false;
        }
        public static bool Contains(ulong msid, activequery[] target)
        {
            foreach (activequery qst in target)
            {
                if (qst.msg.Id == msid)
                    return true;
            }
            return false;
        }
        public static int Find(ulong msid, activequery[] target)
        {
            int ind = 0;
            foreach (activequery qst in target)
            {
                if (qst.msg.Id == msid)
                    return ind;
                else
                    ind++;
            }
            return -1;
        }
        public int page;
        public ulong? userid;
        public ulong? channelid;
        public ulong? guildid;
        public RestUserMessage msg;
        public DateTime expirationtime;
    }
    public class activegiving
    {
        public static bool Contains(ulong msid, activegiving[] target)
        {
            foreach (activegiving qst in target)
            {
                if (qst.msg.Id == msid)
                    return true;
            }
            return false;
        }
        public static int Find(ulong msid, activegiving[] target)
        {
            int ind = 0;
            foreach (activegiving qst in target)
            {
                if (qst.msg.Id == msid)
                    return ind;
                else
                    ind++;
            }
            return -1;
        }
        public string tkey;
        public ulong user;
        public ulong subject;
        public RestUserMessage msg;
        public DateTime expirationtime;
    }
    public class activeinfo
    {
        public static bool Contains(ulong msid, activeinfo[] target)
        {
            foreach (activeinfo qst in target)
            {
                if (qst.msg.Id == msid)
                    return true;
            }
            return false;
        }
        public ulong user;
        public static int Find(ulong msid, activeinfo[] target)
        {
            int ind = 0;
            foreach (activeinfo qst in target)
            {
                if (qst.msg.Id == msid)
                    return ind;
                else
                    ind++;
            }
            return -1;
        }
        public string tkey;
        public RestUserMessage msg;
        public DateTime expirationtime;
    }
    public class tryParseResponse
    {
        public tryParseResponse(object r, bool s)
        {
            this.success = s;
            this.response = r;
        }
        public bool success;
        public object response;
    }
    public static class util
    {
        public static string GetFileNameFromUrl(string url)
        {
            // Use Uri to parse the URL
            Uri uri = new Uri(url);

            // Get the file name and extension from the URL
            string fileName = Path.GetFileName(uri.LocalPath);

            return fileName;
        }
        public static string CalculateSHA256(string input)
        {
            using SHA256 sha256 = SHA256.Create();
            // Convert the input string to a byte array and compute the hash.
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] hashBytes = sha256.ComputeHash(inputBytes);

            // Convert the byte array to a hexadecimal string.
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                builder.Append(hashBytes[i].ToString("x2")); // "x2" formats byte to hexadecimal
            }
            return builder.ToString();
        }
        public static void logError(Exception ex, string path)
        {
            using StreamWriter writer = new StreamWriter(path, true);
            DateTime now = DateTime.UtcNow;
            Int32 unixTimestamp = (int)now.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
            writer.WriteLine($"[{unixTimestamp}]");
            writer.WriteLine($"Exception occurred at {DateTime.Now}");
            writer.WriteLine($"   Message: {ex.Message}");
            writer.WriteLine($"   Stack Trace:");
            writer.WriteLine($"   {ex.StackTrace}");
            writer.WriteLine(new string('-', 30));
        }
        public static string FindClosestMatch(string userInput, List<string> options)
        {
            // Find the option with the smallest Levenshtein distance
            var closestMatch = options.OrderBy(option => ComputeLevenshteinDistance(userInput, option)).First();
            return closestMatch;
        }
        public static int ComputeLevenshteinDistance(string s, string t)
        {
            int[,] distance = new int[s.Length + 1, t.Length + 1];

            for (int i = 0; i <= s.Length; i++)
                distance[i, 0] = i;

            for (int j = 0; j <= t.Length; j++)
                distance[0, j] = j;

            for (int j = 1; j <= t.Length; j++)
            {
                for (int i = 1; i <= s.Length; i++)
                {
                    int cost = (s[i - 1] == t[j - 1]) ? 0 : 1;
                    distance[i, j] = Math.Min(Math.Min(
                        distance[i - 1, j] + 1,
                        distance[i, j - 1] + 1),
                        distance[i - 1, j - 1] + cost);
                }
            }

            return distance[s.Length, t.Length];
        }
        public static string[] GetItemsOnPage(string[] allItems, int itemsPerPage, int pageNumber)
        {
            // Calculate the starting index of the items on the specified page
            int startIndex = pageNumber * itemsPerPage;

            // Calculate the ending index of the items on the specified page
            int endIndex = Math.Min(startIndex + itemsPerPage, allItems.Length);

            // Extract the items on the specified page from the array
            string[] itemsOnPage = new string[endIndex - startIndex];
            Array.Copy(allItems, startIndex, itemsOnPage, 0, endIndex - startIndex);

            return itemsOnPage;
        }
        public static bool IsValidId(string id)
        {
            if (id.Length == 7 && id.StartsWith("#"))
            {
                for (int i = 1; i < id.Length; i++)
                {
                    if (!char.IsDigit(id[i]))
                    {
                        return false;
                    }
                }

                string numberString = id[1..];
                int number = int.Parse(numberString);
                if (number < 100000)
                {
                    for (int i = 1; i < numberString.Length; i++)
                    {
                        if (numberString[i] != '0')
                        {
                            return false;
                        }
                    }
                }

                return true;
            }
            else
            {
                return false;
            }
        }
        public static tryParseResponse ExctractNumberFromId(string id)
        {
            if (id.StartsWith("#"))
            {
                string numberString = id[1..];
                if (int.TryParse(numberString, out int number))
                {
                    return new tryParseResponse(number, true);
                }
                else
                {
                    Console.WriteLine("Invalid ID format: cannot parse number.");
                    return new tryParseResponse(null, false);
                }
            }
            else
            {
                Console.WriteLine("Invalid ID format: must start with \"#\"");
                return new tryParseResponse(null, false);
            }
        }
        public static tryParseResponse convertNumberToId(int number)
        {
            if (number >= 0 && number <= 999999)
            {
                string id = number.ToString("D6");
                return new tryParseResponse($"#{id}", true);
            }
            else
            {
                Console.WriteLine("Invalid number range. Must be between 0 and 999999.");
                return new tryParseResponse("", true);
            }
        }
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
        public static string cdnget(string file, ulong cdngld, ulong channelid, DiscordSocketClient client, ref cache _cache)
        {
            if (_cache._cache.ContainsKey(file))
            {
                if (_cache.checkSingle(file))
                {
                    return _cache.get(file);
                }
                else
                {
                    SocketGuild cdn = client.GetGuild(cdngld);
                    SocketTextChannel cdnchn = cdn.GetTextChannel(channelid);
                    string temp_path = Path.Combine(Directory.GetParent(file).ToString(), $"tank{Path.GetExtension(file)}");
                    if (File.Exists(temp_path))
                    {
                        File.Delete(temp_path);
                    }
                    File.Copy(file, temp_path);
                    RestUserMessage msg = cdnchn.SendFileAsync(temp_path).Result;
                    File.Delete(temp_path);
                    _cache.add(file, msg.Attachments.ToArray()[0].Url);
                    File.WriteAllText("cache.json", _cache.Serialize());
                    return msg.Attachments.ToArray()[0].Url;
                }
            }
            else
            {
                SocketGuild cdn = client.GetGuild(cdngld);
                SocketTextChannel cdnchn = cdn.GetTextChannel(channelid);
                string temp_path = Path.Combine(Directory.GetParent(file).ToString(), $"tank{Path.GetExtension(file)}");
                if (File.Exists(temp_path))
                {
                    File.Delete(temp_path);
                }
                File.Copy(file, temp_path);
                RestUserMessage msg = cdnchn.SendFileAsync(temp_path).Result;
                File.Delete(temp_path);
                _cache.add(file, msg.Attachments.ToArray()[0].Url);
                File.WriteAllText("cache.json", _cache.Serialize());
                return msg.Attachments.ToArray()[0].Url;
            }
        }
        public static void WriteArray(object[] input)
        {
            Console.Write($"{{ ");
            foreach (object item in input)
            {
                Console.Write($"{item}, ");
            }
            Console.Write($" }}\n");
        }
        public static void WriteArray(List<object> input)
        {
            Console.Write($"{{ ");
            foreach (object item in input)
            {
                Console.Write($"{item}, ");
            }
            Console.Write($" }}\n");
        }
        public static void WriteArray(List<string> input)
        {
            Console.Write($"{{ ");
            foreach (object item in input)
            {
                Console.Write($"{item}, ");
            }
            Console.Write($" }}\n");
        }
    }
    public class chunk
    {
        public static chunk Deserialize(string json)
        {
            return JsonConvert.DeserializeObject<chunk>(json);
        }
        public static string Serialize(chunk obj)
        {
            return JsonConvert.SerializeObject(obj, Formatting.Indented);
        }
        public static string Serialize2(chunk obj)
        {
            return JsonConvert.SerializeObject(obj, Formatting.None);
        }
        public int? offence { get; set; }
        public int? defence { get; set; }
        public List<string> names { get; set; }
        public string? file { get; set; }
        public string? tkey { get; set; }
    }
    public class cache
    {
        // file, url
        public Dictionary<string, string> _cache = new Dictionary<string, string>();

        public string get(string file)
        {
            foreach (KeyValuePair<string, string> kvp in this._cache)
            {
                if (kvp.Key == file)
                {
                    return kvp.Value;
                }
            }
            return null;
        }
        public void add(string file, string url)
        {
            if (!this._cache.ContainsKey(file))
            {
                this._cache.Add(file, url);
            }
        }
        public bool checkSingle(string file)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(this.get(file));
                request.Method = "HEAD"; // Use HEAD method to only get headers, not full content

                using HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                // Check if the response status code indicates success
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    _cache.Remove(file);
                    return false;
                }
                else
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
        public void checkAll()
        {
            foreach (string file in this._cache.Keys.ToArray())
            {
                try
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(this.get(file));
                    request.Method = "HEAD"; // Use HEAD method to only get headers, not full content

                    using HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                    // Check if the response status code indicates success
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        _cache.Remove(file);
                        Console.WriteLine($"Removed {file}");
                    }
                }
                catch
                {
                    _cache.Remove(file);
                    Console.WriteLine($"Removed {file}");
                }
            }
        }
        public static cache Deserialize(string json)
        {
            return JsonConvert.DeserializeObject<cache>(json);
        }
        public string Serialize()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
    public class FileUploader
    {
        private readonly HttpClient _httpClient;

        public FileUploader()
        {
            _httpClient = new HttpClient();
        }

        public async Task<string> UploadFile(string filePath)
        {
            try
            {
                string apiUrl = "https://tmpfiles.org/api/v1/upload";

                // Create multipart form data content
                using var formData = new MultipartFormDataContent();

                // Read the file data
                byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
                var fileContent = new ByteArrayContent(fileBytes);

                // Add the file content to the form data
                formData.Add(fileContent, "file", Path.GetFileName(filePath));

                // Send the POST request to the API endpoint
                var response = await _httpClient.PostAsync(apiUrl, formData);

                // Check if the request was successful
                if (response.IsSuccessStatusCode)
                {
                    // Read the response content
                    string responseBody = await response.Content.ReadAsStringAsync();
                    Console.WriteLine(responseBody);
                    return responseBody;
                }
                else
                {
                    // Handle request failure
                    Console.WriteLine($"Failed to upload file. Status code: {response.StatusCode}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading file: {ex.Message}");
                return null;
            }
        }
    }
    public class ApiResponse
    {
        public string Status { get; set; }
        public ApiData Data { get; set; }
    }
    public class ApiData
    {
        public string Url { get; set; }
    }
    public class DisconnectLog
    {
        public readonly List<Exception> errors = new List<Exception>();
        public void Add(Exception ex, string path)
        {
            errors.Add(ex);
            File.WriteAllText(path, JsonConvert.SerializeObject(this, Formatting.Indented));
        }
        public void Load(string path)
        {
            DisconnectLog deserializedConfig = null;
            try
            {
                deserializedConfig = JsonConvert.DeserializeObject<DisconnectLog>(File.ReadAllText(path));
            }
            catch
            {
                File.WriteAllText(path, "");
                deserializedConfig.errors.Clear();
                deserializedConfig.errors.Add(null);
                File.WriteAllText(path, JsonConvert.SerializeObject(this, Formatting.Indented));
            }

            PropertyInfo[] properties = typeof(DisconnectLog).GetProperties();
            foreach (PropertyInfo property in properties)
            {
                if (property.CanWrite)
                {
                    PropertyInfo deserializedProperty = typeof(DisconnectLog).GetProperty(property.Name);
                    object value = deserializedProperty.GetValue(deserializedConfig);
                    property.SetValue(this, value);
                }
            }
        }
    }
}
