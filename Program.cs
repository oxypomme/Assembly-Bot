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
        public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);
        public static ServiceProvider services;

        private DiscordSocketClient _client;
        private Logs _loggger;
        private System.Timers.Timer _timer;

        public Program()
        {
        }

        // Starting the program in async
        public static void Main(string[] args) => new Program().MainAsync().GetAwaiter().GetResult();

        private bool _isFirstTimeReady = true;

        public async Task MainAsync()
        {
            try
            {
                // Setup services
                _client = new DiscordSocketClient();
                services = new ServiceCollection()
                    .AddSingleton(_client)
                    .AddSingleton<CommandService>()
                    .AddSingleton<CommandHandler>()
                    .AddSingleton<Logs>()
                    .AddSingleton<Behaviour>()
                    .AddSingleton<Edt>()
                    .BuildServiceProvider();
                _loggger = services.GetRequiredService<Logs>();

                _client.Log += _loggger.Log;
                services.GetRequiredService<CommandService>().Log += _loggger.Log;

                _client.Ready += async () =>
                {
                    // Log that we're ready
                    await _loggger.Log(new(LogSeverity.Info, "Ready", $"Connected as {_client.CurrentUser} on {_client.Guilds.Count} servers"));

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
                        // The user did move
                        if (newVoiceState.VoiceChannel != oldVoiceState.VoiceChannel)
                        {
                            // Clear any empty temporary channel
                            await services.GetRequiredService<Behaviour>().ClearTempChans(oldVoiceState.VoiceChannel);
                            // [Specific APSU] Setup the cleaner for work channels
                            await services.GetRequiredService<Behaviour>().GroupChatToClean(user, oldVoiceState, newVoiceState);
                        }
                    }
                    catch (Exception ex) { await services.GetRequiredService<Logs>().Log(new(LogSeverity.Error, "VoiceStateUpdated", ex.Message, ex)); }
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
                    catch (Exception ex) { await services.GetRequiredService<Logs>().Log(new(LogSeverity.Error, "TimerElapsed", ex.Message, ex)); }
                };
                _timer.AutoReset = true;
                _timer.Enabled = true;

                await Task.Delay(-1);
            }
            catch (Exception e) { await _loggger.Log(new(LogSeverity.Error, "InstallCommands", e.Message, e)); }
        }
    }
}