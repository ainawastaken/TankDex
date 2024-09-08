#pragma warning disable

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
using System.Reflection;
using System.Collections.Immutable;

// Perm: 277025778752
// link: https://discord.com/oauth2/authorize?client_id=1182017101897154621&permissions=277025778752&scope=bot+applications.commands

namespace TankDex
{
    internal class Program
    {
        //public static Program Program_;
        public static DiscordSocketClient _client;
        public static CommandService _commands;
        public IServiceProvider _services;

        public static data data = new data();
        public static config cfg = new config();
        public static index index = new index();
        public static cache cache = new cache();
        public static DisconnectLog disconnectLog = new DisconnectLog();

        public ulong cdnid = ulong.MaxValue;
        public ulong cdnchid = ulong.MaxValue;

        public volatile static List<activebtn> activebuttons = new List<activebtn>();
        public volatile static List<activequestion> activequestions = new List<activequestion>();
        public volatile static List<activequery> activequeries = new List<activequery>();
        public volatile static List<activegiving> activegivings = new List<activegiving>();
        public volatile static List<activeinfo> activeinfos = new List<activeinfo>();
        public volatile static List<SocketGuild> _guilds = new List<SocketGuild>();
        public volatile static List<gldcfg> guilds = new List<gldcfg>();
        public volatile static List<KeyValuePair<ulong,DateTime>> blocked = new List<KeyValuePair<ulong, DateTime>>();
        public volatile static List<bindreact> activereactions = new List<bindreact>();

        public volatile static Dictionary<ulong, long> server_leaderboad = new Dictionary<ulong, long>();
        public volatile static Dictionary<ulong, SocketGuild> __guilds = new Dictionary<ulong, SocketGuild>();
        public volatile static Dictionary<ulong, KeyValuePair<SocketGuild, SocketUser>> users = new Dictionary<ulong, KeyValuePair<SocketGuild, SocketUser>>();
        public static Dictionary<string, string> paths = new Dictionary<string, string>();

        private System.Timers.Timer buttonTimer;
        private System.Timers.Timer spawnTimer;
        private System.Timers.Timer cacheTimer;

        private FileUploader fileUploader = new FileUploader();

        public bool isReady = false;

        Random rnd = new Random();

        static void Main(string[] args)
        {
#if DEBUG
            new Program().RunBotAsync().GetAwaiter().GetResult();
            //Program_ = new Program();
            //Program_.RunBotAsync().GetAwaiter().GetResult();
#else
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
#endif
        }
        public async Task RunBotAsync()
        {
            #region paths
            config.load(out cfg);
            if ((bool)cfg.Windows) paths.Add("index", "tanks\\index.json"); else paths.Add("index", "tanks/index.json");
            if ((bool)cfg.Windows) paths.Add("tempdir", "tanks\\temp"); else paths.Add("tempdir", "tanks/temp");
            if ((bool)cfg.Windows) paths.Add("images", "tanks\\images"); else paths.Add("images", "tanks/images");
            if ((bool)cfg.Windows) paths.Add("images2", "tanks\\images\\"); else paths.Add("images2", "tanks/images/");
            if ((bool)cfg.Windows) paths.Add("images3", "images\\"); else paths.Add("images3", "images/");
            if ((bool)cfg.Windows) paths.Add("inddest", $"{paths["tempdir"]}\\index.zip"); else paths.Add("inddest", $"{paths["tempdir"]}/index.zip");
            if ((bool)cfg.Windows) paths.Add("old", $"tanks\\old"); else paths.Add("old", $"tanks/old");
            if ((bool)cfg.Windows) paths.Add("crashlog", $"crashlog.txt"); else paths.Add("crashlog", $"crashlog.txt");
            if ((bool)cfg.Windows) paths.Add("errorlog", $"errorlog.txt"); else paths.Add("errorlog", $"errorlog.txt");
            if ((bool)cfg.Windows) paths.Add("disconnectlog", $"disconnectlog.json"); else paths.Add("disconnectlog", $"disconnectlog.json");
            if ((bool)cfg.Windows) paths.Add("disconnectsha", $"sha\\disconnectlogsha256.txt"); else paths.Add("disconnectsha", $"sha/disconnectlogsha256.txt");
            if ((bool)cfg.Windows) paths.Add("stacktemp", $"tanks\\temp\\stack.txt"); else paths.Add("stacktemp", $"tanks/temp/stack.txt");
            if ((bool)cfg.Windows) paths.Add("infotemp", $"tanks\\temp\\info.txt"); else paths.Add("infotemp", $"tanks/temp/info.txt");
            #endregion
            #region directory setup
            Directory.CreateDirectory("sha");
            Directory.CreateDirectory(paths["tempdir"]);
            Directory.CreateDirectory(paths["old"]);
            DirectoryInfo di = new DirectoryInfo(paths["tempdir"]);
            foreach (FileInfo file in di.GetFiles()) file.Delete();
            if (!File.Exists(paths["errorlog"])) File.Create(paths["errorlog"]).Dispose();
            if (!File.Exists(paths["crashlog"])) File.Create(paths["crashlog"]).Dispose();
            if (!File.Exists(paths["disconnectlog"])) File.Create(paths["disconnectlog"]).Dispose();
            if (!File.Exists(paths["disconnectsha"])) File.Create(paths["disconnectsha"]).Dispose();
            if (!File.Exists("changelog.txt")) File.Create("changelog.txt").Dispose();
            #endregion
            #region config and data
            cache.Deserialize(File.ReadAllText("cache.json"));
            disconnectLog.Load(paths["disconnectlog"]);
            index = index.Deserialize(File.ReadAllText(paths["index"]));
            guilds = gldcfg.load().ToList();
            data.load(index);
            cache = cache.Deserialize(File.ReadAllText("cache.json"));
            string[] cdnstr = File.ReadAllText("cdn.txt").Split(';');
            cdnid = ulong.Parse(cdnstr[0]);
            cdnchid = ulong.Parse(cdnstr[1]);
            #endregion
            #region bot setup
            _client = new DiscordSocketClient(
            new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.All | GatewayIntents.Guilds | GatewayIntents.GuildMembers | GatewayIntents.GuildMessages,
                UseInteractionSnowflakeDate = false,
                LogLevel = LogSeverity.Info,
                AlwaysDownloadUsers = true,
                AlwaysDownloadDefaultStickers = true,
                AlwaysResolveStickers = true,
                AuditLogCacheSize = 4096,
            });
            _commands = new CommandService();
            _client.Log += Log;
            _client.Ready += Client_Ready;
            _client.SlashCommandExecuted += SlashCommandHandler;
            _client.GuildAvailable += GuildAvailable;
            _client.MessageReceived += MessageReceived;
            _client.ReactionAdded += ReactionAdded;
            _client.Disconnected += Disconnected;
            _client.Connected += Connected;


            #endregion
            #region timer setup
            buttonTimer = new System.Timers.Timer(500);
            buttonTimer.Elapsed += OnButtonTimedEvent;
            buttonTimer.AutoReset = true;
            buttonTimer.Enabled = true;
            
            spawnTimer = new System.Timers.Timer(25*60000);
            spawnTimer.Elapsed += SpawnTimerEvent;
            spawnTimer.AutoReset = true;
            spawnTimer.Enabled = true;

            cacheTimer = new System.Timers.Timer(10000);
            cacheTimer.Elapsed += CacheTimerEvent;
            cacheTimer.AutoReset = true;
            cacheTimer.Enabled = true;
            #endregion
            #region devmode
            if ((bool)cfg.DevMode)
            {
                _client.LoginAsync(TokenType.Bot, cfg.DevToken).Wait();
            }
            else
            {
                _client.LoginAsync(TokenType.Bot, cfg.Token).Wait();
            }
            #endregion
            _client.StartAsync().Wait();
            await Task.Delay(-1);
        }
        private Task Log(LogMessage arg)
        {
            Console.WriteLine(arg);
            return Task.CompletedTask;
        }
        private async Task DownloadUsersAsync(SocketGuild guild)
        {
            await guild.DownloadUsersAsync();
        }
        public async Task Client_Ready()
        {
            await _client.SetGameAsync($"{index.tanks.Count} tanks!", null, ActivityType.Watching);
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
            foreach (var guild in _client.Guilds)
            {
                Console.WriteLine(guild.Name + guild.MemberCount);
                await DownloadUsersAsync(guild);
                __guilds.Add(guild.Id, guild);
                server_leaderboad.Add(guild.Id, 0u);
                foreach (var user in guild.Users)
                {
                    server_leaderboad[guild.Id] += data.pow(user.Id) + data.def(user.Id);
                    try { users.Add(user.Id, new KeyValuePair<SocketGuild, SocketUser>(guild, user)); } catch { continue; }
                    data.cch(user.Id);
                }
            }
            data.write(index);
            isReady = true;
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
            if (guild.Id == cdnid)
            {
                RegexOptions options = RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline;
                Match[] crashlog = Regex.Matches(File.ReadAllText(paths["crashlog"]), @"\[\d+\].*?-{30,}", options).ToArray<Match>();
                string mentions = "";
                foreach (ulong id in cfg.Developers)
                {
                    mentions += $"<@{id}>\n";
                }
                Regex squareContents = new Regex("\\[[^\\]]*\\]", RegexOptions.IgnoreCase);
                Dictionary<Int32, string> crashes = new Dictionary<int, string>();
                foreach (Match match in crashlog)
                {
                    string smatch = match.Value;
                    Int32 unixTime = Int32.Parse(squareContents.Match(smatch).Value.Replace("[","").Replace("]",""));
                    crashes.Add(unixTime, smatch);
                }
                if (crashes.Count != 0)
                {
                    KeyValuePair<Int32, string> latest = new KeyValuePair<Int32, string>( 0, "" );
                    foreach (Int32 time in crashes.Keys)
                    {
                        if (time > latest.Key) latest = new KeyValuePair<Int32, string>(time, crashes[time]);
                    }
                    mentions += $"# Latest crash at <t:{latest.Key}:F>\n```{latest.Value}```";

                    var ch = guild.GetTextChannel(cdnchid);
                    await ch.SendMessageAsync("Bot started");
                    RestUserMessage msg = await ch.SendMessageAsync(mentions);
                    msg.AddReactionAsync(new Emoji("🗑️")).Wait();
                    ReactionMetadata rct = msg.Reactions.FirstOrDefault().Value;
                    bindreact br = new bindreact(msg, rct, new Emoji("🗑️"), DateTime.MinValue, cfg.Developers.ToArray());
                    br.func += Reactions.CrashLogDeleteHandler;
                    activereactions.Add(br);

                    mentions = "";
                    foreach (ulong id in cfg.Developers)
                    {
                        mentions += $"<@{id}>\n";
                    }
                    string fileHash = util.CalculateSHA256(File.ReadAllText(paths["disconnectlog"]));
                    string compHash = File.ReadAllText(paths["disconnectsha"]);
                    if (fileHash != compHash)
                    {
                        await ch.SendMessageAsync($"{mentions}\nDisconnect detected!");
                        File.WriteAllText(paths["disconnectsha"], fileHash);
                    }
                }
            }
        }
        private async Task SlashCommandHandler(SocketSlashCommand command)
        {
            SocketGuild guild = _client.GetGuild((ulong)command.GuildId);
            switch (command.Data.Name)
            {
                case "activate":
                    if (util.isAdmin(_client.GetGuild((ulong)command.GuildId).GetUser(command.User.Id)) || cfg.Developers.Contains(command.User.Id))
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
                case "info":
                    if (!data._data.ContainsKey(command.User.Id))
                    {
                        command.RespondAsync($"You dont own any tanks yet <@{command.User.Id}>!");
                        break;
                    }
                    EmbedBuilder _eb = new EmbedBuilder();
                    DateTime expiry = DateTime.UtcNow.AddMinutes(1);
                    Int32 unixTimestamp = (int)expiry.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
                    activequery aq = new activequery();
                    _eb.Title = $"Tank info for {command.User.GlobalName}";
                    _eb.Description = $"Page {aq.page + 1}/{util.CalculatePagesNeeded(data.ama(command.User.Id), 25)}\n{util.CalculateItemsOnPage(data.ama(command.User.Id), 25, 0)}/25 per page" +
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

                    response.AddReactionAsync(new Emoji("⏪")).Wait();
                    response.AddReactionAsync(new Emoji("⏩")).Wait();

                    bindreact br = new bindreact(response as RestUserMessage, new KeyValuePair<Emoji, ReactionMetadata?>[] 
                    {
                        new KeyValuePair<Emoji, ReactionMetadata?>(new Emoji("⏪"), null), 
                        new KeyValuePair<Emoji, ReactionMetadata?>(new Emoji("⏩"), null)
                    }, DateTime.UtcNow.AddMinutes(1), new ulong[] { command.User.Id });
                    br.func += Reactions.MenuReactHandler;

                    aq.msg = (RestUserMessage)response;

                    activereactions.Add(br);
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
                case "invite":
                    if ((bool)cfg.DevMode)
                    {
                        await command.RespondAsync(cfg.DevLink);
                    }
                    else
                    {
                        await command.RespondAsync(cfg.Link);
                    }
                    break;
                case "help":
                    string coms = "";
                    foreach (KeyValuePair<string, string> kvp in cfg.Commands)
                    {
                        coms += $"``{kvp.Key}``: *{kvp.Value}*\n";
                    }
                    await command.RespondAsync(coms);
                    break;
                case "leaderboard":
                    Dictionary<ulong, ulong> leaderboard = new Dictionary<ulong, ulong>();
                    string message = $"# Leaderboard for *{guild.Name}*:\n";
                    foreach (KeyValuePair<ulong, Dictionary<tank, uint>> user in data._data)
                    {
                        if (guild.GetUser(user.Key) != null)
                        {
                            leaderboard.Add(user.Key, data.pow(user.Key) + data.def(user.Key));
                        }
                    }
                    //var sortedDict = leaderboard.OrderByDescending(entry => entry.Value).ToDictionary(entry => entry.Key, entry => entry.Value);
                    var top15Dict = leaderboard.OrderByDescending(entry => entry.Value)
                              .ToDictionary(entry => entry.Key, entry => entry.Value).Take(15);
                    var ind = 0;
                    ulong lastScore = 0;
                    foreach(var entry in top15Dict)
                    {
                        var user = guild.GetUser(entry.Key);
                        if (user.IsBot) continue;
                        if (ind == 0) 
                        { 
                            message += $"{ind}. `{user.DisplayName}`  Power: **{entry.Value}**\n"; 
                        }
                        else
                        {
                            message += $"{ind}. `{user.DisplayName}`  Power: **{entry.Value}** Gap: **{lastScore - entry.Value}**\n";
                        }
                        ind++;
                        lastScore = entry.Value;
                    }
                    await command.RespondAsync(message);
                    break;
                case "globalleaderboard":
                    Dictionary<ulong, long> g_leaderboard = new Dictionary<ulong, long>();
                    string g_message = $"# Global leaderboard:\n";
                    foreach (KeyValuePair<ulong, Dictionary<tank, uint>> user in data._data)
                    {
                        g_leaderboard.Add(user.Key, data.pow(user.Key) + data.def(user.Key));
                    }
                    //var sortedDict = leaderboard.OrderByDescending(entry => entry.Value).ToDictionary(entry => entry.Key, entry => entry.Value);
                    var g_top15Dict = g_leaderboard.OrderByDescending(entry => entry.Value)
                              .ToDictionary(entry => entry.Key, entry => entry.Value).Take(15);
                    var g_ind = 0;
                    long g_lastScore = 0;
                    foreach (var entry in g_top15Dict)
                    {
                        if (g_ind == 0)
                        {
                            try
                            {
                                g_message += $"{g_ind}. `{users[entry.Key].Value.Username}`  Power: **{entry.Value}**\n";
                            }
                            catch
                            {
                                continue;
                            }
                        }
                        else
                        {
                            try
                            {
                                g_message += $"{g_ind}. `{users[entry.Key].Value.Username}`  Power: **{entry.Value}** Gap: **{g_lastScore - entry.Value}**\n";
                            }
                            catch
                            {
                                continue;
                            }
                        }
                        g_ind++;
                        g_lastScore = entry.Value;
                    }
                    await command.RespondAsync(g_message);
                    break;
                case "serverleaderboard":
                    string s_message = $"# Server leaderboard:\n";
                    //var sortedDict = leaderboard.OrderByDescending(entry => entry.Value).ToDictionary(entry => entry.Key, entry => entry.Value);
                    var s_top15Dict = server_leaderboad.OrderByDescending(entry => entry.Value)
                              .ToDictionary(entry => entry.Key, entry => entry.Value)
                              .Take(15);
                    var s_ind = 0;
                    long s_lastScore = 0;
                    foreach (var entry in s_top15Dict)
                    {
                        if (s_ind == 0)
                        {
                            s_message += $"{s_ind}. `{__guilds[entry.Key].Name}`  Power: **{entry.Value}**\n";
                        }
                        else
                        {
                            s_message += $"{s_ind}. `{__guilds[entry.Key].Name}`  Power: **{entry.Value}** Gap: **{s_lastScore - entry.Value}**\n";
                        }
                        s_ind++;
                        s_lastScore = entry.Value;
                    }
                    await command.RespondAsync(s_message);
                    break;
            }
            data.write(index);
        }
        private async Task MessageReceived(SocketMessage message)
        {
            if (message is not IUserMessage userMessage || message.Author.IsBot)
                return;

            IGuild guild = (userMessage.Channel as IGuildChannel)?.Guild;

            var msg2 = message as IUserMessage;
            var msg3 = message as SocketUserMessage;

            #region dev commands
            if      (Regex.IsMatch(message.Content, "\\$\\$loadcoms\\$\\$") && cfg.Developers.Contains(message.Author.Id))
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
                                util.logError(ex, paths["errorlog"]);
                            }
                        }
                        File.WriteAllText(paths["index"], index.Serialize(index));
                        await _client.SetGameAsync($"{index.tanks.Count} tanks!", null, ActivityType.Watching);
                    }
                    catch (Exception ex)
                    {
                        await msg3.ReplyAsync($"Invalid JSON\n{ex.Message}");
                        util.logError(ex, paths["errorlog"]);
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
                    File.Delete(paths["inddest"]);
                    using (var zip = ZipFile.Open(paths["inddest"], ZipArchiveMode.Create))
                    {
                        //zip.CreateEntry("tanks", CompressionLevel.SmallestSize);
                        zip.CreateEntryFromFile(paths["index"], "index.json", CompressionLevel.SmallestSize);
                        foreach (string fi in Directory.GetFiles(paths["images"]))
                        {
                            zip.CreateEntryFromFile(paths["images2"] + Path.GetFileName(fi), paths["images3"] + Path.GetFileName(fi), CompressionLevel.SmallestSize);
                        }
                    }
                    int highest = 0;
                    foreach (string tkey in index.tanks.Keys)
                    {
                        if ((int)util.ExctractNumberFromId(tkey).response > highest) highest = (int)util.ExctractNumberFromId(tkey).response;
                    }
                    string response = fileUploader.UploadFile(paths["inddest"]).Result;
                    ApiResponse uresponse = JsonConvert.DeserializeObject<ApiResponse>(response);
                    Console.WriteLine(uresponse.Data.Url);
                    DateTime expiry = DateTime.UtcNow.AddMinutes(60);
                    Int32 unixTimestamp = (int)expiry.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
                    msg2.ReplyAsync($"Link to index file: {uresponse.Data.Url.Replace(".org/",".org/dl/")}\nLink will expire <t:{unixTimestamp}:R>\nRemember not to use this command too often :)").Wait();
                    File.Delete(paths["inddest"]);
                    await message.AddReactionAsync(new Emoji("✅"));
                }
                catch (Exception ex)
                {
                    util.logError(ex, paths["errorlog"]);
                    Console.WriteLine(ex);
                }
            }
            else if (Regex.IsMatch(message.Content, "\\$\\$setindex\\$\\$") && cfg.Developers.Contains(message.Author.Id))
            {
                try
                {
                    await message.AddReactionAsync(new Emoji("⏳"));
                    if (message.Attachments.Count > 0)
                    {
                        using (WebClient client = new WebClient())
                        {
                            try
                            {
                                Int32 unixTimestamp = (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
                                //File.Create(Path.Combine(paths["old"], $"oldIndex{unixTimestamp}.json")).Dispose();
;                               File.Move(paths["index"], Path.Combine(paths["old"], $"oldIndex{unixTimestamp}.json"));
                                string fileName = Path.Join(paths["index"]);
                                client.DownloadFile(message.Attachments.FirstOrDefault().Url, fileName);
                                index = index.Deserialize(File.ReadAllText(paths["index"]));
                                await msg3.AddReactionAsync(new Emoji("✅"));
                            }
                            catch (Exception ex)
                            {
                                util.logError(ex, paths["errorlog"]);
                                await msg3.ReplyAsync($"Couldnt download file\n{ex.Message}");
                            }
                        }
                    }
                    await message.AddReactionAsync(new Emoji("✅"));
                }
                catch (Exception ex)
                {
                    util.logError(ex, paths["errorlog"]);
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
                                util.logError(ex, paths["errorlog"]);
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
                    util.logError(ex, paths["errorlog"]);
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
                    util.logError(ex, paths["errorlog"]);
                    await msg3.ReplyAsync($"Idk bro something went wrong figure it out yourself\n{ex.Message}\n{ex.StackTrace}");
                }
            }
            else if (Regex.IsMatch(message.Content, "\\$\\$fixcoms\\$\\$") && cfg.Developers.Contains(message.Author.Id))
            {
                await message.AddReactionAsync(new Emoji("⏳"));
                foreach (var command in guild.GetApplicationCommandsAsync().Result.ToArray())
                {
                    await command.DeleteAsync();
                }

                SocketApplicationCommand[] commands = _client.GetGlobalApplicationCommandsAsync().Result.ToArray();
                foreach (SocketApplicationCommand command in commands)
                {
                    command.DeleteAsync().Wait();
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
                await msg2.ReplyAsync(zhirik[rnd.Next(0, zhirik.Length)]);
            }
            else if (Regex.IsMatch(message.Content, "\\$\\$ping\\$\\$"))
            {
                await msg2.ReplyAsync("Pong");
            }
            else if (Regex.IsMatch(message.Content, "\\$\\$crashlog\\$\\$") && cfg.Developers.Contains(message.Author.Id))
            {
                await msg3.Channel.SendFileAsync(paths["crashlog"], $"<@{message.Author.Id}>");
            }
            else if (Regex.IsMatch(message.Content, "\\$\\$errorlog\\$\\$") && cfg.Developers.Contains(message.Author.Id))
            {
                await msg3.Channel.SendFileAsync(paths["crashlog"], $"<@{message.Author.Id}>");
            }
            else if (Regex.IsMatch(message.Content, "\\$\\$lastindex\\$\\$") && cfg.Developers.Contains(message.Author.Id))
            {
                int highest = 0;
                foreach (string tkey in index.tanks.Keys)
                {
                    if ((int)util.ExctractNumberFromId(tkey).response > highest) highest = (int)util.ExctractNumberFromId(tkey).response;
                }
                await msg2.ReplyAsync($"Highest ID was: {util.convertNumberToId(highest).response}");
            }
            else if (Regex.IsMatch(message.Content, "\\$\\$testreact\\$\\$") && cfg.Developers.Contains(message.Author.Id))
            {
                DateTime expiry = DateTime.UtcNow.AddSeconds(30);
                Int32 unixTimestamp = (int)expiry.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
                Int32 unixTimestamp2 = (int)new DateTime(1970, 1, 1).Subtract(DateTime.Now).TotalSeconds;
                var msg = await msg2.ReplyAsync("Testing bindable reactions.\n" +
                    $"{new Emoji("✅")} will expire <t:{unixTimestamp}:R>\n" +
                    $"{new Emoji("❌")} will not expire (<t:{unixTimestamp2}:R>)");

                await msg.AddReactionsAsync(new Emoji[] { new Emoji("✅"), new Emoji("❌") });
                KeyValuePair<Emoji, ReactionMetadata?>[] emojis = new KeyValuePair<Emoji, ReactionMetadata?>[]
                {
                    new KeyValuePair<Emoji, ReactionMetadata?>(new Emoji("✅"), null),
                    new KeyValuePair<Emoji, ReactionMetadata?>(new Emoji("❌"), null)
                };
                var bi1 = new bindreact(msg as RestUserMessage, emojis[0], expiry, new ulong[] { message.Author.Id });
                var bi2 = new bindreact(msg as RestUserMessage, emojis[1], DateTime.MinValue, new ulong[] { message.Author.Id });
                bi1.func += Reactions.CrashLogDeleteHandler;
                bi2.func += Reactions.CrashLogDeleteHandler;
                activereactions.Add(bi1);
                activereactions.Add(bi2);
            }
            else if (Regex.IsMatch(message.Content, "\\$\\$crash\\$\\$") && cfg.Developers.Contains(message.Author.Id))
            {
                try
                {
                    int numerator = 0;
                    int denominator = 0;
                    int result = numerator / denominator;
                }
                catch (Exception ex)
                {
                    using (StreamWriter writer = new StreamWriter(paths["crashlog"], true))
                    {
                        DateTime now = DateTime.UtcNow;
                        Int32 unixTimestamp = (int)now.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
                        writer.WriteLine($"[{unixTimestamp}]");
                        writer.WriteLine($"Exception occurred at {DateTime.Now}");
                        writer.WriteLine($"   Message: {ex.Message}");
                        writer.WriteLine($"   Stack Trace:");
                        writer.WriteLine($"   {ex.StackTrace}");
                        writer.WriteLine(new string('-', 30));
                    }
                }
            }
            else if (Regex.IsMatch(message.Content, "\\$\\$disconnects\\$\\$") && cfg.Developers.Contains(message.Author.Id))
            {
                await message.Channel.SendFileAsync(paths["disconnectlog"]);
            }
            else if (Regex.IsMatch(message.Content, "\\$\\$uploadpic\\$\\$\\[[^\\]]*\\]") && cfg.Developers.Contains(message.Author.Id))
            {
                string content2 = message.Content.Replace("$$uploadpic$$[", "");
                content2 = content2.Replace("]", "");
                string url = message.Attachments.FirstOrDefault().Url;
                using (WebClient client = new WebClient())
                {
                    try
                    {
                        string fileName = Path.Join(paths["images"], $"{content2}{Path.GetExtension(util.GetFileNameFromUrl(url))}");
                        client.DownloadFile(url, fileName);
                        await msg3.AddReactionAsync(new Emoji("✅"));
                    }
                    catch (Exception ex)
                    {
                        await msg3.ReplyAsync($"Couldnt download image\n{ex.Message}");
                        util.logError(ex, paths["errorlog"]);
                    }
                }
            }
            else if (Regex.IsMatch(message.Content, "\\$\\$uploadpic\\$\\$") && cfg.Developers.Contains(message.Author.Id))
            {
                string url = message.Attachments.FirstOrDefault().Url;
                using (WebClient client = new WebClient())
                {
                    try
                    {
                        string fileName = Path.Join(paths["images"], util.GetFileNameFromUrl(url));
                        client.DownloadFile(url, fileName);
                        await msg3.AddReactionAsync(new Emoji("✅"));
                    }
                    catch (Exception ex)
                    {
                        await msg3.ReplyAsync($"Couldnt download image\n{ex.Message}");
                        util.logError(ex, paths["errorlog"]);
                    }
                }
            }
            else if (Regex.IsMatch(message.Content, "\\$\\$info\\$\\$"))
            {
                try
                {
                    await msg2.ReplyAsync("```\n" +
                    $"VERSION: {cfg.VERSION} | {Environment.Version}\n" +
                    $"OS: VER|{Environment.OSVersion} ISx86|{Environment.Is64BitOperatingSystem} PAGE|{Environment.SystemPageSize}\n" +
                    $"Process: ID|{Environment.ProcessId} PATH|{Environment.ProcessPath} ISx86|{Environment.Is64BitProcess}\n" +
                    $"Environment: AvailableCPU|{Environment.ProcessorCount} ORG|{Environment.MachineName} WS|{Environment.WorkingSet}\n" +
                    $"Uptime:{Environment.TickCount64 / 3600000}HRS" +
                    "\n```");
                }
                catch
                {
                    File.WriteAllText(paths["infotemp"], 
                        "```\n" +
                        $"VERSION: {cfg.VERSION} | {Environment.Version}\n" +
                        $"OS: VER|{Environment.OSVersion} ISx86|{Environment.Is64BitOperatingSystem} PAGE|{Environment.SystemPageSize}\n" +
                        $"Process: ID|{Environment.ProcessId} PATH|{Environment.ProcessPath} ISx86|{Environment.Is64BitProcess}\n" +
                        $"Environment: AvailableCPU|{Environment.ProcessorCount} ORG|{Environment.MachineName} WS|{Environment.WorkingSet}\n" +
                        $"Uptime: {Environment.TickCount64/3600000}HRS" +
                        "\n```");
                    await message.Channel.SendFileAsync(paths["infotemp"], $"<@{message.Author.Id}>");
                }
            }
            else if (Regex.IsMatch(message.Content, "\\$\\$stack\\$\\$"))
            {
                try
                {
                    await msg2.ReplyAsync($"```{Environment.StackTrace}```");
                }
                catch
                {
                    File.WriteAllText(paths["stacktemp"], $"```{Environment.StackTrace}```");
                    await message.Channel.SendFileAsync(paths["stacktemp"], $"<@{message.Author.Id}>");
                }
            }
            else if (Regex.IsMatch(message.Content, "\\$\\$setcdn\\$\\$") && cfg.Developers.Contains(message.Author.Id))
            {
                File.WriteAllText("cdn.txt", $"{guild.Id};{msg3.Channel.Id}");
                cdnid = (ulong)guild.Id;
                cdnchid = (ulong)msg3.Channel.Id;
                await msg3.ReplyAsync("CDN set!");
            }
            else if (Regex.IsMatch(message.Content, "\\$\\$trigger\\$\\$") && cfg.Developers.Contains(message.Author.Id))
            {
                await message.DeleteAsync();
                await Spawn(message.Channel, rnd.Next(0, index.tanks.Count));
            }
            else if (Regex.IsMatch(message.Content, "\\$\\$getcfg\\$\\$"))
            {
                int gldi2 = gldcfg.find(guilds.ToArray(), guild.Id);
                await msg2.ReplyAsync($"Config for this guild: **\"{guilds[gldi2]}\"**");
            }
            else if (Regex.IsMatch(message.Content, "\\$\\$servers\\$\\$"))
            {
                string m = "# Current servers\n";
                int i = 0;
                foreach (var g in __guilds)
                {
                    m += $"**{i}** `{g.Value.Name}` *Member count: `{g.Value.MemberCount}`*\n";
                    i++;
                }
                msg2.ReplyAsync(m);
            }
            else if (Regex.IsMatch(message.Content, "\\$\\$sendchangelog\\$\\$") && cfg.Developers.Contains(message.Author.Id))
            {
                string m = File.ReadAllText("changelog.txt");
                foreach (var gld in __guilds)
                {
                    gldcfg cf = guilds[gldcfg.find(guilds.ToArray(), gld.Key)];
                    if (cf.active)
                    {
                        SocketTextChannel c = gld.Value.GetTextChannel(cf.channelid);
                        string final_m = $"# TankDex update {cfg.VERSION} : {Environment.Version} released!\n" + m;
                        c.SendMessageAsync(final_m);
                    }
                }
            }
            else if (Regex.IsMatch(message.Content, "\\$\\$setchangelog\\$\\$\\[[^\\]]*\\]") && cfg.Developers.Contains(message.Author.Id))
            {
                string content2 = message.Content.Replace("$$setchangelog$$[", "");
                content2 = content2.Substring(0, content2.Length - 2);
                File.WriteAllText("changelog.txt", content2);
                message.AddReactionAsync(new Emoji("✅"));
            }
            else if (Regex.IsMatch(message.Content, "\\$\\$testchangelog\\$\\$"))
            {
                string m = File.ReadAllText("changelog.txt");
                string final_m = $"# TankDex update {cfg.VERSION} : {Environment.Version} released!\n" + m;
                msg2.ReplyAsync(final_m);
            }
            else if (Regex.IsMatch(message.Content, "\\$\\$devhelp\\$\\$"))
            {
                await msg2.ReplyAsync(
                    "``loadcoms``: Deprecated command that adds guild commands\n" +
                    "``addtank[]``: Adds tank made with the Tank Adder program. Requires picture attachment and valid chunk JSON within the square brackets\n" +
                    "``remtank[]``: Removes a tank. Put the ID/tkey of the tank in the square brackets\n" +
                    "``gettank[]``: Gets the chunk and image of the tank put in the sqaure brackets\n" +
                    "``index``: Gets a download link the the full index. Including pictures. Dont run this command too often\n" +
                    "``setindex``: Sets the index file. It is reccomended to not use this command as it requires that all of the pictures exist. And it is a little buggy\n" +
                    "``settank[]``: Sets the data of a tank using a chunk\n" +
                    "``removecache``: Removes the cache of a tank dex embed. Reply to an embed with a picture using this command\n" +
                    "``fixcoms``: Fixes the global commands and makes sure they are up to date\n" +
                    "``zhirik&ping``: Both work as a sort of ping command to make sure the bot is working. Doesnt require dev access\n" +
                    "``crashlog``: Replies with the crashlog file\n" +
                    "``errorlog``: Replies with the errorlog file\n" +
                    "``lastindex``: Replies with the last index of the index file. Useful for adding tank\n" +
                    "``testreact``: Used for testing bindeable reactions\n" +
                    "``crash``: Crashes TankDex, used for testing the crashlog\n" +
                    "``disconnects``: Replies with disconnect log\n" +
                    "``uploadpic[]``: Uploads picture to the image folder. Using the filename in the brackets. (dont write extension)\n" +
                    "``uploadpic``: Same as the last one. Except it just uses the name of the image\n" +
                    "``info``: Environment and application info. Doesnt require dev access\n" +
                    "``stack``: Current stack trace info. Doesnt require dev access\n" +
                    "``setcdn``: Sets the content delivery channel\n" +
                    "``trigger``: Triggers a spawn\n" +
                    "``getcfg``: Gets the current guild config\n" +
                    "``servers``: Displays list of servers\n" +
                    "``sendchangelog``: Sends changelog to all servers (do not fucking use unless needed)\n" +
                    "``setchangelog[]``: Sets the changelog\n" +
                    "``testchangelog``: Tests the changelog\n" +
                    "``devhelp``: Displays this list of commands. Doesnt require dev access");
            }
            #endregion

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
                        int i = activequery.Find(msg2.ReferencedMessage.Id, activequeries.ToArray());
                        activequery acq = activequeries[i];
                        if (Int32.TryParse(msg2.CleanContent, out int number) && acq.IsOwner(message.Author.Id))
                        {
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
                        else if (index.tanks.ContainsKey(msg2.CleanContent) && acq.IsOwner(message.Author.Id))
                        {
                            if (data.has(message.Author.Id, index.tanks[msg2.CleanContent]))
                            {
                                tank t = index.tanks[msg2.CleanContent];
                                EmbedBuilder eb = new EmbedBuilder();
                                activeinfo ai = new activeinfo();
                                DateTime expiry = DateTime.UtcNow.AddMinutes(1);
                                Int32 unixTimestamp = (int)expiry.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
                                ai.tkey = index.fromTank(t);
                                ai.expirationtime = expiry;
                                ai.msg = acq.msg;
                                ai.user = message.Author.Id;
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
                                await acq.msg.RemoveAllReactionsAsync();
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
                        else if (msg2.CleanContent.ToLower().Replace(" ", "") == "previous" || 
                                msg2.CleanContent.ToLower().Replace(" ", "") == "next" || 
                                Int32.TryParse(msg2.CleanContent, out int n) && acq.IsOwner(message.Author.Id))
                        {
                            if (msg2.CleanContent.ToLower().Replace(" ", "") == "previous" && activequeries[i].page > 0)
                            {
                                acq.page--;
                                EmbedBuilder _eb = new EmbedBuilder();
                                DateTime expiry = DateTime.UtcNow.AddMinutes(1);
                                Int32 unixTimestamp = (int)expiry.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
                                //Int32 unixTimestamp = (int)expiry.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
                                activequery aq = new activequery();
                                _eb.Title = $"Tank info for {message.Author.GlobalName}";
                                _eb.Description = $"Page {acq.page + 1}/{util.CalculatePagesNeeded(data.ama(message.Author.Id), 25)}\n{util.CalculateItemsOnPage(data.ama(message.Author.Id), 25, acq.page)}/25 per page" +
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
                                acq.page++;
                                EmbedBuilder _eb = new EmbedBuilder();
                                DateTime expiry = DateTime.UtcNow.AddMinutes(1);
                                Int32 unixTimestamp = (int)expiry.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
                                activequery aq = new activequery();
                                _eb.Title = $"Tank info for {message.Author.GlobalName}";
                                _eb.Description = $"Page {acq.page + 1}/{util.CalculatePagesNeeded(data.ama(message.Author.Id), 25)}\n{util.CalculateItemsOnPage(data.ama(message.Author.Id), 25, acq.page)}/25 per page" +
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
                            else
                            {
                                await msg2.AddReactionAsync(new Emoji("❌"));
                            }
                        }
                        else if (acq.IsOwner(message.Author.Id))
                        {
                            (bool, tank) closest_match = util.FindClosestMatch2(msg2.CleanContent, data._data[message.Author.Id].Keys.ToList(), index);
                            if (closest_match.Item1)
                            {
                                EmbedBuilder eb = new EmbedBuilder();
                                activeinfo ai = new activeinfo();
                                DateTime expiry = DateTime.UtcNow.AddMinutes(1);
                                Int32 unixTimestamp = (int)expiry.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
                                ai.tkey = index.fromTank(closest_match.Item2);
                                ai.expirationtime = expiry;
                                ai.msg = acq.msg;
                                eb.ImageUrl = util.cdnget($@"{paths["images2"]}{closest_match.Item2.file}", cdnid, cdnchid, _client, ref cache);
                                eb.Title = closest_match.Item2.names[0];
                                eb.AddField("Offence", closest_match.Item2.offence, true);
                                eb.AddField("Defence", closest_match.Item2.defence, true);
                                eb.Description = $"Will timeout <t:{unixTimestamp}:R>\n**Valid names:**";
                                foreach (string name in closest_match.Item2.names)
                                {
                                    eb.Description += $"\n*{name}*";
                                }
                                eb.WithFooter($"File name: {closest_match.Item2.file}\nID: {index.fromTank(closest_match.Item2)}");
                                await acq.msg.ModifyAsync(msg =>
                                {
                                    msg.Embed = eb.Build();
                                });
                                await acq.msg.RemoveAllReactionsAsync();
                                await message.DeleteAsync();
                                activequeries.Remove(acq);
                                activeinfos.Add(ai);
                            }
                            else await msg2.ReplyAsync($"\"{message.CleanContent}\" not found");
                            /*List<string> options = new List<string>();
                            foreach (tank t in data._data[message.Author.Id].Keys) options.AddRange(t.names);
                            util.WriteArray(options);
                            string closest = util.FindClosestMatch(msg2.CleanContent, util.GetItemsOnPage(options.ToArray(), 25, acq.page).ToList());
                            foreach (tank t in data._data[message.Author.Id].Keys)
                            {
                                if (t.names.Contains(closest))
                                {
                                    if (data.has(message.Author.Id, t))
                                    {
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
                                        eb.WithFooter($"File name: {t.file}\nID: {index.fromTank(t)}");
                                        await acq.msg.ModifyAsync(msg =>
                                        {
                                            msg.Embed = eb.Build();
                                        });
                                        await message.DeleteAsync();
                                        activequeries.Remove(acq);
                                        activeinfos.Add(ai);
                                        found = true;
                                    }
                                    else
                                    {
                                        await msg2.ReplyAsync("You dont own this tank!");
                                        found = true;
                                    }
                                }
                            }
                            */
                        }
                    } //                 menu
                    else if (activeinfo.Contains(msg2.ReferencedMessage.Id, activeinfos.ToArray()))
                    {
                        string id = Regex.Match(msg2.Content, "<@[0-9]+>").Value.Replace("<@", "").Replace(">", "");
                        int actinf = activeinfo.Find(msg2.ReferencedMessage.Id, activeinfos.ToArray());
                        if (activeinfos[actinf].user == message.Author.Id)
                        {
                            if (Regex.IsMatch(msg2.Content, "<@(\\d+)>"))
                            {
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
                            catch (Exception ex)
                            {
                                util.logError(ex, paths["errorlog"]);
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
                else Console.WriteLine("no ref");
                data.write(index);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                util.logError(ex, paths["errorlog"]);
            }

            var a = rnd.Next(0, 50);
            if (
                a == 1 && 
                curcfg.active &&
                blocked.FirstOrDefault(pair => pair.Key == guild.Id, new KeyValuePair<ulong, DateTime>(0, DateTime.UnixEpoch)).Key
                == new KeyValuePair<ulong, DateTime>(0, DateTime.UnixEpoch).Key)
            {
                await Spawn(message.Channel, rnd.Next(0, index.tanks.Count));
            }
            
        }
        private async Task Spawn(ISocketMessageChannel? channel, int randtank)
        {
            Random rnd = new Random(); 
            ComponentBuilder builder = new ComponentBuilder(); 
            DateTime expiry = DateTime.UtcNow.AddMinutes(5);
            DateTime expiry2 = DateTime.UtcNow.AddMinutes(5);
            Int32 unixTimestamp = (int)expiry.Subtract(new DateTime(1970, 1, 1)).TotalSeconds; 
            IGuild guild = (channel as IGuildChannel)?.Guild;

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
            bool _break = false;
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
                    _break = true;
                }
            }
            if (_break) return;
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
                    _break = true;
                }
            }
            if (_break) return;
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
                    activeinfos.Remove(activeinfos[actinf]);
                    _break = true;
                }
            }
            if (_break) return;
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
                    activegivings.Remove(activegivings[actinf]);
                    _break = true;
                }
            }
            if (_break) return;
            foreach (bindreact bi in activereactions.ToArray())
            {
                if (DateTime.Compare((DateTime)bi.expirationtime, DateTime.UtcNow) < 0 && bi.expirationtime != DateTime.MinValue)
                {
                    foreach (KeyValuePair<Emoji, ReactionMetadata?> kvp in bi.rct)
                    {
                        await bi.msg.RemoveAllReactionsForEmoteAsync(kvp.Key);
                    }
                    _break = true;
                }
            }
            if (_break) return;
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
            try
            {
                foreach (SocketGuild gld in _guilds)
                {
                    gldcfg cfg = guilds[gldcfg.find(guilds.ToArray(), gld.Id)];
                    if (
                        cfg.active &&
                        blocked.FirstOrDefault(pair => pair.Key == gld.Id, new KeyValuePair<ulong, DateTime>(0, DateTime.UnixEpoch)).Key
                        == new KeyValuePair<ulong, DateTime>(0, DateTime.UnixEpoch).Key
                        )
                    {
                        ISocketMessageChannel chn = (ISocketMessageChannel)gld.GetChannel(cfg.channelid);
                        Random rnd = new Random();
                        if (chn != null)
                        {
                            await Spawn(chn, rnd.Next(0, index.tanks.Count - 1));
                        }
                    }
                }
            }
            
            catch (Exception ex)
            {
                util.logError(ex, paths["errorlog"]);
                Console.WriteLine(ex);
            }
        }
        private void CacheTimerEvent(Object source, ElapsedEventArgs e)
        {
            if (cache == null)
            {
                Console.WriteLine("its null retard");
                cache = new cache();
            }
            try
            {
                if (isReady)
                {
                    cache.checkAll();
                    File.WriteAllText("cache.json", cache.Serialize());
                }
            }
            catch (Exception ex)
            {
                util.logError(ex, paths["errorlog"]);
            }
        }
        private async Task ReactionAdded(Cacheable<IUserMessage, ulong> message, Cacheable<IMessageChannel, ulong> channel, SocketReaction react)
        {
            var _message = await message.GetOrDownloadAsync();
            if (_message == null) return;
            var _channel = await channel.GetOrDownloadAsync();
            if (_channel == null) return;
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
                            util.logError(ex, paths["errorlog"]);
                            await _message.ReplyAsync($"Couldnt download image\n{ex.Message}");
                        }
                    }
                    File.WriteAllText(paths["index"], index.Serialize(index));

                    await _message.AddReactionAsync(new Emoji("👍"));
                }
                catch (Exception ex)
                {
                    util.logError(ex, paths["errorlog"]);
                    await _message.AddReactionAsync(new Emoji("❌"));
                    Console.WriteLine(ex.Message);
                }
            }
            if (react.UserId != _client.CurrentUser.Id)
            {
                foreach (bindreact bind in activereactions.ToArray())
                {
                    if (_message.Id == bind.msg.Id)
                    {
                        foreach (KeyValuePair<Emoji, ReactionMetadata?> em in bind.rct)
                        {
                            if (em.Key.Name == react.Emote.Name && bind.userid.Contains(react.UserId))
                            {
                                bind.Fire(_message as RestUserMessage, em);
                                await _message.RemoveReactionAsync(react.Emote, react.User.Value);
                            }
                        }
                    }
                }
            }
        }
        private Task Disconnected(Exception ex)
        {
            disconnectLog.Add(ex, paths["disconnectlog"]);
            return Task.CompletedTask;
        }
        private async Task Connected()
        {
            if ((bool)cfg.DevMode)
            {
                var user = _client.Rest.GetUserAsync((ulong)cfg.Master).Result;
                Int32 unixTimestamp = (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
                await user.SendMessageAsync($"Just connected <t:{unixTimestamp}:R>");
            }
        }
    }
}
