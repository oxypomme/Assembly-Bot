using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Assembly_Bot.Modules
{
    [Summary("Utils Commands")]
    public class UtilsModule : ModuleBase<SocketCommandContext>
    {
        [Command("ping")]
        [Summary("Wanna play huh ?")]
        public Task PingAsync() => ReplyAsync("Pong !");
    }
}