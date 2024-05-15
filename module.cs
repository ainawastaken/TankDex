using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TankDex
{
    public class CommandModule : ModuleBase<SocketCommandContext>
    {
        [Command("suggestion")]
        [Summary("Send the developer a suggestion")]
        public async Task Suggestion()
        {
            await ReplyAsync("Hello!");
        }
    }
}
