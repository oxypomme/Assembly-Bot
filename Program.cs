using Discord;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace Assembly_Bot
{
    public class Program
    {
        private DiscordSocketClient _client;

        public static void Main(string[] args) => new Program().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
        {
            _client = new DiscordSocketClient();
            _client.Log += Log;
            await _client.LoginAsync(TokenType.Bot, System.IO.File.ReadAllText("token.txt")); //TODO: in a file pls
            await _client.StartAsync();
            await Task.Delay(-1);
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}