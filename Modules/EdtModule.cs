using Discord;
using Discord.Commands;
using System.Threading.Tasks;

namespace Assembly_Bot.Modules
{
    [Group("edt")]
    [Summary("EDT Commands")]
    public class EdtModule : ModuleBase<SocketCommandContext>
    {
        [Command("force")]
        [Summary("Force an update of timetables")]
        [RequireOwner]
        [RequireBotPermission(ChannelPermission.ManageMessages)]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public async Task ForceEdtUpdate()
        {
            await EdtUtils.ReloadEdt(true);
        }
    }
}