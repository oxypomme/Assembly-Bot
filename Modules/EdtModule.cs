﻿using Discord;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace Assembly_Bot.Modules
{
    [Group("edt")]
    [Summary("EDT Commands")]
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
        [RequireBotPermission(ChannelPermission.ManageMessages)]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public async Task ForceEdtUpdate()
        {
            await _edt.ReloadEdt(true);
        }
    }
}