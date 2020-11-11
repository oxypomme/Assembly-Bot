using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assembly_Bot.Modules
{
    [Group("temp-chan")]
    [Summary("Temporary Channels Commands : `temp-chan`")]
    public class TempChanModule : ModuleBase<SocketCommandContext>
    {
        private Logs _logger;
        private Behaviour _behave;

        public TempChanModule(IServiceProvider service)
        {
            _logger = service.GetRequiredService<Logs>();
            _behave = service.GetRequiredService<Behaviour>();
        }

        [Command("create")]
        [Alias("new")]
        [Summary("Ask Temp-chan to create a private category.")]
        public async Task CreatePrivAsync(
            [Summary("The name of the channels")] string name = "",
            [Summary("The maximum users in the voice channel")] int maxUsers = 0,
            [Summary("The users to invite with you")] params SocketGuildUser[] users)
        {
            IUserMessage initMessage = null;
            RestCategoryChannel categ = null;
            RestTextChannel tChan = null;
            RestVoiceChannel vChan = null;
            try
            {
                var userList = new HashSet<SocketGuildUser>(users);

                initMessage = await ReplyAsync("", embed: ChatUtils.CreateEmbed("Temp-chan", "J'arrive ! `(\\*>﹏<\\*)′", Color.Purple));
                string categName = "tmp-" + name;
                if (name == "")
                    do
                    {
                        var rnd = new Random();
                        name = categName = "tmp-" + rnd.Next(100, 1000);
                    } while (Context.Guild.CategoryChannels.Any(cat => cat.Name.Contains(name, StringComparison.OrdinalIgnoreCase)));
                else if (Context.Guild.CategoryChannels.Any(cat => cat.Name.Contains(name, StringComparison.OrdinalIgnoreCase)))
                    throw new System.Data.DuplicateNameException("Le nom existe déjà ㄟ(≧◇≦)ㄏ");

                if (!userList.Contains(Context.User))
                    userList.Add((SocketGuildUser)Context.User);

                categ = await Context.Guild.CreateCategoryChannelAsync(categName);
                await categ.AddPermissionOverwriteAsync(Context.User, new(manageChannel: PermValue.Allow, manageMessages: PermValue.Allow, muteMembers: PermValue.Allow, deafenMembers: PermValue.Allow));

                tChan = await Context.Guild.CreateTextChannelAsync(name, c =>
                {
                    c.Topic = "Channel temporaire de " + Context.User.Mention;
                    c.CategoryId = categ.Id;
                });

                vChan = await Context.Guild.CreateVoiceChannelAsync(name, c =>
                {
                    c.UserLimit = maxUsers > 0 ? (int?)maxUsers : null;
                    c.CategoryId = categ.Id;
                });

                var userEmbed = new List<EmbedFieldBuilder>()
                {
                    new() {Name = "Nom", Value = name, IsInline = true },
                    new() {Name = "Avec" }
                };
                foreach (var user in userList)
                {
                    await categ.AddPermissionOverwriteAsync(user, new(viewChannel: PermValue.Allow));
                    await user.ModifyAsync(u => u.Channel = vChan);
                    userEmbed[1].WithValue(userEmbed[1].Value + (userEmbed[1].Value is not null ? ", " : "") + user.Mention);
                }
                if (maxUsers > 0)
                    userEmbed.Add(new() { Name = "Maximum", Value = maxUsers, IsInline = true });
                await ReplyAsync("", embed: ChatUtils.CreateEmbed("Temp-chan", "Houra ! Vous êtes dans votre salon ! o(\\*^＠^\\*)o", Color.Green, userEmbed));
            }
            catch (System.Data.DuplicateNameException e)
            {
                await ReplyAsync("", embed: ChatUtils.CreateEmbed("Temp-chan", e.Message, Color.Red));
            }
            catch (Exception e)
            {
                try
                {
                    await (categ?.DeleteAsync() ?? Task.CompletedTask);
                    await (tChan?.DeleteAsync() ?? Task.CompletedTask);
                    await (vChan?.DeleteAsync() ?? Task.CompletedTask);
                }
                catch (Exception ex) { await _logger.Log(new(LogSeverity.Error, "TempChan - Cleanup", e.Message, ex)); }
                finally
                {
                    await ReplyAsync("", embed: ChatUtils.CreateEmbed("Temp-chan", "OOPS, quelque chose est arrivé .·´¯\\`(>▂<)´¯\\`·. ", Color.Red));
                    await _logger.Log(new(LogSeverity.Error, "TempChan", e.Message, e));
                }
            }
            finally
            {
                await (initMessage?.DeleteAsync() ?? Task.CompletedTask);
                await Context.Message.DeleteAsync();
            }
        }

        [Command("delete")]
        [Summary("Ask Temp-chan to delete a private category.")]
        [Priority(1)]
        public async Task RemovePrivAsync([Summary("The id of the category or any of the channels")] ulong id)
        {
            IUserMessage initMessage = null;
            try
            {
                initMessage = await ReplyAsync("", embed: ChatUtils.CreateEmbed("Temp-chan", "J'arrive ! `(\\*>﹏<\\*)′", Color.Purple));
                SocketGuildChannel toDel = Context.Guild.GetCategoryChannel(id);
                if (toDel == null)
                    toDel = Context.Guild.GetChannel(id);
                if (toDel == null)
                    throw new KeyNotFoundException("Désolé, j'ai pas trouvé ＞﹏＜");

                SocketVoiceChannel channel;
                if (toDel is SocketCategoryChannel categ && categ.Name.StartsWith("tmp-", StringComparison.OrdinalIgnoreCase))
                    channel = categ.Channels.First(c => c is SocketVoiceChannel) as SocketVoiceChannel;
                else if (toDel is SocketTextChannel textChannel)
                    channel = ((SocketCategoryChannel)textChannel.Category).Channels.First(c => c is SocketVoiceChannel && string.Equals(c.Name, toDel.Name, StringComparison.OrdinalIgnoreCase)) as SocketVoiceChannel;
                else if (toDel is SocketVoiceChannel voiceChannel)
                    channel = voiceChannel;
                else
                    throw new ArrayTypeMismatchException("AH MAIS QU'EST-CE QUE C'EST QUE CE TRUC ! (╬▔皿▔)╯");

                await _behave.ClearTempChans(channel);

                const int delay = 5000;
                var msg = await ReplyAsync("", embed: ChatUtils.CreateEmbed("Temp-chan", $"J'ai nettoyé `{channel.Name}` ! (✿◡‿◡)", Color.Green));
                await Task.Delay(delay);
                await msg.DeleteAsync();
            }
            catch (Exception e)
            {
                await ReplyAsync("", embed: ChatUtils.CreateEmbed("Temp-chan", e.Message, Color.Red));
                await _logger.Log(new(LogSeverity.Error, "TempChan", e.Message, e));
            }
            finally
            {
                await (initMessage?.DeleteAsync() ?? Task.CompletedTask);
                await Context.Message.DeleteAsync();
            }
        }

        [Command("delete")]
        [Summary("Ask Temp-chan to delete a private category.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task RemovePrivAsync([Summary("The name of the category or any of the channels")] string name)
        {
            IUserMessage initMessage = null;
            try
            {
                initMessage = await ReplyAsync("", embed: ChatUtils.CreateEmbed("Temp-chan", "J'arrive ! `(\\*>﹏<\\*)′", Color.Purple));
                SocketCategoryChannel category = null;
                foreach (var cat in Context.Guild.CategoryChannels)
                    if (cat.Name.StartsWith("tmp-") && cat.Name.Contains(name, StringComparison.OrdinalIgnoreCase))
                    {
                        category = cat;
                        break;
                    }
                if (category == null)
                    throw new KeyNotFoundException("Désolé, j'ai pas trouvé ＞﹏＜");
                await _behave.ClearTempChans(category.Channels.First(c => c is SocketVoiceChannel) as SocketVoiceChannel);

                const int delay = 5000;
                var msg = await ReplyAsync("", embed: ChatUtils.CreateEmbed("Temp-chan", $"J'ai nettoyé `{name}` ! (✿◡‿◡)", Color.Green));
                await Task.Delay(delay);
                await msg.DeleteAsync();
            }
            catch (Exception e)
            {
                await ReplyAsync("", embed: ChatUtils.CreateEmbed("Temp-chan", e.Message, Color.Red));
                await _logger.Log(new(LogSeverity.Error, "TempChan", e.Message, e));
            }
            finally
            {
                await (initMessage?.DeleteAsync() ?? Task.CompletedTask);
                await Context.Message.DeleteAsync();
            }
        }

        [Command("delete")]
        [Alias("clean", "clear", "prune")]
        [Summary("Ask Temp-chan to delete all privates category.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task RemovePrivAsync()
        {
            IUserMessage initMessage = null;
            try
            {
                initMessage = await ReplyAsync("", embed: ChatUtils.CreateEmbed("Temp-chan", "J'arrive ! `(\\*>﹏<\\*)′", Color.Purple));
                foreach (var chan in from categ in Context.Guild.CategoryChannels
                                     where categ.Name.StartsWith("tmp-")
                                     from chan in categ.Channels
                                     where chan is SocketVoiceChannel
                                     select chan)
                    await _behave.ClearTempChans((SocketVoiceChannel)chan);

                const int delay = 5000;
                var msg = await ReplyAsync("", embed: ChatUtils.CreateEmbed("Temp-chan", "C'est tout beau tout propre ! (✿◡‿◡)", Color.Green));
                await Task.Delay(delay);
                await msg.DeleteAsync();
            }
            catch (Exception e)
            {
                await ReplyAsync("", embed: ChatUtils.CreateEmbed("Temp-chan", e.Message, Color.Red));
                await _logger.Log(new(LogSeverity.Error, "TempChan", e.Message, e));
            }
            finally
            {
                await (initMessage?.DeleteAsync() ?? Task.CompletedTask);
                await Context.Message.DeleteAsync();
            }
        }

        [Command("add")]
        [Summary("Ask Temp-chan to add users your private channel")]
        public async Task AddPrivAsync([Summary("The users to invite with you")] params SocketGuildUser[] users)
        {
            //TODO: check if Context.User is "Admin"
        }

        [Command("kick")]
        [Summary("Ask Temp-chan to kick an user from your private channel")]
        public async Task KickPrivAsync([Summary("The user to kick")] SocketGuildUser user)
        {
            //TODO: check if Context.User is "Admin"
        }

        [Command("edit")]
        [Summary("Ask Temp-chan to edit your private channel")]
        public async Task EditPrivAsync(
            [Summary("The new name of the channel")] string name = "",
            [Summary("The new max user of the channel")] int maxUsers = 0)
        {
            //TODO: check if Context.User is "Admin"
        }

        [Command("join")]
        [Alias("ask")]
        [Summary("Ask Temp-chan if you can join a private channel")]
        public async Task JoinPrivAsync([Summary("The name of the channel or the category")] string name)
        {
        }
    }
}