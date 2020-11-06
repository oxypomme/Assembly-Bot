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

        [RequireBotPermission(ChannelPermission.SendMessages)]
        public static async Task PingMessage(ISocketMessageChannel channel, string message, IMentionable mentionable = null)
        {
            if (mentionable == null)
                await channel.SendMessageAsync("@here" + " : " + message);
            else
                await channel.SendMessageAsync(mentionable + " : " + message);
        }
    }
}