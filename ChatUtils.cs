using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
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
                await channel.SendMessageAsync("@here : " + message);
            else
                await channel.SendMessageAsync(mentionable + " : " + message);
        }

        public static Embed CreateEmbed(EmbedBuilder builder)
        {
            builder.WithFooter(new EmbedFooterBuilder() { Text = "by OxyTom#1831" }).WithTimestamp(DateTimeOffset.Now).WithAuthor(Program.services.GetRequiredService<DiscordSocketClient>().CurrentUser);
            return builder.Build();
        }

        public static Embed CreateEmbed(string title, string message, Color color, List<EmbedFieldBuilder> fields = null)
        {
            var builder = new EmbedBuilder()
            {
                Title = title,
                Description = message,
                Timestamp = DateTimeOffset.Now,
                Color = color,
                Footer = new EmbedFooterBuilder() { Text = "by OxyTom#1831", IconUrl = "https://avatars3.githubusercontent.com/u/34627360?u=2e1dd6031fa2703dfd3f16700c978c85559e2e5f" }
            }.WithAuthor(Program.services.GetRequiredService<DiscordSocketClient>().CurrentUser);
            if (fields != null)
                foreach (var field in fields)
                    builder.AddField(field);
            return builder.Build();
        }
    }
}