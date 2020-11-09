using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Assembly_Bot.Models
{
    public static class Apsu
    {
        public static SocketGuild server;
        public static List<SocketTextChannel> infos;
        public static List<SocketTextChannel> edts;

        static Apsu()
        {
            server = Program.services.GetRequiredService<DiscordSocketClient>().GetGuild(773545167117746198);
            infos = new List<SocketTextChannel>()
            {
                server.GetTextChannel(773546790090833920),
                server.GetTextChannel(773546828947259443),
                server.GetTextChannel(773546852183310337)
            };
            edts = new List<SocketTextChannel>()
            {
                server.GetTextChannel(773550484677066782),
                server.GetTextChannel(773550604173049878)
            };
        }
    }
}