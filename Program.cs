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

        public async Task MainAsync()
        {
            // Setup services
            services = new ServiceCollection()
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandler>()
                .AddSingleton<GlobalBehaviour>()
                .BuildServiceProvider();
            _client = services.GetRequiredService<DiscordSocketClient>();

            _client.Log += services.GetRequiredService<GlobalBehaviour>().Log;
            services.GetRequiredService<CommandService>().Log += services.GetRequiredService<GlobalBehaviour>().Log;

            _client.Ready += async () =>
            {
                // Log that we're ready
                await services.GetRequiredService<GlobalBehaviour>().Log(new LogMessage(LogSeverity.Info, "Ready", $"Connected as {_client.CurrentUser} on {_client.Guilds.Count} servers"));

#if !DEBUG
                if (_isFirstTimeReady)
                    // Bypass the condition in previous statement
                    await services.GetRequiredService<GlobalBehaviour>().LogOnDiscord("Hello world", "I've just awoken my master !", Color.Green, new List<EmbedFieldBuilder>
                    {
                        new EmbedFieldBuilder() { Name = "Launch platform", Value = Environment.OSVersion + "\nat " + DateTime.Now.ToString("HH:mm:ss") }
                    }).ConfigureAwait(true);
                _isFirstTimeReady = false;
#endif
            };

            // Setup and starting the bot
            await _client.LoginAsync(TokenType.Bot, File.ReadLines("token.txt").First());
            await _client.StartAsync();
            try
            {
                await services.GetRequiredService<CommandHandler>().InstallCommandsAsync();
            }
            catch (Exception e) { await services.GetRequiredService<GlobalBehaviour>().Log(new LogMessage(LogSeverity.Error, "InstallCommands", e.Message, e)); }

            // Just fooling around with activity
#if DEBUG
            await _client.SetGameAsync("laboratoire", type: ActivityType.Playing);
#else
            await _client.SetGameAsync("Sbotify", type: ActivityType.Listening);
#endif

            // Setup some events
            _client.UserVoiceStateUpdated += async (user, oldVoiceState, newVoiceState) =>
            {
                // [Specific APSU] Setup the cleaner for work channels
                await VoiceUtils.GroupChatToClean(user, oldVoiceState, newVoiceState);
            };

            // Setup a Timer
            _timer = new System.Timers.Timer(
#if DEBUG
                10000
#else
                30000
#endif
            );
            //
            _timer.Elapsed += AlertStudents;
            _timer.AutoReset = true;
            _timer.Enabled = true;

            await Task.Delay(-1);
        }

        private (bool falert, bool salert) _isAlreadyAlerted = (false, false);
        private DateTime _lastUpdate;
        private bool _isFirstTimeReady;

        public void AlertStudents(object sender, System.Timers.ElapsedEventArgs e)
        {
            Task.Run(async () =>
            {
                try
                {
                    if (_lastUpdate.AddHours(2) <= DateTime.Now)
                    {
                        _lastUpdate = DateTime.Now;
                        await EdtUtils.ReloadEdt();
                    }
                    foreach (var edt in EdtUtils.edts) //TODO: Tasks ?
                    {
                        Day day; // DayOfWeek.Sunday = 0, or in the JSON, Sunday is the 7th day
                        Console.WriteLine($"We're {DateTime.Today.DayOfWeek} ({(int)DateTime.Today.DayOfWeek})");
                        if (DateTime.Today.DayOfWeek == DayOfWeek.Sunday)
                        {
                            day = edt.Weeks[0].Days[6]; // Get the real Sunday
                            Console.WriteLine($"Getted i:6");
                        }
                        else
                        {
                            day = edt.Weeks[0].Days[(int)DateTime.Today.DayOfWeek - 1]; // Get the day
                            Console.WriteLine($"Getted i:" + ((int)DateTime.Today.DayOfWeek - 1));
                        }
                        SocketTextChannel channel;
#if DEBUG
                        channel = Sandbox.main;
#endif
                        Console.WriteLine("Is it time ?");
                        foreach (var evnt in day.Events) //TODO: Tasks ?
                        {
                            var timeLeft = evnt.Dtstart.Subtract(DateTime.Now);
                            if (timeLeft.Hours == 0 && timeLeft.Minutes <= 15 && !(_isAlreadyAlerted.falert && _isAlreadyAlerted.salert))
                            {
                                Console.WriteLine("15min before next lesson !");
                                var eventSplitted = evnt.Summary.Split(" - ");
                                // Mat - Group - Room - Type
#if !DEBUG
                                if (eventSplitted[1].EndsWith("grp3.1"))
                                    channel = Apsu.infos[1];
                                else if (eventSplitted[1].EndsWith("grp3.2"))
                                    channel = Apsu.infos[2];
                                else
                                    channel = Apsu.infos[0];
#endif
                                if (timeLeft.Minutes == 15 && !_isAlreadyAlerted.falert)
                                {
                                    Console.WriteLine("ping 15");
                                    await ChatUtils.PingMessage(channel, $"{eventSplitted[0]} dans 15 minutes.");
                                    _isAlreadyAlerted.falert = true;
                                }
                                else if (timeLeft.Minutes == 5 && !_isAlreadyAlerted.salert)
                                {
                                    Console.WriteLine("ping 5");
                                    await ChatUtils.PingMessage(channel, $"{eventSplitted[0]} dans 5 minutes.", channel.Guild.EveryoneRole);
                                    _isAlreadyAlerted.salert = true;
                                }
                            }
                            else if ((timeLeft.Hours != 0 || timeLeft.Minutes != 10) && _isAlreadyAlerted.falert && _isAlreadyAlerted.salert)
                            {
                                Console.WriteLine("reset ping");
                                _isAlreadyAlerted.falert = false;
                                _isAlreadyAlerted.salert = false;
                            }
                        }
                    }
                }
                catch (Exception e) { await services.GetRequiredService<GlobalBehaviour>().Log(new LogMessage(LogSeverity.Error, "AlertStudents", e.Message, e)); }
            });
        }
    }
}