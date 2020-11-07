using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assembly_Bot.Modules
{
    public class HelpModule : ModuleBase<SocketCommandContext>
    {
        private readonly CommandService _commands;

        public HelpModule(CommandService commands)
        {
            _commands = commands;
        }

        [Command("help")]
        [Alias("h")]
        [Summary("Show every commands available")]
        public async Task HelpCommand()
        {
            var fields = new List<EmbedFieldBuilder>();
            // foreach module, creates a builder and...
            foreach (var (module, builder) in from module in _commands.Modules.Where(m => !m.Name.Contains("help", StringComparison.OrdinalIgnoreCase))
                                              let builder = new EmbedFieldBuilder().WithName(module.Summary ?? module.Name)
                                              select (module, builder))
            {
                //... foreach command in module, check if it doesn't exist in help yet. If it doesn't, create an entry
                foreach (var cmd in module.Commands.Where(cmd => builder.Value == null || !(builder.Value as string).Contains(cmd.Name)))
                    builder.Value += (builder.Value == null ? "" : ", ") + "`" + cmd.Name + "`";
                fields.Add(builder);
            }

            await ReplyAsync("", embed: ChatUtils.CreateEmbed("Help !", $"Prefix : `{CommandHandler.prefix}`\nFor more info about a command : `help [command]`", Color.DarkBlue, fields));
        }

        [Command("help")]
        [Alias("h")]
        [Summary("Show informations about a command")]
        public async Task HelpCommand([Summary("The command to explain")] string commandName)
        {
            var commands = _commands.Commands.Where(c => c.Name == commandName.ToLower());
            if (!commands.Any())
            {
                var errorMessage = await ReplyAsync("", embed: ChatUtils.CreateEmbed("", "There are no commands with that name.", Color.DarkBlue));
                await Task.Delay(5000); // 5 seconds
                await errorMessage.DeleteAsync();
                return;
            }

            var embedBuilder = new EmbedBuilder().WithColor(Color.DarkBlue);
            foreach (var cmd in commands)
            {
                embedBuilder.AddField("Command " + cmd.Name, "" +
                    string.Join("\n", cmd.Aliases.Select(a => "`" + CommandHandler.prefix + a + string.Join("", cmd.Parameters.Select(p => " [" + p.Name + "]")) + "`")) +
                    "\n__Summary :__ \n" +
                    "*" + (cmd.Summary ?? "No description available") + "*\n" +
                    (cmd.Parameters.Count() > 0
                        ? "__Parameters :__ " + string.Concat(cmd.Parameters.Select(p => "\n[" + p.Name + "] : *" + (p.Summary ?? "No description available") + "*")) + "\n"
                        : "") +
                    "");
            }
            await ReplyAsync("", embed: ChatUtils.CreateEmbed(embedBuilder));
        }
    }
}