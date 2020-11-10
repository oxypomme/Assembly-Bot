using Discord;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Assembly_Bot.Modules
{
    [Group("debug")]
    [Summary("Utils Commands : `debug`")]
    public class DebugModule : ModuleBase<SocketCommandContext>
    {
        [Command("roleinfo")]
        [Summary("Get info about a role.")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public async Task MentionRoleAsync(IRole role)
        {
            var embed = new EmbedBuilder();
            embed.WithDescription("Role ID : " + role.Id + Environment.NewLine + "Role Name : " + role.Name + Environment.NewLine + "Role Mention : " + role.Mention + Environment.NewLine + "Role Mention : " + role.Mention + Environment.NewLine + "Role Color : " + role.Color.ToString() + Environment.NewLine + "Role Created at : " + role.CreatedAt);
            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }
    }
}