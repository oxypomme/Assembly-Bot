using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Assembly_Bot
{
    internal class ChatUtils
    {
        [RequireBotPermission(ChannelPermission.ManageMessages)]
        public static async Task CleanChannel(ISocketMessageChannel channel, int count)
        {
            var messages = await channel.GetMessagesAsync(count).FlattenAsync();

            await (channel as SocketTextChannel).DeleteMessagesAsync(messages);
        }
    }
}