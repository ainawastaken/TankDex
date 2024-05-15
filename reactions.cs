#pragma warning disable

using Discord;
using Discord.Rest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TankDex
{
    public static class Reactions
    {
        public static void CrashLogDeleteHandler(RestUserMessage msg, KeyValuePair<Emoji, ReactionMetadata?> react, bindreact bind)
        {
            File.WriteAllText(Program.paths["crashlog"], "");
            msg.RemoveAllReactionsForEmoteAsync(react.Key);
        }
        public static void MenuReactHandler(RestUserMessage msg, KeyValuePair<Emoji, ReactionMetadata?> react, bindreact bind)
        {
            try
            {
                activequery menu = Program.activequeries[activequery.Find(msg.Id, Program.activequeries.ToArray())];
                if (react.Key.Name == new Emoji("⏪").Name)
                {
                    if (menu.page > 0)
                    {
                        menu.page--;
                    }
                }
                else if (react.Key.Name == new Emoji("⏩").Name)
                {
                    if (menu.page < util.CalculatePagesNeeded(Program.data.ama((ulong)menu.userid), 25))
                    {
                        menu.page++;
                    }
                }
                EmbedBuilder _eb = new();
                DateTime expiry = DateTime.UtcNow.AddMinutes(1);
                Int32 unixTimestamp = (int)expiry.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
                activequery aq = new();
                IUser user = Program._client.Rest.GetUserAsync((ulong)menu.userid).Result;
                _eb.Title = $"Tank info for {user.GlobalName}";
                _eb.Description = $"Page {menu.page + 1}/{util.CalculatePagesNeeded(Program.data.ama((ulong)menu.userid), 25)}\n{util.CalculateItemsOnPage(Program.data.ama((ulong)menu.userid), 25, menu.page)}/25 per page" +
                    $"\nType \"next\" for next page. \"previous\" for previous page, or page number. And tank ID for info\n" +
                    $"Will timeout <t:{unixTimestamp}:R>";
                List<string> allItems = new();
                foreach (tank t in Program.data._data[(ulong)menu.userid].Keys)
                {
                    allItems.Add(Program.index.fromTank(t));
                }

                int depth = 0;
                foreach (string item in util.GetItemsOnPage(allItems.ToArray(), 25, menu.page))
                {
                    if (depth == 25) break;
                    EmbedFieldBuilder efb = new();
                    efb.WithName($"{item} x{Program.data.amt((ulong)menu.userid, Program.index.fromId(item))}");
                    efb.WithValue(Program.index.fromId(item).names[0]);
                    efb.IsInline = true;
                    _eb.AddField(efb);
                    depth++;
                }
                menu.msg.ModifyAsync(msg =>
                {
                    msg.Embed = _eb.Build();
                });
                Program.activequeries[activequery.Find(msg.Id, Program.activequeries.ToArray())] = menu;
                DateTime _expiry = DateTime.UtcNow.AddMinutes(1);
                Int32 _unixTimestamp = (int)_expiry.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
                bind.expirationtime = _expiry;
                Program.activereactions[Program.activereactions.IndexOf(bind)] = bind;
            }
            catch
            {

            }
        }
    }
}
