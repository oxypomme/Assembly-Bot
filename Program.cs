using Discord;
using Discord.Commands;
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
        public static List<Models.Edt> edts = new List<Models.Edt>();
        private DiscordSocketClient _client;
        private System.Timers.Timer _timer;

        public Program()
        {
        }

        public static async Task ReloadEdt()
        {
            using (var client = new WebClient())
            {
                //var url = new Uri("http://wildgoat.fr/api/info-ical-json.php?url=" + System.Web.HttpUtility.UrlEncode("https://dptinfo.iutmetz.univ-lorraine.fr/lna/agendas/ical.php?ical=4352c5485001785") + "&week=1");
                var url = new Uri("http://wildgoat.fr/api/info-ical-json.php?url=4352c5485001785&week=1");
                edts.Add(JsonConvert.DeserializeObject<Models.Edt>(await client.DownloadStringTaskAsync(url)));
            }
        }

        public static void Main(string[] args) => new Program().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
        {
            using var services = new ServiceCollection()
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandler>()
                .BuildServiceProvider();

            _client = services.GetRequiredService<DiscordSocketClient>();
            _client.Log += Log;
            services.GetRequiredService<CommandService>().Log += Log;

            _client.Ready += async () =>
            {
                await Log(new LogMessage(LogSeverity.Info, "Ready", $"Connected as {_client.CurrentUser} on {_client.Guilds.Count} servers"));
                //PM me the start timestamps
                await (await _client.GetUser(151261754704265216).GetOrCreateDMChannelAsync()).SendMessageAsync(embed: new EmbedBuilder()
                {
                    Title = "Hello world",
                    Description = "I've just awoken my master !",
                    Timestamp = DateTimeOffset.Now,
                    Color = Color.Green,
                    Footer = new EmbedFooterBuilder() { Text = "by OxyTom#1831" }
                }
                .AddField("Launch platform", Environment.OSVersion + "\nat " + DateTime.Now.ToString("HH:mm:ss"))
                .WithAuthor(_client.CurrentUser)
                .Build());
            };

            await _client.LoginAsync(TokenType.Bot, File.ReadLines("token.txt").First());
            await _client.StartAsync();

            await services.GetRequiredService<CommandHandler>().InstallCommandsAsync();

#if DEBUG
            await _client.SetGameAsync("laboratoire", type: ActivityType.Playing).ConfigureAwait(true);
#else
            await _client.SetGameAsync("le prochain cours", type: ActivityType.Listening).ConfigureAwait(true);
#endif

            _client.UserVoiceStateUpdated += VoiceUtils.GroupChatToClean;

            await ReloadEdt().ConfigureAwait(true);

#if DEBUG
            _timer = new System.Timers.Timer(10000);
#else
            _timer = new System.Timers.Timer(30000);
#endif
            _timer.Elapsed += AlertStudents;
            _timer.AutoReset = true;
            _timer.Enabled = true;

            await Task.Delay(-1);
        }

        private (bool falert, bool salert) _isAlreadyAlerted = (false, false);

        public async void AlertStudents(object sender, System.Timers.ElapsedEventArgs e)
        {
            foreach (var edt in edts)
                if (edt.Weeks[0].Days.Count >= (int)DateTime.Today.DayOfWeek)
                {
                    var day = edt.Weeks[0].Days[(int)DateTime.Today.DayOfWeek - 1];
                    SocketTextChannel channel;
#if DEBUG
                    channel = _client.GetGuild(436909627834368010).GetTextChannel(773611076557602836); // Sandbox - assembly_bot
#endif
                    foreach (var evnt in day.Events)
                    {
                        var timeLeft = evnt.Dtstart.Subtract(DateTime.Now);
                        if (timeLeft.Hours == 0 && timeLeft.Minutes <= 15 && !(_isAlreadyAlerted.falert && _isAlreadyAlerted.salert))
                        {
                            var eventSplitted = evnt.Summary.Split(" - ");
                            // Mat - Group - Room - Type
#if !DEBUG
                            if (eventSplitted[1].EndsWith("grp3.1"))
                                channel = _client.GetGuild(773545167117746198).GetTextChannel(773546828947259443); // APSU - grp3-1
                            else if (eventSplitted[1].EndsWith("grp3.2"))
                                channel = _client.GetGuild(773545167117746198).GetTextChannel(773546852183310337); // APSU - grp3-2
                            else
                                channel = _client.GetGuild(773545167117746198).GetTextChannel(773546790090833920); // APSU - grp3
#endif
                            if (timeLeft.Minutes == 15 && !_isAlreadyAlerted.falert)
                            {
                                await ChatUtils.PingMessage(channel, $"{eventSplitted[0]} dans 15 minutes.");
                                _isAlreadyAlerted.falert = true;
                            }
                            else if (timeLeft.Minutes == 5 && !_isAlreadyAlerted.salert)
                            {
                                await ChatUtils.PingMessage(channel, $"{eventSplitted[0]} dans 5 minutes.", channel.Guild.EveryoneRole);
                                _isAlreadyAlerted.salert = true;
                            }
                        }
                        else if ((timeLeft.Hours != 0 || timeLeft.Minutes != 10) && _isAlreadyAlerted.falert && _isAlreadyAlerted.salert)
                        {
                            _isAlreadyAlerted.falert = false;
                            _isAlreadyAlerted.salert = false;
                        }
                    }
                }
                else
                {
                    await ReloadEdt().ConfigureAwait(true);
                    break;
                }
        }

        private async Task Log(LogMessage message)
        {
            Color color = Color.Default;
            switch (message.Severity)
            {
                case LogSeverity.Critical:
                case LogSeverity.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    color = Color.Red;
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
            Console.WriteLine($"{DateTime.Now,-19} [{message.Severity}] {message.Source}: {message.Message} {message.Exception}");
            if (_client.ConnectionState == ConnectionState.Connected && message.Severity < LogSeverity.Info)
            {
                //PM me the start timestamps
                await (await _client.GetUser(151261754704265216).GetOrCreateDMChannelAsync()).SendMessageAsync(embed: new EmbedBuilder()
                {
                    Title = "Something went wrong",
                    Description = "*" + message.Source + "*\n" + message.Message,
                    Timestamp = DateTimeOffset.Now,
                    Color = color,
                    Footer = new EmbedFooterBuilder() { Text = "by OxyTom#1831" }
                }
                .WithAuthor(_client.CurrentUser)
                .Build()).ConfigureAwait(true);
            }
            Console.ResetColor();
        }
    }
}