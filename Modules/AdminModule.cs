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
    [Summary("Admin Commands : `admin`")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public class AdminModule : ModuleBase<SocketCommandContext>
    {
        [Command("clean", RunMode = RunMode.Async)]
        [Alias("cleans", "clear", "clears", "purge", "prune")]
        [Summary("Cleans the specified amount of messages in the channel.")]
        public async Task CleanAsync([Summary("Default 100")] int count = 100)
        {
            await Context.Message.DeleteAsync();
            await ChatUtils.CleanChannel(Context.Channel, count);

            const int delay = 5000;
            var msg = await ReplyAsync($"Purge completed. _This message will be deleted in {delay / 1000} seconds._");
            await Task.Delay(delay);
            await msg.DeleteAsync();
        }

        [Command("mutev", RunMode = RunMode.Async)]
        [Summary("Mute a whole voice chat for a specified duration.")]
        public async Task MuteVoiceAsync([Summary("The id of the voice channel. Default it's yours")] ulong vid = 0, [Summary("The duration of the mute. Default 1 min")] int secs = 60)
        {
            SocketVoiceChannel channel = Context.Guild.GetVoiceChannel(vid);
            if (vid == 0)
            {
                channel = ((SocketGuildUser)Context.User).VoiceChannel;
                if (channel is null)
                    foreach (var vchat in Context.Guild.VoiceChannels)
                        if (vchat.Users.Contains(Context.User))
                        {
                            channel = vchat;
                            break;
                        }
                if (channel is null)
                    throw new ArgumentException("Je vous ai pas trouvé, déso pas déso");
            }
            await channel.AddPermissionOverwriteAsync(Context.Guild.EveryoneRole, new(speak: PermValue.Deny));
            foreach (var vuser in channel.Users)
                await vuser.ModifyAsync((user) => user.Mute = true);

            var msg = await ReplyAsync($"{channel.Name} is now muted for {secs} seconds.");
            await Context.Message.DeleteAsync();

            await Task.Delay(secs * 1000);
            await channel.AddPermissionOverwriteAsync(Context.Guild.EveryoneRole, new(speak: PermValue.Allow));
            foreach (var vuser in channel.Users)
                await vuser.ModifyAsync((user) => user.Mute = false);

            const int delay = 5000;
            await msg.ModifyAsync(m => m.Content = $"{channel.Name} is no longer muted. _This message will be deleted in {delay / 1000} seconds._");
            await Task.Delay(delay);
            await msg.DeleteAsync();
        }

        [Command("activity")]
        [Summary("Set the bot's activity.")]
        public async Task SetActivity(string activity, [Summary("Default : Playing. See doc about `ActivityType` for ids.")] int type = 0)
        {
            await Context.Client.SetGameAsync(activity, type: (ActivityType)type);
            await Context.Channel.SendMessageAsync("Activity updated");
        }
    }
}