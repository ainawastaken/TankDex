#pragma warning disable IDE0079
#pragma warning disable CS1998
#pragma warning disable CS8600
#pragma warning disable CS8601
#pragma warning disable CS8602
#pragma warning disable CS8603
#pragma warning disable CS8604
#pragma warning disable CS8618
#pragma warning disable CS8622
#pragma warning disable CS8629
#pragma warning disable SYSLIB0014
#pragma warning disable IDE0050
#pragma warning disable IDE0090
#pragma warning disable IDE0063
#pragma warning disable IDE0044
#pragma warning disable IDE0017
#pragma warning disable IDE0052
#pragma warning disable IDE0079
#pragma warning disable IDE0060
#pragma warning disable IDE0063
#pragma warning disable IDE0059

using System;
using System.Timers;
using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.IO.Compression;

// Perm: 277025778752
// link: https://discord.com/oauth2/authorize?client_id=1182017101897154621&permissions=277025778752&scope=bot+applications.commands

namespace TankDex
{
    internal class Program
    {
        private DiscordSocketClient _client;
        private CommandService _commands;
        //private IServiceProvider _services;

        private data data = new data();
        private config cfg = new config();
        private index index = new index();
        private cache cache = new cache();

        private ulong cdnid = ulong.MaxValue;
        private ulong cdnchid = ulong.MaxValue;

        private volatile static List<activebtn> activebuttons = new List<activebtn>();
        private volatile static List<activequestion> activequestions = new List<activequestion>();
        private volatile static List<activequery> activequeries = new List<activequery>();
        private volatile static List<activegiving> activegivings = new List<activegiving>();
        private volatile static List<activeinfo> activeinfos = new List<activeinfo>();
        private volatile static List<SocketGuild> _guilds = new List<SocketGuild>();
        private volatile static List<gldcfg> guilds = new List<gldcfg>();
        private volatile static List<KeyValuePair<ulong,DateTime>> blocked = new List<KeyValuePair<ulong, DateTime>>();

        public static Dictionary<string, string> paths = new Dictionary<string, string>();

        private System.Timers.Timer buttonTimer;
        private System.Timers.Timer spawnTimer;
        private System.Timers.Timer cacheTimer;

        private FileUploader fileUploader = new FileUploader();

        static void Main(string[] args)
        {
            try
            {
                new Program().RunBotAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                string logFilePath = "crashlog.txt";

                // Write the exception details to a log file
                using (StreamWriter writer = new StreamWriter(logFilePath, true))
                {
                    DateTime now = DateTime.UtcNow;
                    Int32 unixTimestamp = (int)now.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
                    writer.WriteLine($"[{unixTimestamp}]");
                    writer.WriteLine($"Exception occurred at {DateTime.Now}");
                    writer.WriteLine($"   Message: {ex.Message}");
                    writer.WriteLine($"   Stack Trace:");
                    writer.WriteLine($"   {ex.StackTrace}");
                    writer.WriteLine(new string('-', 30));
                    Console.WriteLine($"[{unixTimestamp}]");
                    Console.WriteLine($"Exception occurred at {DateTime.Now}");
                    Console.WriteLine($"   Message: {ex.Message}");
                    Console.WriteLine($"   Stack Trace:");
                    Console.WriteLine($"   {ex.StackTrace}");
                }
            }
        }
        public async Task RunBotAsync()
        {
            _client = new DiscordSocketClient(
            new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent,
                UseInteractionSnowflakeDate = false,
                LogLevel = LogSeverity.Info
            });
            _commands = new CommandService();
            config.load(out cfg);
            if ((bool)cfg.Windows) paths.Add("index", "tanks\\index.json"); else paths.Add("index", "tanks/index.json");
            if ((bool)cfg.Windows) paths.Add("tempdir", "tanks\\temp"); else paths.Add("tempdir", "tanks/temp");
            if ((bool)cfg.Windows) paths.Add("images", "tanks\\images"); else paths.Add("images", "tanks/images");
            if ((bool)cfg.Windows) paths.Add("images2", "tanks\\images\\"); else paths.Add("images2", "tanks/images/");
            if ((bool)cfg.Windows) paths.Add("inddest", $"{paths["tempdir"]}\\index.zip"); else paths.Add("inddest", $"{paths["tempdir"]}/index.zip");
            if ((bool)cfg.Windows) paths.Add("old", $"tanks\\old"); else paths.Add("old", $"tanks/old");
            index = index.Deserialize(File.ReadAllText(paths["index"]));
            guilds = gldcfg.load().ToList();
            data.load(index);
            cache = cache.Deserialize(File.ReadAllText("cache.json"));
            string[] cdnstr = File.ReadAllText("cdn.txt").Split(';');
            cdnid = ulong.Parse(cdnstr[0]);
            cdnchid = ulong.Parse(cdnstr[1]);
            Directory.CreateDirectory(paths["tempdir"]);
            Directory.CreateDirectory(paths["old"]);
            DirectoryInfo di = new DirectoryInfo(paths["tempdir"]);
            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }

            _client.Log += Log;
            _client.Ready += Client_Ready;
            _client.SlashCommandExecuted += SlashCommandHandler;
            _client.GuildAvailable += GuildAvailable;
            _client.MessageReceived += MessageReceived;
            _client.ReactionAdded += ReactionAdded;

            
            //chidsock.SendMessageAsync();
           
            //_client.ButtonExecuted += ButtonExecuted;
            //_client.InteractionCreated += InteractionCreated;
            
            buttonTimer = new System.Timers.Timer(1000);
            buttonTimer.Elapsed += OnButtonTimedEvent;
            buttonTimer.AutoReset = true;
            buttonTimer.Enabled = true;
            
            spawnTimer = new System.Timers.Timer(30*60000);
            spawnTimer.Elapsed += SpawnTimerEvent;
            spawnTimer.AutoReset = true;
            spawnTimer.Enabled = true;

            cacheTimer = new System.Timers.Timer(10000);
            cacheTimer.Elapsed += CacheTimerEvent;
            cacheTimer.AutoReset = true;
            cacheTimer.Enabled = true;

            if ((bool)cfg.DevMode)
            {
                _client.LoginAsync(TokenType.Bot, cfg.DevToken).Wait();
            }
            else
            {
                _client.LoginAsync(TokenType.Bot, cfg.Token).Wait();
            }
            _client.StartAsync().Wait();

            //var chidsock = _client.GetGuild(cdnid).GetChannel(cdnchid) as SocketTextChannel;
            string mentions = "";
            foreach (ulong id in cfg.Developers)
            {
                mentions += $"<@{id}>\n";
            }
            mentions += "";

            await Task.Delay(-1);
        }
        private Task Log(LogMessage arg)
        {
            Console.WriteLine(arg);
            return Task.CompletedTask;
        }
        public async Task Client_Ready()
        {
            await _client.SetGameAsync($"{index.tanks.Count} tanks!", null, ActivityType.Watching);
        }
        public async Task GuildAvailable(SocketGuild guild)
        {
            if (!gldcfg.contains(guilds.ToArray(), guild.Id))
            {
                guilds.Add(new gldcfg(guild.Id, ulong.MaxValue, true, ulong.MaxValue, ulong.MaxValue));
                gldcfg.write(guilds.ToArray());
            }
            _guilds.RemoveAll(x => x == guild);
            _guilds.Add(guild);
        }
        private async Task SlashCommandHandler(SocketSlashCommand command)
        {
            switch (command.Data.Name)
            {
                case "reload":
                    if (cfg.Developers.Contains(command.User.Id))
                    {
                        await command.RespondAsync("Not implemented", null, false, true);
                    }
                    else
                    {
                        await command.RespondAsync("You're not a developer!", null, false, true);
                    }
                    break;
                case "getcfg":
                    if (cfg.Developers.Contains(command.User.Id))
                    {
                        int gldi2 = gldcfg.find(guilds.ToArray(), command.GuildId);
                        await command.RespondAsync($"Config for this guild: **\"{guilds[gldi2]}\"**", null, false, false);
                    }
                    else
                    {
                        await command.RespondAsync("You're not a developer!", null, false, true);
                    }
                    break;
                case "trigger":
                    if (cfg.Developers.ToArray().Contains(command.User.Id) & util.isAdmin(_client.GetGuild((ulong)command.GuildId).GetUser(command.User.Id)))
                    {
                        await command.RespondAsync($"Triggered! ||(Developer only)||", null, false, true); 
                        Random rnd = new Random(); 
                        await Spawn(command.Channel, rnd.Next(0, index.tanks.Count)); 
                    }
                    else
                    {
                        await command.RespondAsync("You're not a developer/admin!", null, false, true);
                    }
                    break;
                case "activate":
                    if (util.isAdmin(_client.GetGuild((ulong)command.GuildId).GetUser(command.User.Id)))
                    {
                        int a = gldcfg.find(guilds.ToArray(), command.GuildId);
                        gldcfg b = guilds[a];
                        b.active = true;
                        b.channelid = (ulong)command.ChannelId;
                        guilds[a] = b;
                        gldcfg.write(guilds.ToArray());
                        await command.RespondAsync("Activated", null, false, true);
                    }
                    else
                    {
                        await command.RespondAsync("You do not have the administrator permission!", null, false, true);
                    }
                    break;
                case "disable":
                    if (util.isAdmin(_client.GetGuild((ulong)command.GuildId).GetUser(command.User.Id)))
                    {
                        int gldi = gldcfg.find(guilds.ToArray(), command.GuildId);
                        gldcfg gld = guilds[gldi];
                        gld.active = false;
                        guilds[gldi] = gld;
                        gldcfg.write(guilds.ToArray());
                        await command.RespondAsync("Deactivated", null, false, true);
                    }
                    else
                    {
                        await command.RespondAsync("You do not have the administrator permission!", null, false, true);
                    }
            break;
                case "completion":
                    EmbedBuilder eb = new EmbedBuilder();
                    if (data._data.ContainsKey(command.User.Id))
                    {
                        eb.Title = $"Tank completion of {command.User.GlobalName}";
                        eb.Description = $"{command.User.GlobalName} owns {data.ama(command.User.Id)} unique tanks.\n" +
                            $"And {data.tot(command.User.Id)} tanks in total.\n";
                        eb.AddField("Completion", $"%{data.cml(command.User.Id, index)}\n{data.ama(command.User.Id)}/{index.tanks.Count}", false);
                        eb.AddField("Offence", data.pow(command.User.Id), true);
                        eb.AddField("Defence", data.def(command.User.Id), true);
                        eb.ThumbnailUrl = command.User.GetAvatarUrl();
                        await command.RespondAsync(null, embed: eb.Build());
                    }
                    else
                    {
                        await command.RespondAsync("You do not own any tanks!", null, false, true);
                    }
                    break;
                case "setcdn":
                    if (cfg.Developers.Contains(command.User.Id) & util.isAdmin(_client.GetGuild((ulong)command.GuildId).GetUser(command.User.Id)))
                    {
                        File.WriteAllText("cdn.txt", $"{command.GuildId};{command.ChannelId}");
                        cdnid = (ulong)command.GuildId;
                        cdnchid = (ulong)command.ChannelId;
                        await command.RespondAsync("CDN set!", null, false, true);
                    }
                    else
                    {
                        await command.RespondAsync("You're not a developer!", null, false, true);
                    }
                    break;
                case "info":
                    EmbedBuilder _eb = new EmbedBuilder();
                    DateTime expiry = DateTime.UtcNow.AddMinutes(1);
                    Int32 unixTimestamp = (int)expiry.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
                    activequery aq = new activequery();
                    _eb.Title = $"Tank info for {command.User.GlobalName}";
                    _eb.Description = $"Page {aq.page}/{util.CalculatePagesNeeded(data.ama(command.User.Id), 25)}\n{util.CalculateItemsOnPage(data.ama(command.User.Id), 25, 0)}/25 per page" +
                        $"\nType \"next\" for next page. \"previous\" for previous page, or page number. And tank ID for info\n" +
                        $"Will timeout <t:{unixTimestamp}:R>";
                    aq.page = 0;
                    aq.expirationtime = expiry;
                    aq.userid = command.User.Id;
                    aq.channelid = command.ChannelId;
                    aq.guildid = command.GuildId;

                    int depth = 0;
                    foreach (KeyValuePair<tank, uint> t in data._data[command.User.Id])
                    {
                        if (depth == 25) break;
                        EmbedFieldBuilder efb = new EmbedFieldBuilder();
                        efb.WithName($"{index.fromTank(t.Key)} x{t.Value}");
                        efb.WithValue(t.Key.names[0]);
                        efb.IsInline = true;
                        _eb.AddField(efb);
                        depth++;
                    }
                    await command.RespondAsync(embed: _eb.Build());
                    var messages = await command.Channel.GetMessagesAsync(1).FlattenAsync();
                    var response = messages.FirstOrDefault(m => m.Author.Id == _client.CurrentUser.Id);

                    aq.msg = (RestUserMessage)response;

                    activequeries.Add( aq );

                    break;
                case "give":
                    EmbedBuilder __eb = new EmbedBuilder();
                    activegiving ag = new activegiving();
                    DateTime _expiry = DateTime.UtcNow.AddMinutes(1);
                    Int32 _unixTimestamp = (int)_expiry.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
                    ag.expirationtime = _expiry;
                    ag.user = command.User.Id;
                    EmbedBuilder ___eb = new EmbedBuilder();
                    ___eb.WithAuthor(command.User.GlobalName, command.User.GetDisplayAvatarUrl());
                    ___eb.WithTitle("Who would you like to gift a tank to?");
                    ___eb.WithDescription($"@ the user and the ID of the tank, like this: \"*<@{_client.CurrentUser.Id}>:#000001*\"\nMind the colon!\nExpires <t:{_unixTimestamp}:R>");
                    ___eb.WithFooter($"You can do this from the info menu if you would like to see the tank before you trade it");

                    await command.RespondAsync(embed: ___eb.Build());
                    var _messages = await command.Channel.GetMessagesAsync(1).FlattenAsync();
                    var _response = _messages.FirstOrDefault(m => m.Author.Id == _client.CurrentUser.Id);

                    ag.msg = (RestUserMessage)_response;
                    activegivings.Add(ag);

                    break;
            }
            data.write(index);
        }
        private async Task MessageReceived(SocketMessage message)
        {
            if (message is not IUserMessage userMessage || message.Author.IsBot)
                return;

            Random rnd = new Random();
            IGuild guild = (userMessage.Channel as IGuildChannel)?.Guild;

            var msg2 = message as IUserMessage;
            var msg3 = message as SocketUserMessage;

            if (message.CleanContent == "$$loadcoms$$" && cfg.Developers.Contains(message.Author.Id))
            {
                await message.AddReactionAsync(new Emoji("⏳"));
                foreach (KeyValuePair<string, string> coms in cfg.Commands)
                {
                    var guildCommand = new Discord.SlashCommandBuilder();
                    guildCommand.WithName(coms.Key);
                    guildCommand.WithDescription(coms.Value);
                    await guild.CreateApplicationCommandAsync(guildCommand.Build());
                }
                await message.AddReactionAsync(new Emoji("✅"));
                return;
            }
            else if (Regex.IsMatch(message.Content, "\\$\\$addtank\\$\\$\\[[^\\]]*\\]") && cfg.Developers.Contains(message.Author.Id))
            {
                string content = Regex.Match(message.Content, "\\$\\$addtank\\$\\$\\[[^\\]]*\\]").Value;
                if (message.Attachments.Count != 1)
                {
                    await msg3.ReplyAsync("Invalid attachments");
                }
                else
                {
                    chunk ch = new chunk();
                    try
                    {
                        string content2 = message.Content.Replace("$$addtank$$[", "");
                        content2 = content2.Replace("\"}]", "\"}");
                        Console.WriteLine(content2);
                        ch = chunk.Deserialize(content2);
                        tank t = new tank();
                        t.names = ch.names;
                        t.offence = ch.offence;
                        t.defence = ch.defence;
                        t.file = ch.file;
                        index.tanks.Add(ch.tkey, t);
                        string url = message.Attachments.FirstOrDefault().Url;
                        using (WebClient client = new WebClient())
                        {
                            try
                            {
                                string fileName = Path.Join(paths["images"],ch.file);
                                client.DownloadFile(url, fileName);
                                await msg3.AddReactionAsync(new Emoji("✅"));
                            }
                            catch (Exception ex)
                            {
                                await msg3.ReplyAsync($"Couldnt download image\n{ex.Message}");
                            }
                        }
                        File.WriteAllText(paths["index"], index.Serialize(index));
                        await _client.SetGameAsync($"{index.tanks.Count} tanks!", null, ActivityType.Watching);
                    }
                    catch (Exception ex)
                    {
                        await msg3.ReplyAsync($"Invalid JSON\n{ex.Message}");
                    }
                }
            }
            else if (Regex.IsMatch(message.Content, "\\$\\$remtank\\$\\$\\[[^\\]]*\\]") && cfg.Developers.Contains(message.Author.Id))
            {
                string content2 = message.Content.Replace("$$remtank$$[", "");
                content2 = content2.Replace("\"}]", "\"}");
                foreach (KeyValuePair<ulong, Dictionary<tank, uint>> kvp in data._data)
                {
                    data._data[kvp.Key].Remove(index.fromId(content2));
                }
                index.tanks.Remove(content2);
                File.WriteAllText(paths["index"], index.Serialize(index));
                await _client.SetGameAsync($"{index.tanks.Count} tanks!", null, ActivityType.Watching);
                await message.AddReactionAsync(new Emoji("✅"));
            }
            else if (Regex.IsMatch(message.Content, "\\$\\$gettank\\$\\$\\[[^\\]]*\\]") && cfg.Developers.Contains(message.Author.Id))
            {
                string content2 = message.Content.Replace("$$gettank$$[", "");
                content2 = content2.Replace("\"}]", "\"}");
                chunk ch = new chunk();
                if (index.tanks.ContainsKey(content2))
                {
                    tank t = index.fromId(content2);
                    ch.tkey = content2;
                    ch.names = t.names;
                    ch.offence = t.offence;
                    ch.defence = t.defence;
                    ch.file = t.file;

                    await msg2.ReplyAsync(chunk.Serialize2(ch));
                }
                ch.tkey = content2;
                
                await _client.SetGameAsync($"{index.tanks.Count} tanks!", null, ActivityType.Watching);
            }
            else if (Regex.IsMatch(message.Content, "\\$\\$index\\$\\$") && cfg.Developers.Contains(message.Author.Id))
            {
                try
                {
                    await message.AddReactionAsync(new Emoji("⏳"));
                    ZipFile.CreateFromDirectory("tanks", paths["inddest"],CompressionLevel.SmallestSize,false);
                    int highest = 0;
                    foreach (string tkey in index.tanks.Keys)
                    {
                        if ((int)util.ExctractNumberFromId(tkey).response > highest) highest = (int)util.ExctractNumberFromId(tkey).response;
                    }
                    string response = fileUploader.UploadFile(paths["inddest"]).Result;
                    Console.WriteLine(response);
                    msg2.ReplyAsync(response).Wait();
                    /*message.Channel.SendFileAsync(paths["inddest"],
                        $"<@{message.Author.Id}>\n" +
                        $"Highest id: {util.convertNumberToId(highest).response}").Wait();*/

                    File.Delete(paths["inddest"]);
                    await message.AddReactionAsync(new Emoji("✅"));
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }

                
            }
            else if (Regex.IsMatch(message.Content, "\\$\\$setindex\\$\\$") && cfg.Developers.Contains(message.Author.Id))
            {
                try
                {
                    await message.AddReactionAsync(new Emoji("⏳"));
                    if (message.Attachments.Count >0)
                    {
                        using (WebClient client = new WebClient())
                        {
                            try
                            {
                                File.Move(paths["index"], Path.Combine(paths["old"], $"oldIndex{DateTime.Now}.json"));
                                string fileName = Path.Join(paths["index"]);
                                client.DownloadFile(message.Attachments.FirstOrDefault().Url, fileName);
                                index = index.Deserialize(File.ReadAllText(paths["index"]));
                                await msg3.AddReactionAsync(new Emoji("✅"));
                            }
                            catch (Exception ex)
                            {
                                await msg3.ReplyAsync($"Couldnt download image\n{ex.Message}");
                            }
                        }
                    }
                    await message.AddReactionAsync(new Emoji("✅"));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            else if (Regex.IsMatch(message.Content, "\\$\\$settank\\$\\$\\[[^\\]]*\\]") && cfg.Developers.Contains(message.Author.Id))
            {
                try
                {
                    string content = Regex.Match(message.Content, "\\$\\$settank\\$\\$\\[[^\\]]*\\]").Value;

                    chunk ch = new chunk();
                    string content2 = message.Content.Replace("$$settank$$[", "");
                    content2 = content2.Replace("\"}]", "\"}");
                    ch = chunk.Deserialize(content2);
                    tank t = new tank();
                    t.names = ch.names;
                    t.offence = ch.offence;
                    t.defence = ch.defence;
                    t.file = ch.file;
                    if (message.Attachments.Count == 0)
                    {
                        string url = message.Attachments.FirstOrDefault().Url;
                        File.Delete(Path.Combine(paths["images"], index.fromId(ch.tkey).file));
                        using (WebClient client = new WebClient())
                        {
                            try
                            {
                                string fileName = Path.Join(paths["images"], ch.file);
                                client.DownloadFile(url, fileName);
                                t.file = ch.file;
                                await msg3.AddReactionAsync(new Emoji("⬇️"));
                            }
                            catch (Exception ex)
                            {
                                await msg3.ReplyAsync($"Couldnt download image\n{ex.Message}");
                            }
                        }
                    }
                    index.tanks[ch.tkey] = t;
                    File.WriteAllText(paths["index"], index.Serialize(index));
                    await message.AddReactionAsync(new Emoji("✅"));
                }
                catch (Exception ex)
                {
                    await msg3.ReplyAsync($"Idk bro something went wrong figure it out yourself\n{ex.Message}\n{ex.StackTrace}");
                }

            }
            else if (Regex.IsMatch(message.Content, "\\$\\$removecache\\$\\$") && cfg.Developers.Contains(message.Author.Id))
            {
                var chan = _client.GetChannel(message.Channel.Id);
                try
                {
                    if (!message.Reference.FailIfNotExists.Value)
                    {
                        var msg = await message.Channel.GetMessageAsync(message.Reference.MessageId.Value);
                        if (msg.Author.Id == _client.CurrentUser.Id)
                        {
                            int i = 0;
                            foreach (string cacheitem in cache._cache.Values)
                            {
                                if (cacheitem == msg.Embeds.FirstOrDefault().Image.Value.Url)
                                {
                                    cache._cache.Remove(cache._cache.Keys.ToArray()[i]);
                                    break;
                                }
                                i++;
                            }
                            await message.AddReactionAsync(new Emoji("✅"));
                        }
                    }
                }
                catch (Exception ex)
                {
                    await msg3.ReplyAsync($"Idk bro something went wrong figure it out yourself\n{ex.Message}\n{ex.StackTrace}");
                }
            }
            else if (message.CleanContent == "$$fixcoms$$" && cfg.Developers.Contains(message.Author.Id))
            {
                await message.AddReactionAsync(new Emoji("⏳"));
                foreach (var command in guild.GetApplicationCommandsAsync().Result.ToArray())
                {
                    await command.DeleteAsync();
                }

                SocketApplicationCommand[] commands = _client.GetGlobalApplicationCommandsAsync().Result.ToArray();
                foreach (SocketApplicationCommand command in commands)
                {
                    await command.DeleteAsync();
                }
                foreach (KeyValuePair<string,string> kvp in cfg.Commands)
                {
                    var command = new Discord.SlashCommandBuilder();
                    command.IsNsfw = false;
                    command.Name = kvp.Key;
                    command.Description = kvp.Value;
                    await _client.CreateGlobalApplicationCommandAsync(command.Build());
                }
                await message.AddReactionAsync(new Emoji("✅"));
            }
            else if (Regex.IsMatch(message.Content, "\\$\\$zhirik\\$\\$"))
            {
                string[] zhirik = new string[] {
                    "https://tenor.com/view/%D0%B6%D0%B8%D1%80%D0%B8%D0%BA%D0%BF%D0%B0%D0%BB-%D0%BF%D0%B0%D0%BB-%D0%B6%D0%B8%D1%80%D0%B8%D0%BA-%D0%B6%D0%B8%D1%80%D0%B8%D0%BD%D0%BE%D0%B2%D1%81%D0%BA%D0%B8%D0%B9-gif-20787105",
                    "https://tenor.com/view/polacy-gif-23665279",
                    "https://media.discordapp.net/attachments/1201155300439371946/1202001189886246952/image0.gif?ex=661eec57&is=660c7757&hm=a7edc7fa2ea20ff4f224ccdf6e8e76feaeeaa1b1fe1aafea3e79be251346017e&"};
                await msg2.ReplyAsync(zhirik[rnd.Next(0, 2)]);
            }

            gldcfg curcfg = guilds[gldcfg.find(guilds.ToArray(), guild.Id)];
            Console.WriteLine($"Message received {{{msg2.CleanContent}}}");
            try
            {
                if (msg2.ReferencedMessage != null)
                {
                    if (activequestion.Contains(msg2.ReferencedMessage.Id, activequestions.ToArray()) && curcfg.active)
                    {
                        activequestion qst = activequestions[activequestion.Find(message.Reference.MessageId.Value, activequestions.ToArray())];
                        if (util.CheckValidity(
                            msg2.CleanContent, 
                            qst.tank, 
                            index, 
                            qst))
                        {

                            data.add(message.Author.Id, qst.tank);
                            data.write(index);
                            await msg2.ReplyAsync($"<@{message.Author.Id}> guessed \"*{qst.tank.names[0]}*\"!\nYou now own **{data.amt(message.Author.Id, qst.tank)}**, \"*{qst.tank.names[0]}'s*\" ***({qst.tkey})***!");
                            await msg2.AddReactionAsync(new Emoji("✅"));
                            EmbedBuilder oldEmbed = qst.btn.msg.Embeds.FirstOrDefault().ToEmbedBuilder();
                            oldEmbed.WithTitle($"~~A tank has appeared!~~ *{qst.tank.names[0]}* ***Captured***");
                            await qst.btn.msg.ModifyAsync(msg =>
                            {
                                msg.Embed = oldEmbed.Build();
                            });
                            activebuttons.Remove(qst.btn);
                            activequestions.Remove(qst);
                        }
                        else
                        {
                            await msg2.AddReactionAsync(new Emoji("❌"));
                        }
                    } //guess
                    else if (activequery.Contains(msg2.ReferencedMessage.Id, activequeries.ToArray()))
                    {
                        if (Int32.TryParse(msg2.CleanContent, out int number))
                        {
                            int i = activequery.Find(msg2.ReferencedMessage.Id, activequeries.ToArray());
                            activequery acq = activequeries[i];
                            if (number > 0 && number <= util.CalculatePagesNeeded(data.ama((ulong)acq.userid), 25))
                            {
                                activequeries[i].page = number;
                                acq = activequeries[i];
                                EmbedBuilder _eb = new EmbedBuilder();
                                DateTime expiry = DateTime.UtcNow.AddMinutes(1);
                                Int32 unixTimestamp = (int)expiry.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
                                activequery aq = new activequery();
                                _eb.Title = $"Tank info for {message.Author.GlobalName}";
                                _eb.Description = $"Page {acq.page+1}/{util.CalculatePagesNeeded(data.ama(message.Author.Id), 25)}\n{util.CalculateItemsOnPage(data.ama(message.Author.Id), 25, acq.page)}/25 per page" +
                                    $"\nType \"next\" for next page. \"previous\" for previous page, or page number. And tank ID for info\n" +
                                    $"Will timeout <t:{unixTimestamp}:R>";
                                List<string> allItems = new List<string>();
                                foreach (tank t in data._data[message.Author.Id].Keys)
                                {
                                    allItems.Add(index.fromTank(t));
                                }

                                int depth = 0;
                                foreach (string item in util.GetItemsOnPage(allItems.ToArray(), 25, acq.page))
                                {
                                    if (depth == 25) break;
                                    EmbedFieldBuilder efb = new EmbedFieldBuilder();
                                    efb.WithName($"{item} x{data.amt(message.Author.Id, index.fromId(item))}");
                                    efb.WithValue(index.fromId(item).names[0]);
                                    efb.IsInline = true;
                                    _eb.AddField(efb);
                                    depth++;
                                }
                                await acq.msg.ModifyAsync(msg =>
                                {
                                    msg.Embed = _eb.Build();
                                });
                                await message.DeleteAsync();
                            }
                            
                        }
                        else if (index.tanks.ContainsKey(msg2.CleanContent))
                        {
                            if (data.has(message.Author.Id, index.tanks[msg2.CleanContent]))
                            {
                                activequery acq = activequeries[activequery.Find(msg2.ReferencedMessage.Id, activequeries.ToArray())];
                                tank t = index.tanks[msg2.CleanContent];
                                EmbedBuilder eb = new EmbedBuilder();
                                activeinfo ai = new activeinfo();
                                DateTime expiry = DateTime.UtcNow.AddMinutes(1);
                                Int32 unixTimestamp = (int)expiry.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
                                ai.tkey = index.fromTank(t);
                                ai.expirationtime = expiry;
                                ai.msg = acq.msg;
                                eb.ImageUrl = util.cdnget($@"{paths["images2"]}{t.file}", cdnid, cdnchid, _client, ref cache);
                                eb.Title = t.names[0];
                                eb.AddField("Offence", t.offence, true);
                                eb.AddField("Defence", t.defence, true);
                                eb.Description = $"Will timeout <t:{unixTimestamp}:R>\n**Valid names:**";
                                foreach (string name in t.names)
                                {
                                    eb.Description += $"\n*{name}*";
                                }
                                eb.WithFooter($"File name: {t.file}\nID: {msg2.CleanContent}");
                                await acq.msg.ModifyAsync(msg =>
                                {
                                    msg.Embed = eb.Build();
                                });
                                await message.DeleteAsync();
                                activequeries.Remove(acq);
                                activeinfos.Add(ai);
                            }
                            else
                            {
                                await msg2.ReplyAsync("You dont own this tank!");
                            }
                        }
                        else if (msg2.CleanContent.ToLower().Replace(" ", "") == "previous" || msg2.CleanContent.ToLower().Replace(" ", "") == "next" || Int32.TryParse(msg2.CleanContent, out int n))
                        {
                            int i = activequery.Find(msg2.ReferencedMessage.Id, activequeries.ToArray());
                            if (msg2.CleanContent.ToLower().Replace(" ", "") == "previous" && activequeries[i].page > 0)
                            {
                                activequeries[i].page--;
                                activequery acq = activequeries[i];
                                EmbedBuilder _eb = new EmbedBuilder();
                                DateTime expiry = DateTime.UtcNow.AddMinutes(1);
                                Int32 unixTimestamp = (int)expiry.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
                                //Int32 unixTimestamp = (int)expiry.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
                                activequery aq = new activequery();
                                _eb.Title = $"Tank info for {message.Author.GlobalName}";
                                _eb.Description = $"Page {acq.page}/{util.CalculatePagesNeeded(data.ama(message.Author.Id), 25)}\n{util.CalculateItemsOnPage(data.ama(message.Author.Id), 25, acq.page)}/25 per page" +
                                    $"\nType \"next\" for next page. \"previous\" for previous page, or page number. And tank ID for info\n" +
                                    $"Will timeout <t:{unixTimestamp}:R>";
                                List<string> allItems = new List<string>();
                                foreach (tank t in data._data[message.Author.Id].Keys)
                                {
                                    allItems.Add(index.fromTank(t));
                                }
                                
                                int depth = 0;
                                foreach (string item in util.GetItemsOnPage(allItems.ToArray(), 25, acq.page))
                                {
                                    if (depth == 25) break;
                                    EmbedFieldBuilder efb = new EmbedFieldBuilder();
                                    efb.WithName($"{item} x{data.amt(message.Author.Id, index.fromId(item))}");
                                    efb.WithValue(index.fromId(item).names[0]);
                                    efb.IsInline = true;
                                    _eb.AddField(efb);
                                    depth++;
                                }
                                await acq.msg.ModifyAsync(msg =>
                                {
                                    msg.Embed = _eb.Build();
                                });
                            }
                            else if (msg2.CleanContent.ToLower().Replace(" ", "") == "next" && activequeries[i].page < util.CalculatePagesNeeded(data.ama(message.Author.Id),25))
                            {
                                activequeries[i].page++;
                                activequery acq = activequeries[i];
                                EmbedBuilder _eb = new EmbedBuilder();
                                DateTime expiry = DateTime.UtcNow.AddMinutes(1);
                                Int32 unixTimestamp = (int)expiry.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
                                activequery aq = new activequery();
                                _eb.Title = $"Tank info for {message.Author.GlobalName}";
                                _eb.Description = $"Page {acq.page}/{util.CalculatePagesNeeded(data.ama(message.Author.Id), 25)}\n{util.CalculateItemsOnPage(data.ama(message.Author.Id), 25, acq.page)}/25 per page" +
                                    $"\nType \"next\" for next page. \"previous\" for previous page, or page number. And tank ID for info\n" +
                                    $"Will timeout <t:{unixTimestamp}:R>";
                                List<string> allItems = new List<string>();
                                foreach (tank t in data._data[message.Author.Id].Keys)
                                {
                                    allItems.Add(index.fromTank(t));
                                }

                                int depth = 0;
                                foreach (string item in util.GetItemsOnPage(allItems.ToArray(), 25, acq.page))
                                {
                                    if (depth == 25) break;
                                    EmbedFieldBuilder efb = new EmbedFieldBuilder();
                                    efb.WithName($"{item} x{data.amt(message.Author.Id, index.fromId(item))}");
                                    efb.WithValue(index.fromId(item).names[0]);
                                    efb.IsInline = true;
                                    _eb.AddField(efb);
                                    depth++;
                                }
                                await acq.msg.ModifyAsync(msg =>
                                {
                                    msg.Embed = _eb.Build();
                                });
                                await message.DeleteAsync();
                            }
                            else
                            {
                                await msg2.AddReactionAsync(new Emoji("❌"));
                            }
                        }
                        else
                        {
                            await msg2.AddReactionAsync(new Emoji("❌"));
                        }
                    } //                 menu
                    else if (activeinfo.Contains(msg2.ReferencedMessage.Id, activeinfos.ToArray()))
                    {
                        if (Regex.IsMatch(msg2.Content, "<@(\\d+)>"))
                        {
                            string id = Regex.Match(msg2.Content, "<@[0-9]+>").Value.Replace("<@","").Replace(">","");
                            int actinf = activeinfo.Find(msg2.ReferencedMessage.Id, activeinfos.ToArray());
                            string tkey = activeinfos[actinf].tkey;
                            if (ulong.TryParse(id, out ulong u_id))
                            {
                                data.add(u_id, index.fromId(tkey));
                                data.rem(msg2.Author.Id, index.fromId(tkey));
                                await message.Channel.SendMessageAsync($"<@{message.Author.Id}> has given <@{id}> **1** \"*{index.fromId(tkey).names[0]}*\"!");
                                EmbedBuilder old_eb = activeinfos[actinf].msg.Embeds.FirstOrDefault().ToEmbedBuilder();
                                Int32 unixTimestamp = (int)activeinfos[actinf].expirationtime.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
                                old_eb.Description = old_eb.Description.Replace($"Will timeout <t:{unixTimestamp}:R>", "__Expired__");
                                await activeinfos[actinf].msg.ModifyAsync(msg =>
                                {
                                    msg.Embed = old_eb.Build();
                                });
                                activeinfos.Remove(activeinfos[actinf]);
                            }
                            else
                            {
                                Console.WriteLine($"couldnt parse {id}");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"no match on {msg2.Content}");
                        }
                    } //                    info
                    else if (activegiving.Contains(msg2.ReferencedMessage.Id, activegivings.ToArray()))//                    give
                    {
                        string[] bits = msg2.Content.Split(':');
                        if (bits.Length == 2 )
                        {
                            try
                            {
                                ulong id = ulong.Parse(Regex.Match(bits[0], "<@[0-9]+>").Value.Replace("<@", "").Replace(">", ""));
                                int actgiv = activegiving.Find(msg2.ReferencedMessage.Id, activegivings.ToArray());
                                string tkey = bits[1];
                                if (index.tanks.ContainsKey(tkey) && data.has(msg2.Author.Id, index.fromId(tkey)))
                                {
                                    data.add(id, index.fromId(tkey));
                                    data.rem(msg2.Author.Id, index.fromId(tkey));
                                    await message.Channel.SendMessageAsync($"<@{message.Author.Id}> has given <@{id}> **1** \"*{index.fromId(tkey).names[0]}*\"!");
                                    EmbedBuilder old_eb = activegivings[actgiv].msg.Embeds.FirstOrDefault().ToEmbedBuilder();
                                    Int32 unixTimestamp = (int)activegivings[actgiv].expirationtime.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
                                    old_eb.Description = old_eb.Description.Replace($"Expires <t:{unixTimestamp}:R>", "__Expired__");
                                    await activegivings[actgiv].msg.ModifyAsync(msg =>
                                    {
                                        msg.Embed = old_eb.Build();
                                    });
                                    activegivings.Remove(activegivings[actgiv]);
                                }
                            }
                            catch
                            {
                                await msg2.AddReactionAsync(new Emoji("❗"));
                                await msg2.AddReactionAsync(new Emoji("❌"));
                            }
                        }
                        else
                        {
                            await msg2.AddReactionAsync(new Emoji("❌"));
                        }
                    } //                give
                    else
                    {
                        Console.WriteLine($"no matching ref");
                    }
                }
                else
                {
                    Console.WriteLine("no ref");
                }
                data.write(index);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }

            var a = rnd.Next(0, 50);
            if (a == 1 && curcfg.active)
            {
                await Spawn(message.Channel, rnd.Next(0, index.tanks.Count));
            }
            
        }
        private async Task Spawn(ISocketMessageChannel? channel, int randtank)
        {
            Random rnd = new Random(DateTime.Now.Millisecond); 
            ComponentBuilder builder = new ComponentBuilder(); 
            DateTime expiry = DateTime.UtcNow.AddMinutes(2);
            DateTime expiry2 = DateTime.UtcNow.AddMinutes(5);
            Int32 unixTimestamp = (int)expiry.Subtract(new DateTime(1970, 1, 1)).TotalSeconds; 
            IGuild guild = (channel as IGuildChannel)?.Guild; 
            gldcfg cfg = guilds[gldcfg.find(guilds.ToArray(), guild.Id)]; 

            builder.WithButton(
                "Guess", 
                $"{rnd.Next(-10000, 10000)}", 
                ButtonStyle.Success); 


            EmbedBuilder eb = new EmbedBuilder();
            eb.Title = $"**A tank has appeared!** *Expires <t:{unixTimestamp}:R>*"; 
            eb.WithImageUrl(util.cdnget($"{paths["images2"]}{index.tanks[index.tanks.Keys.ToArray()[randtank]].file}", cdnid, cdnchid, _client, ref cache));

            RestUserMessage msg = await channel.SendMessageAsync(embed:eb.Build());

            /*RestUserMessage msg = await channel.SendFileAsync(
                $@"tanks\images\{index.tanks[index.tanks.Keys.ToArray()[randtank]].file}",
                $"**A tank has appeared!** *Expires <t:{unixTimestamp}:R>*");*/

            blocked.Add(new KeyValuePair<ulong, DateTime>(guild.Id, expiry2));
            activebuttons.Add(new activebtn(
                msg,
                channel.Id,
                guild.Id,
                index.tanks.Keys.ToArray()[randtank],
                expiry)); 
            tank t = index.tanks[index.tanks.Keys.ToArray()[randtank]];
            activequestions.Add(
                new activequestion(msg, 
                t, 
                new activebtn(
                    msg,
                    channel.Id,
                    guild.Id,
                    index.tanks.Keys.ToArray()[randtank],
                    expiry),
                index.fromTank(t)));
        }
        private async void OnButtonTimedEvent(Object source, ElapsedEventArgs e)    
        {
            foreach (activequestion qst in activequestions.ToArray())
            { 
                if (DateTime.Compare(qst.btn.expirationtime, DateTime.UtcNow) < 0)
                {
                    EmbedBuilder oldEmbed = qst.btn.msg.Embeds.FirstOrDefault().ToEmbedBuilder();
                    oldEmbed.WithTitle($"~~A tank has appeared!~~ ***Expired***");
                    await qst.btn.msg.ModifyAsync(msg =>
                    {
                        msg.Embed = oldEmbed.Build();
                    });
                    activequestions.Remove(qst);
                    activebuttons.Remove(qst.btn);
                }
            }
            foreach (activequery acq in activequeries.ToArray())
            {
                if (DateTime.Compare(acq.expirationtime, DateTime.UtcNow) < 0)
                {
                    EmbedBuilder oldEmbed = acq.msg.Embeds.FirstOrDefault().ToEmbedBuilder();
                    oldEmbed.WithDescription(
                        $"~~Page {acq.page}/{util.CalculatePagesNeeded(data.ama((ulong)acq.userid), 25)}\n{util.CalculateItemsOnPage(data.ama((ulong)acq.userid), 25, 0)}/25 per page~~" +
                        $"\n~~Type \"next\" for next page. \"previous\" for previous page, or page number. And tank ID for info~~\n" +
                        $"# __Timed Out__");
                    await acq.msg.ModifyAsync(msg =>
                    {
                        msg.Embed = oldEmbed.Build();
                    });
                    activequeries.Remove(acq);
                }
            }
            foreach (activeinfo aci in activeinfos.ToArray())
            {
                if (DateTime.Compare(aci.expirationtime, DateTime.UtcNow) < 0)
                {
                    int actinf = activeinfo.Find(aci.msg.Id, activeinfos.ToArray());
                    EmbedBuilder old_eb = activeinfos[actinf].msg.Embeds.FirstOrDefault().ToEmbedBuilder();
                    Int32 unixTimestamp = (int)activeinfos[actinf].expirationtime.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
                    old_eb.Description = old_eb.Description.Replace($"Will timeout <t:{unixTimestamp}:R>", "__Expired__");
                    await activeinfos[actinf].msg.ModifyAsync(msg =>
                    {
                        msg.Embed = old_eb.Build();
                    });
                }
            }
            foreach (activegiving aci in activegivings.ToArray())
            {
                if (DateTime.Compare(aci.expirationtime, DateTime.UtcNow) < 0)
                {
                    int actinf = activegiving.Find(aci.msg.Id, activegivings.ToArray());
                    EmbedBuilder old_eb = activegivings[actinf].msg.Embeds.FirstOrDefault().ToEmbedBuilder();
                    Int32 unixTimestamp = (int)activegivings[actinf].expirationtime.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
                    old_eb.Description = old_eb.Description.Replace($"Expires <t:{unixTimestamp}:R>", "__Expired__");
                    await activegivings[actinf].msg.ModifyAsync(msg =>
                    {
                        msg.Embed = old_eb.Build();
                    });
                }
            }
            foreach (KeyValuePair<ulong, DateTime> kvp in blocked.ToArray())
            {
                if (DateTime.Compare(kvp.Value, DateTime.UtcNow) < 0)
                {
                    blocked.Remove(kvp);
                }
            }
            GC.Collect();
        }
        private async void SpawnTimerEvent(Object source, ElapsedEventArgs e)
        {
            foreach (SocketGuild gld in _guilds)
            {
                gldcfg cfg = guilds[gldcfg.find(guilds.ToArray(), gld.Id)];
                if (cfg.active)
                {
                    ISocketMessageChannel chn = (ISocketMessageChannel)gld.GetChannel(cfg.channelid);
                    Random rnd = new Random();
                    await Spawn(chn, rnd.Next(0, index.tanks.Count - 1));
                }
            }
        }
        private async void CacheTimerEvent(Object source, ElapsedEventArgs e)
        {
            cache.checkAll();
            File.WriteAllText("cache.json", cache.Serialize());
        }
        private async Task ReactionAdded(Cacheable<IUserMessage, ulong> message, Cacheable<IMessageChannel, ulong> channel, SocketReaction react)
        {
            var _message = await message.GetOrDownloadAsync();
            if (_message == null) return;
            if (react.Emote.Name == new Emoji("🚡").Name)
            {
                try
                {
                    chunk ch = new chunk();
                    string content2 = _message.Content.Replace("$$addtank$$[", "");
                    content2 = content2.Replace("\"}]", "\"}");
                    Console.WriteLine(content2);
                    ch = chunk.Deserialize(content2);
                    tank t = new tank();
                    t.names = ch.names;
                    t.offence = ch.offence;
                    t.defence = ch.defence;
                    t.file = ch.file;
                    index.tanks.Add(ch.tkey, t);
                    string url = _message.Attachments.FirstOrDefault().Url;
                    using (WebClient client = new WebClient())
                    {
                        try
                        {
                            string fileName = Path.Join(paths["images"], ch.file);
                            client.DownloadFile(url, fileName);
                            await _message.AddReactionAsync(new Emoji("✅"));
                        }
                        catch (Exception ex)
                        {
                            await _message.ReplyAsync($"Couldnt download image\n{ex.Message}");
                        }
                    }
                    File.WriteAllText(paths["index"], index.Serialize(index));

                    await _message.AddReactionAsync(new Emoji("👍"));
                }
                catch (Exception ex)
                {
                    await _message.AddReactionAsync(new Emoji("❌"));
                    Console.WriteLine(ex.Message);
                }
            }
            
        }
    }
}