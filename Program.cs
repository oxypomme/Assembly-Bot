using Assembly_Bot.Models;
using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Assembly_Bot
{
    public class Program
    {
        public static ServiceProvider services;

        public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

        private DiscordSocketClient _client;
        private System.Timers.Timer _timer;

        public Program()
        {
        }

        // Starting the program in async
        public static void Main(string[] args) => new Program().MainAsync().GetAwaiter().GetResult();

        private bool _isFirstTimeReady;

        public async Task MainAsync()
        {
            try
            {
                // Setup services
                services = new ServiceCollection()
                    .AddSingleton<DiscordSocketClient>()
                    .AddSingleton<CommandService>()
                    .AddSingleton<CommandHandler>()
                    .AddSingleton<Logs>()
                    .AddSingleton<Behaviour>()
                    .BuildServiceProvider();
                _client = services.GetRequiredService<DiscordSocketClient>();

                _client.Log += services.GetRequiredService<Logs>().Log;
                services.GetRequiredService<CommandService>().Log += services.GetRequiredService<Logs>().Log;

                _client.Ready += async () =>
                {
                    // Log that we're ready
                    await services.GetRequiredService<Logs>().Log(new LogMessage(LogSeverity.Info, "Ready", $"Connected as {_client.CurrentUser} on {_client.Guilds.Count} servers"));

#if !DEBUG
                    if (_isFirstTimeReady)
                        // Bypass the condition in previous statement
                        await services.GetRequiredService<Logs>().LogOnDiscord("Hello world", "I've just awoken my master !", Color.Green, new List<EmbedFieldBuilder>
                        {
                            new EmbedFieldBuilder() { Name = "Launch platform", Value = Environment.OSVersion + "\nat " + DateTime.Now.ToString("HH:mm:ss") }
                        }).ConfigureAwait(true);
                    _isFirstTimeReady = false;
#endif
                };

                // Setup and starting the bot
                await _client.LoginAsync(TokenType.Bot, File.ReadLines("token.txt").First());
                await _client.StartAsync();

                await services.GetRequiredService<CommandHandler>().InstallCommandsAsync();

                // Just fooling around with activity
#if DEBUG
                await _client.SetGameAsync("laboratoire", type: ActivityType.Playing);
#else
                await _client.SetGameAsync("Sbotify", type: ActivityType.Listening);
#endif

                // Setup some events
                _client.UserVoiceStateUpdated += async (user, oldVoiceState, newVoiceState) =>
                {
                    try
                    {
                        // [Specific APSU] Setup the cleaner for work channels
                        await services.GetRequiredService<Behaviour>().GroupChatToClean(user, oldVoiceState, newVoiceState);
                    }
                    catch (Exception ex) { await services.GetRequiredService<Logs>().Log(new LogMessage(LogSeverity.Error, "VoiceStateUpdated", ex.Message, ex)); }
                };

                // Setup a Timer
                _timer = new System.Timers.Timer(
#if DEBUG
                10000
#else
                30000
#endif
                );

                _timer.Elapsed += async (sender, e) =>
                {
                    try
                    {
                        // [Specific APSU] The alerts
                        services.GetRequiredService<Behaviour>().AlertStudents(sender, e);
                    }
                    catch (Exception ex) { await services.GetRequiredService<Logs>().Log(new LogMessage(LogSeverity.Error, "TimerElapsed", ex.Message, ex)); }
                };
                _timer.AutoReset = true;
                _timer.Enabled = true;

                await Task.Delay(-1);
            }
            catch (Exception e) { await services.GetRequiredService<Logs>().Log(new LogMessage(LogSeverity.Error, "InstallCommands", e.Message, e)); }
        }
    }
}