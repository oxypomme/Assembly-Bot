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
            if (vid == 0)
            {
                var usr = Context.User;
                foreach (SocketVoiceChannel vchat in Context.Guild.VoiceChannels)
                {
                    if (vchat.Users.Contains(usr))
                    {
                        var role = await Context.Guild.CreateRoleAsync("Muted", new GuildPermissions(speak: false), Color.DarkGrey, false, null);
                        await vchat.AddPermissionOverwriteAsync(role, new OverwritePermissions(speak: PermValue.Deny));
                        foreach (var vuser in vchat.Users)
                            await vuser.AddRoleAsync(role);

                        var startmsg = await ReplyAsync($"{vchat.Name} is now muted for {secs} seconds.");
                        await Context.Message.DeleteAsync();

                        await Task.Delay(secs * 1000);
                        await role.DeleteAsync();
                        await startmsg.DeleteAsync().ConfigureAwait(true);

                        const int delay = 5000;
                        var endmsg = await ReplyAsync($"{vchat.Name} is no longer muted. _This message will be deleted in {delay / 1000} seconds._");
                        await Task.Delay(delay);
                        await endmsg.DeleteAsync();

                        break;
                    }
                }
            }
        }
    }
}