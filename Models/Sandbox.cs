using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Assembly_Bot.Models
{
    public static class Sandbox
    {
        public static SocketGuild server;
        public static SocketTextChannel main;
        public static SocketTextChannel log;

        static Sandbox()
        {
            server = Program.services.GetRequiredService<DiscordSocketClient>().GetGuild(436909627834368010);
            main = server.GetTextChannel(773611076557602836);
            log = server.GetTextChannel(774398527718686781);
        }
    }
}