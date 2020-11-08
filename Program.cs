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
        public static List<Edt> edts = new List<Edt>();
        public static ServiceProvider services;

        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

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
                .BuildServiceProvider();
            _client = services.GetRequiredService<DiscordSocketClient>();

            _client.Log += Log;
            services.GetRequiredService<CommandService>().Log += Log;

            _client.Ready += async () =>
            {
                // Log that we're ready
                await Log(new LogMessage(LogSeverity.Info, "Ready", $"Connected as {_client.CurrentUser} on {_client.Guilds.Count} servers"));

#if !DEBUG
                // Bypass the condition in previous statement
                await LogOnDiscord("Hello world", "I've just awoken my master !", Color.Green, new List<EmbedFieldBuilder>
                {
                    new EmbedFieldBuilder() { Name = "Launch platform", Value = Environment.OSVersion + "\nat " + DateTime.Now.ToString("HH:mm:ss") }
                }).ConfigureAwait(true);
#endif
            };

            // Setup and starting the bot
            await _client.LoginAsync(TokenType.Bot, File.ReadLines("token.txt").First());
            await _client.StartAsync();
            try
            {
                await services.GetRequiredService<CommandHandler>().InstallCommandsAsync();
            }
            catch (Exception e) { await Log(new LogMessage(LogSeverity.Error, "InstallCommands", e.Message, e)); }

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

        private static readonly string[] edtCodes = { "4352c5485001785", "1c57595e2401824" };
        private RestUserMessage[] _edtMessages = new RestUserMessage[edtCodes.Length];
        private bool _edtIsSundayAlreadyPosted = false;

        public async Task ReloadEdt()
        {
            while (edts.Count < edtCodes.Length) // If the timetable doesn't exist
                edts.Add(new Edt());

            await Task.WhenAll(edtCodes.Select(async (codes, i) =>
            {
                try
                {
                    // Setup a new WebClient for each task
                    using var client = new WebClient();
                    var task = client.DownloadStringTaskAsync(GetJSONUriFromCode(codes));

                    // Download the JSON in a specified delay
                    if (await Task.WhenAny(task, Task.Delay(Timeout)) == task)
                        try
                        {
                            var json = task.Result;
                            //! A JSON update occurs only if there's a change in the current week
                            bool isJsonUpdated = edts[i].RawJsonCode == 0 || json.GetHashCode(StringComparison.OrdinalIgnoreCase) != edts[i].RawJsonCode;
                            if (isJsonUpdated)
                            {
                                // Converts the JSON to Objects
                                edts[i] = JsonConvert.DeserializeObject<Edt>(json);
                                edts[i].RawJsonCode = json.GetHashCode(StringComparison.OrdinalIgnoreCase);
                            }

                            int offset = 0;
                            bool isEdtDownloaded = isJsonUpdated || (DateTime.Today.DayOfWeek == DayOfWeek.Sunday && !_edtIsSundayAlreadyPosted);
                            if (isEdtDownloaded)
                            {
                                // Download the table
                                if (DateTime.Today.DayOfWeek == DayOfWeek.Sunday)
                                {
                                    _edtIsSundayAlreadyPosted = true;
                                    offset = 1;
                                }

                                await client.DownloadFileTaskAsync(GetIMGUriFromCode(edtCodes[i], offset), edtCodes[i] + ".png");
                            }

                            if (isEdtDownloaded) //TODO: Force upload
                            {
                                // Send it to the correct channel
                                if (_edtMessages[i] != null)
                                    await _edtMessages[i].DeleteAsync();
#if DEBUG
                                _edtMessages[i] = await Sandbox.main
#else
                                _edtMessages[i] = await Apsu.edts[i]
#endif
                                .SendFileAsync(
                                    edtCodes[i] + ".png", "",
                                    embed: ChatUtils.CreateEmbed(
                                        new EmbedBuilder()
                                        {
                                            Title = ":date: Groupe 3." + (i + 1),
                                            Description = $"Semaine du {DateTime.Today.AddDays(offset * 7).StartOfWeek(DayOfWeek.Monday):dd/MM} au {DateTime.Today.AddDays(offset * 7).EndOfWeek(DayOfWeek.Monday):dd/MM}.",
                                            Fields = new List<EmbedFieldBuilder>() {
                                                new EmbedFieldBuilder() { IsInline = true, Name="Généré", Value = "par [Wildgoat#6969](https://github.com/WildGoat07)" },
                                                new EmbedFieldBuilder() { IsInline = true, Name="avec :hearts:", Value = $"[Lien direct]({GetIMGUriFromCode(edtCodes[i])})" }
                                            },
                                            ImageUrl = $"attachment://{edtCodes[i]}.png"
                                        }
                                    )
                                );
                            }
                        }
                        catch (Exception e) { await Log(new LogMessage(LogSeverity.Error, "ReloadEdt", e.Message, e)); }
                    else
                        throw new TimeoutException("Can't get distant JSON");
                    // If today is not a Sunday, allow edts update from Sundays
                    if (DateTime.Today.DayOfWeek != DayOfWeek.Sunday)
                        _edtIsSundayAlreadyPosted = false;
                }
                catch (Exception e) { await Log(new LogMessage(LogSeverity.Error, "ReloadEdt", e.Message, e)); }
            }));

            static Uri GetJSONUriFromCode(string id) => new Uri("http://wildgoat.fr/api/ical-json.php?url=" + System.Web.HttpUtility.UrlEncode("https://dptinfo.iutmetz.univ-lorraine.fr/lna/agendas/ical.php?ical=" + id) + "&week=1");
            static Uri GetIMGUriFromCode(string id, int offset = 0) => new Uri("http://wildgoat.fr/api/ical-png.php?url=" + System.Web.HttpUtility.UrlEncode("https://dptinfo.iutmetz.univ-lorraine.fr/lna/agendas/ical.php?ical=" + id) + "&regex=" + Uri.EscapeDataString("/^(.*)-.*-.*-.*$/") + "&offset=" + offset);
        }

        private (bool falert, bool salert) _isAlreadyAlerted = (false, false);
        private DateTime _lastUpdate;

        public void AlertStudents(object sender, System.Timers.ElapsedEventArgs e)
        {
            Task.Run(async () =>
            {
                try
                {
                    if (_lastUpdate.AddHours(2) <= DateTime.Now)
                    {
                        _lastUpdate = DateTime.Now;
                        await ReloadEdt();
                    }
                    foreach (var edt in edts) //TODO: Tasks ?
                    {
                        if (edt.Weeks[0].Days.Count >= (int)DateTime.Today.DayOfWeek)
                        {
                            var day = edt.Weeks[0].Days[(int)DateTime.Today.DayOfWeek - 1];
                            SocketTextChannel channel;
#if DEBUG
                            channel = Sandbox.main;
#endif
                            foreach (var evnt in day.Events) //TODO: Tasks ?
                            {
                                var timeLeft = evnt.Dtstart.Subtract(DateTime.Now);
                                if (timeLeft.Hours == 0 && timeLeft.Minutes <= 15 && !(_isAlreadyAlerted.falert && _isAlreadyAlerted.salert))
                                {
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
                    }
                }
                catch (Exception e) { await Log(new LogMessage(LogSeverity.Error, "AlertStudents", e.Message, e)); }
            });
        }

        private async Task Log(LogMessage message)
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
            Console.WriteLine($"{DateTime.Now,-19} [{message.Severity}] {message.Source}: {message.Message} {message.Exception.StackTrace}");
            if (_client.ConnectionState == ConnectionState.Connected && message.Severity < LogSeverity.Info)
                await LogOnDiscord("Something went wrong", "*" + message.Source + "*\n" + message.Message, color, isImportant: isImp).ConfigureAwait(true);
            Console.ResetColor();
        }

        private async Task LogOnDiscord(string title, string message, Color color, List<EmbedFieldBuilder> fields = null, bool isImportant = false)
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