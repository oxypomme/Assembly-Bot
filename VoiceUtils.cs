using Discord;
using Discord.Commands;
using Discord.WebSocket;
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
                && (oldVoiceState.VoiceChannel.Guild.Id == 773545167117746198 || oldVoiceState.VoiceChannel.Guild.Id == 436909627834368010))
                // Just activate this functionality on the APSU and my test server
#if DEBUG
                if (oldVoiceState.VoiceChannel.Name.StartsWith("VocalABot") && oldVoiceState.VoiceChannel.Users.Count == 0)
                {
                    var channel = oldVoiceState.VoiceChannel.Guild.TextChannels.First(chan => chan.Name.EndsWith("assembly_bot"));
                    await ChatUtils.CleanChannel(channel, 1);
                }
#else
                if ((oldVoiceState.VoiceChannel.Name.StartsWith("Duo") || oldVoiceState.VoiceChannel.Name.StartsWith("Trio") || oldVoiceState.VoiceChannel.Name.StartsWith("Quatuor")) && oldVoiceState.VoiceChannel.Users.Count == 0)
                {
                    var channel = oldVoiceState.VoiceChannel.Guild.TextChannels.First(chan => chan.Name == oldVoiceState.VoiceChannel.Name.ToLower());
                    await ChatUtils.CleanChannel(channel, 100);
                }
#endif
        }
    }
}