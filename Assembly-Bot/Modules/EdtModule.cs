using Discord;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace Assembly_Bot.Modules
{
    [Group("edt")]
    [Summary("EDT Commands : `edt`")]
    public class EdtModule : ModuleBase<SocketCommandContext>
    {
        private Edt _edt;

        public EdtModule(IServiceProvider services)
        {
            _edt = services.GetRequiredService<Edt>();
        }

        [Command("force")]
        [Summary("Force an update of timetables")]
        [RequireOwner]
        public async Task ForceEdtUpdate()
        {
            await _edt.ReloadEdt(true);
        }
    }
}