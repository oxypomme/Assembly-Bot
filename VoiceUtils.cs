using Assembly_Bot.Models;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assembly_Bot
{
    public class VoiceUtils
    {
        public static async Task GroupChatToClean(SocketUser user, SocketVoiceState oldVoiceState, SocketVoiceState newVoiceState)
        {
            if (oldVoiceState.VoiceChannel != null
                && (oldVoiceState.VoiceChannel.Guild.Id == Apsu.server.Id || oldVoiceState.VoiceChannel.Guild.Id == Sandbox.server.Id))
                // Just activate this functionality on the APSU and my test server
#if !DEBUG
                if (oldVoiceState.VoiceChannel.Name.StartsWith("VocalABot") && oldVoiceState.VoiceChannel.Users.Count == 0)
                {
                    var channel = oldVoiceState.VoiceChannel.Guild.TextChannels.First(chan => chan.Name.EndsWith("assembly_bot"));
                    await ChatUtils.CleanChannel(channel, 1);
                }
#else
                if ((oldVoiceState.VoiceChannel.Name.StartsWith("Duo") || oldVoiceState.VoiceChannel.Name.StartsWith("Trio") || oldVoiceState.VoiceChannel.Name.StartsWith("Quatuor")) && oldVoiceState.VoiceChannel.Users.Count == 0)
                {
                    var channel = oldVoiceState.VoiceChannel.Guild.TextChannels.First(chan => chan.Name == oldVoiceState.VoiceChannel.Name.ToLower());
                    do
                    {
                        await ChatUtils.CleanChannel(channel, 100);
                    } while (await channel.GetMessageAsync(1) != null);
                }
#endif
        }
    }
}