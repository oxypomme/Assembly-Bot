using Assembly_Bot.Models;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Assembly_Bot
{
    public class Logs
    {
        private DiscordSocketClient _client;

        public Logs(IServiceProvider services)
        {
            _client = services.GetRequiredService<DiscordSocketClient>();
        }

        public async Task Log(LogMessage message)
        {
            Color color = Color.Default;
            bool isImp = false;
            switch (message.Severity)
            {
                case LogSeverity.Critical:
                case LogSeverity.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    color = Color.Red;
                    isImp = true;
                    break;

                case LogSeverity.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    color = Color.Orange;
                    break;

                case LogSeverity.Info:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;

                case LogSeverity.Verbose:
                case LogSeverity.Debug:
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    break;
            }
            Console.Write($"{DateTime.Now,-19} [{message.Severity}] {message.Source}: {message.Message}");
            if (message.Exception != null)
                Console.Write(" " + message.Exception.StackTrace);
            Console.WriteLine();
            if (_client.ConnectionState == ConnectionState.Connected && message.Severity < LogSeverity.Info)
                await LogOnDiscord("Something went wrong", "*" + message.Source + "*\n" + message.Message, color, isImportant: isImp).ConfigureAwait(true);
            Console.ResetColor();
        }

        public async Task LogOnDiscord(string title, string message, Color color, List<EmbedFieldBuilder> fields = null, bool isImportant = false)
        {
            try
            {
                string mention = (isImportant && Sandbox.server.Owner != null ? Sandbox.server.Owner.Mention : "");
                await Sandbox.log.SendMessageAsync(mention, embed: ChatUtils.CreateEmbed(title, message, color, fields)).ConfigureAwait(true);
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"{DateTime.Now,-19} [Error] DiscordLogger: {e.GetType().Name} {e}");
                Console.ResetColor();
            }
        }
    }
}