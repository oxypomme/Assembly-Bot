using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Assembly_Bot
{
    public class CommandHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        private readonly IServiceProvider _services;

        private const string _prefix = "!";

        public CommandHandler(IServiceProvider services)
        {
            _services = services;
            _commands = services.GetRequiredService<CommandService>();
            _client = services.GetRequiredService<DiscordSocketClient>();

            _client.MessageReceived += HandleCommandAsync;
        }

        public async Task InstallCommandsAsync() => await _commands.AddModulesAsync(System.Reflection.Assembly.GetEntryAssembly(), _services);

        private async Task HandleCommandAsync(SocketMessage messageParam)
        {
            if (!(messageParam is SocketUserMessage || messageParam.Source == Discord.MessageSource.User))
                return;

            var message = messageParam as SocketUserMessage;

            int argPos = 0;
            if (!(message.HasStringPrefix(_prefix, ref argPos)))
                return;

            var context = new SocketCommandContext(_client, message);

            var result = await _commands.ExecuteAsync(context, argPos, _services);

            if (!result.IsSuccess)
                await context.Channel.SendMessageAsync(result.ErrorReason);
        }
    }
}