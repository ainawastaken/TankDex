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


#pragma warning disable CS1998
#pragma warning disable CS8600
#pragma warning disable CS8602
#pragma warning disable CS8604
#pragma warning disable CS8618
#pragma warning disable CS8622

namespace TankDex
{
    internal class Program
    {
        private DiscordSocketClient _client;
        private CommandService _commands;
        private IServiceProvider _services;
        private config cfg = new config();
        private index index = new index();

        private static List<activebtn> activebuttons = new List<activebtn>();

        private System.Timers.Timer buttonTimer;

        List<gldcfg> guilds = new List<gldcfg>();


        static void Main(string[] args)
        => new Program().RunBotAsync().GetAwaiter().GetResult();

        public async Task RunBotAsync()
        {
            _client = new DiscordSocketClient();
            _commands = new CommandService();
            cfg.load(out cfg);
            index.load(out index);
            guilds = gldcfg.load().ToList();

            _client.Log += Log;
            _client.Ready += Client_Ready;
            _client.SlashCommandExecuted += SlashCommandHandler;
            _client.GuildAvailable += GuildAvailable;
            _client.MessageReceived += MessageReceived;
            _client.ButtonExecuted += ButtonExecuted;
            _client.InteractionCreated += InteractionCreated;

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
            string[] paths = Directory.GetFiles(@"tanks\images");
            await _client.SetGameAsync($"{paths.Length} tanks!", null, ActivityType.Watching);
        }
        public async Task GuildAvailable(SocketGuild guild)
        {
            if (!gldcfg.contains(guilds.ToArray(), guild.Id))
            {
                guilds.Add(new gldcfg(guild.Id, ulong.MaxValue, false));
                gldcfg.write(guilds.ToArray());
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
                    Console.WriteLine(json);
                }
            }
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
                        int a = gldcfg.find(guilds.ToArray(), command.GuildId);
                        await command.RespondAsync($"Config for this guild: **\"{guilds[a]}\"**", null, false, false);
                    }
                    else
                    {
                        await command.RespondAsync("You're not a developer!", null, false, true);
                    }
                    break;
                case "trigger":
                    if (cfg.Developers.Contains(command.User.Id))
                    {
                        Random rnd = new Random(DateTime.Now.Millisecond);
                        await command.RespondAsync($"Triggered! ||(Developer only)||", null, false, false);
                        await Spawn(command.Channel, rnd.Next(0, index.tanks.Count));
                    }
                    else
                    {
                        await command.RespondAsync("You're not a developer!", null, false, true);
                    }
                    break;
                case "activate":
                    break;
                case "disable":
                    break;
            }
        }
        private async Task MessageReceived(SocketMessage message)
        {
            if (message is not IUserMessage userMessage || message.Author.IsBot)
                return;

            IGuild guild = (userMessage.Channel as IGuildChannel)?.Guild;

            Random rnd = new Random(DateTime.Now.Millisecond);
            gldcfg curcfg = guilds[gldcfg.find(guilds.ToArray(), guild.Id)];
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

            builder.WithButton(
                "Guess", 
                $"{rnd.Next(-10000, 10000)}", 
                ButtonStyle.Success);
            RestUserMessage msg = await channel.SendFileAsync(
                $@"tanks\images\{index.tanks[index.tanks.Keys.ToArray()[randtank]].file}",
                $"**A tank has appeared!** *Expires <t:{unixTimestamp}:R>*",
                components: builder.Build());
            ButtonComponent btn = msg.Components.ToArray()[0].Components.ToArray()[0] as ButtonComponent;

            IGuild guild = (channel as IGuildChannel)?.Guild;
            activebuttons.Add(new activebtn(btn,
                msg,
                channel.Id,
                guild.Id,
                index.tanks.Keys.ToArray()[randtank],
                expiry));
        }
        private async Task ButtonExecuted(SocketMessageComponent component)
        {
            
        }
        private void OnButtonTimedEvent(Object source, ElapsedEventArgs e)
        {
            foreach (activebtn btn in activebuttons)
            {
                if (DateTime.Compare(btn.expirationtime, DateTime.UtcNow) < 0)
                {
                    btn.msg.ModifyAsync(properties =>
                    {
                        properties.Content = "**A tank has appeared!**";
                        properties.Components = (new ComponentBuilder().WithButton(
                            "Guess",
                            btn.btn.CustomId,
                            ButtonStyle.Danger,
                            null,
                            null,
                            true)).Build();
                    });
                    activebuttons.Remove(btn);
                    Console.WriteLine($"Button timed out. {btn.btn.CustomId}");
                }
            }
        }
        private async Task InteractionCreated(object sender, object e)
        {
            if (e is SocketMessageComponent interaction)
            {
                activebtn b = activebtn.find(activebuttons.ToArray(), interaction.Data.CustomId);
                var tb = new TextInputBuilder()
                    .WithLabel("Name or Designation:")
                    .WithCustomId($"{interaction.Data.CustomId}text_input")
                textInp 
            }
            
        }
    }
}