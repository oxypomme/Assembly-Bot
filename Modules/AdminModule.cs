using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Assembly_Bot.Modules
{
    [Group("admin")]
    public class AdminModule : ModuleBase<SocketCommandContext>
    {
        [Command("clean", RunMode = RunMode.Async)]
        [Summary("Cleans the specified amount of messages in the channel. Default 100.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        [RequireBotPermission(ChannelPermission.ManageMessages)]
        public async Task CleanAsync(int count = 99)
        {
            await ChatUtils.CleanChannel(Context.Channel, count + 1);

            const int delay = 5000;
            var msg = await ReplyAsync($"Purge completed. _This message will be deleted in {delay / 1000} seconds._");
            await Task.Delay(delay);
            await msg.DeleteAsync();
        }
    }
}