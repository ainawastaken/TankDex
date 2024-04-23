using Discord;
using Discord.Rest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TankDex
{
    public static class reactions
    {
        public static void crashLogDeleteHandler(RestUserMessage msg, KeyValuePair<Emoji, ReactionMetadata?> react, bindreact bind)
        {
            File.WriteAllText(Program.paths["crashlog"], "");
            msg.RemoveAllReactionsForEmoteAsync(react.Key);
        }
    }
}
