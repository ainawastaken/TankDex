using System;
using System.Timers;
using System.Reflection;
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
using System.Threading;
using System.Threading.Channels;
using System.Drawing;
using System.Runtime.InteropServices;


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
    internal class Program
    {
        private DiscordSocketClient _client;
        private CommandService _commands;
        private IServiceProvider _services;

        private data data = new data();
        private config cfg = new config();
        private index index = new index();

        private ulong cdnid = ulong.MaxValue;
        private ulong cdnchid = ulong.MaxValue;

        private volatile static List<activebtn> activebuttons = new List<activebtn>();
        private volatile static List<activequestion> activequestions = new List<activequestion>();

        private System.Timers.Timer buttonTimer;

        List<gldcfg> guilds = new List<gldcfg>();


        static void Main(string[] args)
        => new Program().RunBotAsync().GetAwaiter().GetResult();

        public async Task RunBotAsync()
        {
            _client = new DiscordSocketClient(
            new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
            });
            _commands = new CommandService();
            cfg.load(out cfg);
            index.load(out index);
            guilds = gldcfg.load().ToList();
            data.load(index);
            string[] cdnstr = File.ReadAllText("cdn.txt").Split(';');
            cdnid = ulong.Parse(cdnstr[0]);
            cdnchid = ulong.Parse(cdnstr[1]);
            
            _client.Log += Log;
            _client.Ready += Client_Ready;
            _client.SlashCommandExecuted += SlashCommandHandler;
            _client.GuildAvailable += GuildAvailable;
            _client.GuildAvailable += GuildAvailable;
            _client.MessageReceived += MessageReceived;
            
            //_client.ButtonExecuted += ButtonExecuted;
            //_client.InteractionCreated += InteractionCreated;
            
            buttonTimer = new System.Timers.Timer(1000);
            buttonTimer.Elapsed += OnButtonTimedEvent;
            buttonTimer.AutoReset = true;
            buttonTimer.Enabled = true;

            await _client.LoginAsync(TokenType.Bot, cfg.Token);
            await _client.StartAsync();

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
                guilds.Add(new gldcfg(guild.Id, ulong.MaxValue, false, ulong.MaxValue, ulong.MaxValue));
                gldcfg.write(guilds.ToArray());
            }
            /*
            var commands = guild.GetApplicationCommandsAsync().Result;
            foreach (SocketApplicationCommand command in commands)
            {
                if (!cfg.Commands.Keys.Contains(command.Name))
                {
                    await command.DeleteAsync();
                }
            }
            foreach (KeyValuePair<string, string> kvp in cfg.Commands)
            {
                var guildCommand = new Discord.SlashCommandBuilder();
                guildCommand.WithName(kvp.Key);
                guildCommand.WithDescription(kvp.Value);

                try
                {
                    await guild.CreateApplicationCommandAsync(guildCommand.Build());
                }
                catch (HttpException exception)
                {
                    var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);
                }
            }
            */
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
                    if (cfg.Developers.Contains(command.User.Id) & util.isAdmin(_client.GetGuild((ulong)command.GuildId).GetUser(command.User.Id)))
                    {
                        Random rnd = new Random(DateTime.Now.Millisecond);
                        await command.RespondAsync($"Triggered! ||(Developer only)||", null, false, true);
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
                        await command.RespondAsync("You are not an admin!", null, false, true);
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
                        await command.RespondAsync("You are not an admin!", null, false, true);
                    }
            break;
                case "completion":
                    EmbedBuilder eb = new EmbedBuilder();
                    eb.Title = $"Tank completion of {command.User.GlobalName}";
                    eb.Description = $"{command.User.GlobalName} owns {data.ama(command.User.Id)} unique tanks.\n" +
                        $"And {data.tot(command.User.Id)} tanks in total.\n";
                    eb.AddField("Offence", data.pow(command.User.Id), true);
                    eb.AddField("Defence", data.def(command.User.Id), true);
                    eb.ThumbnailUrl = command.User.GetAvatarUrl();
                    await command.RespondAsync(null, embed:eb.Build());
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
            }
        }
        private async Task MessageReceived(SocketMessage message)
        {
            if (message is not IUserMessage userMessage || message.Author.IsBot)
                return;

            IGuild guild = (userMessage.Channel as IGuildChannel)?.Guild;

            var msg2 = message as IUserMessage;
            var msg3 = message as SocketUserMessage;

            Random rnd = new Random(DateTime.Now.Millisecond);
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
                            await msg2.ReplyAsync($"<@{message.Author.Id}> guessed \"*{qst.tank.names[0]}*\"!\nYou now own **{data.amt(message.Author.Id, qst.tank)}**, \"*{qst.tank.names[0]}'s*\"!");
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
                    }
                    else
                    {
                        Console.WriteLine("Suicide");
                    }
                }
                else
                {
                    Console.WriteLine("no ref");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                await msg2.AddReactionAsync(new Emoji("❗"));
            }

            Console.WriteLine(curcfg.active);
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
            DateTime expiry = DateTime.UtcNow.AddMinutes(1);
            Int32 unixTimestamp = (int)expiry.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
            IGuild guild = (channel as IGuildChannel)?.Guild;
            gldcfg cfg = guilds[gldcfg.find(guilds.ToArray(), guild.Id)];

            builder.WithButton(
                "Guess", 
                $"{rnd.Next(-10000, 10000)}", 
                ButtonStyle.Success);

            EmbedBuilder eb = new EmbedBuilder();
            eb.Title = $"**A tank has appeared!** *Expires <t:{unixTimestamp}:R>*";
            eb.WithImageUrl(util.cdnget($@"tanks\images\{index.tanks[index.tanks.Keys.ToArray()[randtank]].file}", cdnid, cdnchid, _client));

            RestUserMessage msg = await channel.SendMessageAsync(embed:eb.Build());

            /*RestUserMessage msg = await channel.SendFileAsync(
                $@"tanks\images\{index.tanks[index.tanks.Keys.ToArray()[randtank]].file}",
                $"**A tank has appeared!** *Expires <t:{unixTimestamp}:R>*");*/

            activebuttons.Add(new activebtn(
                msg,
                channel.Id,
                guild.Id,
                index.tanks.Keys.ToArray()[randtank],
                expiry));
            tank t = index.tanks[index.tanks.Keys.ToArray()[randtank]];
            activequestions.Add(new activequestion(msg, t, new activebtn(
                msg,
                channel.Id,
                guild.Id,
                index.tanks.Keys.ToArray()[randtank],
                expiry)));
        }
        
        private void OnButtonTimedEvent(Object source, ElapsedEventArgs e)
        {
            foreach (activequestion qst in activequestions)
            { 
                if (DateTime.Compare(qst.btn.expirationtime, DateTime.UtcNow) < 0)
                {
                    EmbedBuilder oldEmbed = qst.btn.msg.Embeds.FirstOrDefault().ToEmbedBuilder();
                    oldEmbed.WithTitle($"~~A tank has appeared!~~ ***Expired***");
                    qst.btn.msg.ModifyAsync(msg =>
                    {
                        msg.Embed = oldEmbed.Build();
                    });
                    foreach (activequestion d in activequestions)
                    {
                        Console.WriteLine(d.msg.Id);
                    }
                    Console.WriteLine(qst.btn.msg.Id);
                    activequestions.Remove(activequestions[activequestion.Find(qst.btn.msg.Id, activequestions.ToArray())]);
                    activebuttons.Remove(qst.btn);
                }
            }
        }
    }
}