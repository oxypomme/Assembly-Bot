using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assembly_Bot.Modules
{
    [Group("admin")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public class AdminModule : ModuleBase<SocketCommandContext>
    {
        [Command("clean", RunMode = RunMode.Async)]
        [Summary("Cleans the specified amount of messages in the channel. Default 100.")]
        [RequireBotPermission(ChannelPermission.ManageMessages)]
        public async Task CleanAsync(int count = 99)
        {
            await ChatUtils.CleanChannel(Context.Channel, count + 1);

            const int delay = 5000;
            var msg = await ReplyAsync($"Purge completed. _This message will be deleted in {delay / 1000} seconds._");
            await Task.Delay(delay);
            await msg.DeleteAsync();
        }

        [Command("mutev", RunMode = RunMode.Async)]
        [Summary("Mute a whole voice chat for specified duration. Default yours, and for 1 min.")]
        [RequireBotPermission(GuildPermission.ManageRoles)]
        public async Task MuteVoiceAsync(ulong vid = 0, int secs = 60)
        {
            SocketVoiceChannel channel = Context.Guild.GetVoiceChannel(vid);
            if (vid == 0)
            {
                foreach (SocketVoiceChannel vchat in Context.Guild.VoiceChannels)
                    if (vchat.Users.Contains(Context.User))
                    {
                        channel = vchat;
                        break;
                    }
            }

            foreach (var vuser in channel.Users)
                await vuser.ModifyAsync((user) => user.Mute = true);

            var msg = await ReplyAsync($"{channel.Name} is now muted for {secs} seconds.");
            await Context.Message.DeleteAsync();

            await Task.Delay(secs * 1000);
            foreach (var vuser in channel.Users)
                await vuser.ModifyAsync((user) => user.Mute = false);

            const int delay = 5000;
            await msg.ModifyAsync(m => m.Content = $"{channel.Name} is no longer muted. _This message will be deleted in {delay / 1000} seconds._");
            await Task.Delay(delay);
            await msg.DeleteAsync();
        }

        [Command("activity")]
        [Summary("Set the bot's activity. Default : Playing. See doc about `ActivityType` for ids.")]
        public async Task SetActivity(string activity, int type = 0) => await Context.Client.SetGameAsync(activity, type: (ActivityType)type);
    }
}