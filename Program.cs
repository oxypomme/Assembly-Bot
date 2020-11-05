using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Assembly_Bot
{
    public class Program
    {
        private DiscordSocketClient _client;

        public Program()
        {
            //var edt = JsonConvert.DeserializeObject<Models.Edt>(new WebClient().DownloadString("http://wildgoat.fr/api/info-ical-json.php?url=" + System.Web.HttpUtility.UrlEncode("https%3A%2F%2Fdptinfo.iutmetz.univ-lorraine.fr%2Flna%2Fagendas%2Fical.php%3Fical%3D4352c5485001785") + "&week=1"));
            //foreach (var week in edt.Weeks)
            //    foreach (var day in week.Days)
            //        foreach (var evnt in day.Events)
            //            Console.WriteLine(evnt);
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

            _client.Ready += () =>
            {
                Log(new LogMessage(LogSeverity.Info, "Ready", $"Connected as {_client.CurrentUser} on {_client.Guilds.Count} servers"));
                return Task.CompletedTask;
            };

            await _client.LoginAsync(TokenType.Bot, System.IO.File.ReadLines("token.txt").First());
            await _client.StartAsync();

            await services.GetRequiredService<CommandHandler>().InstallCommandsAsync();

#if DEBUG
            await _client.SetGameAsync("je suis en labo aled !", type: ActivityType.CustomStatus);
#else
            await _client.SetGameAsync("Protecting the Assembly Project", type: ActivityType.CustomStatus);
#endif

            _client.UserVoiceStateUpdated += VoiceUtils.GroupChatToClean;

            await Task.Delay(-1);
        }

        private Task Log(LogMessage message)
        {
            switch (message.Severity)
            {
                case LogSeverity.Critical:
                case LogSeverity.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;

                case LogSeverity.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
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
            Console.ResetColor();

            return Task.CompletedTask;
        }
    }
}