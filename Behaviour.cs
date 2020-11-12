using Assembly_Bot.Models;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Assembly_Bot
{
    internal class Behaviour
    {
        private (bool falert, bool salert) _isAlreadyAlerted = (false, false);
        private DateTime _lastUpdate;

        private Edt _edt;
        private Logs _logger;

        public Behaviour(IServiceProvider services)
        {
            _edt = services.GetRequiredService<Edt>();
            _logger = services.GetRequiredService<Logs>();
        }

        public void AlertStudents(object sender, System.Timers.ElapsedEventArgs e)
        {
            Task.Run(async () =>
            {
                try
                {
                    if (_lastUpdate.AddHours(2) <= DateTime.Now)
                    {
                        _lastUpdate = DateTime.Now;
#if !DEBUG
                        await _edt.ReloadEdt();
#endif
                    }
                    /* Removed because useless + buggy
                    SocketTextChannel channel;
                    foreach (var edt in _edt.edts) //TODO: Tasks ?
                    {
                        Day day; // DayOfWeek.Sunday = 0, or in the JSON, Sunday is the 7th day
                        if (DateTime.Today.DayOfWeek == DayOfWeek.Sunday)
                            day = edt.Weeks[0].Days[6]; // Get the real Sunday
                        else
                            day = edt.Weeks[0].Days[(int)DateTime.Today.DayOfWeek - 1]; // Get the day
#if DEBUG
                        channel = Sandbox.main;
#endif
                        foreach (var evnt in day.Events) //TODO: Tasks ?
                        {
                            var timeLeft = evnt.Dtstart.Subtract(DateTime.Now);
                            if ((timeLeft.Hours == 0 && timeLeft.Minutes <= 15) && !(_isAlreadyAlerted.falert && _isAlreadyAlerted.salert))
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
                                if ((timeLeft.Minutes == 15) && !_isAlreadyAlerted.falert)
                                {
                                    _isAlreadyAlerted.falert = true;
                                    await ChatUtils.PingMessage(channel, $"{eventSplitted[0]} dans 15 minutes.");
                                }
                                else if ((timeLeft.Minutes == 5) && !_isAlreadyAlerted.salert)
                                {
                                    _isAlreadyAlerted.salert = true;
                                    await ChatUtils.PingMessage(channel, $"{eventSplitted[0]} dans 5 minutes.", channel.Guild.EveryoneRole);
                                }
                            }
                            else if ((timeLeft.Hours != 0 || timeLeft.Minutes != 15) && _isAlreadyAlerted.falert && _isAlreadyAlerted.salert)
                            {
                                _isAlreadyAlerted.falert = false;
                                _isAlreadyAlerted.salert = false;
                            }
                        }
                    }
                    */
                }
                catch (Exception e) { await _logger.Log(new(LogSeverity.Error, "AlertStudents", e.Message, e)); }
            });
        }

        public async Task GroupChatToClean(SocketUser user, SocketVoiceState oldVoiceState, SocketVoiceState newVoiceState)
        {
            if (newVoiceState.VoiceChannel is not null
                && (newVoiceState.VoiceChannel.Guild.Id == Apsu.server.Id || newVoiceState.VoiceChannel.Guild.Id == Sandbox.server.Id))
            // Just activate this functionality on the APSU and my test server
            {
#if DEBUG
                if (newVoiceState.VoiceChannel.Name.StartsWith("VocalABot"))
                {
                    var channel = newVoiceState.VoiceChannel.Guild.TextChannels.First(chan => string.Equals(chan.Name, newVoiceState.VoiceChannel.Name, StringComparison.OrdinalIgnoreCase));
                    await channel.AddPermissionOverwriteAsync(user, new(viewChannel: PermValue.Allow));
                }
#else
                if (newVoiceState.VoiceChannel.Name.StartsWith("Duo") || newVoiceState.VoiceChannel.Name.StartsWith("Trio") || newVoiceState.VoiceChannel.Name.StartsWith("Quatuor"))
                {
                    var channel = newVoiceState.VoiceChannel.Guild.TextChannels.First(chan => string.Equals(chan.Name, newVoiceState.VoiceChannel.Name, StringComparison.OrdinalIgnoreCase));
                    await channel.AddPermissionOverwriteAsync(user, new(viewChannel: PermValue.Allow));
                }
#endif
            }
            if (oldVoiceState.VoiceChannel is not null
                && (oldVoiceState.VoiceChannel.Guild.Id == Apsu.server.Id || oldVoiceState.VoiceChannel.Guild.Id == Sandbox.server.Id))
            // Just activate this functionality on the APSU and my test server
            {
#if !DEBUG
                if (oldVoiceState.VoiceChannel.Name.StartsWith("VocalABot"))
                {
                    var channel = oldVoiceState.VoiceChannel.Guild.TextChannels.First(chan => string.Equals(chan.Name, oldVoiceState.VoiceChannel.Name, StringComparison.OrdinalIgnoreCase));
                    await channel.RemovePermissionOverwriteAsync(user);

                    if (oldVoiceState.VoiceChannel.Users.Count == 0)
                        while (await channel.GetMessagesAsync(1).FlattenAsync() is not null)
                            await ChatUtils.CleanChannel(channel, 1);
                }
#else
                if ((oldVoiceState.VoiceChannel.Name.StartsWith("Duo") || oldVoiceState.VoiceChannel.Name.StartsWith("Trio") || oldVoiceState.VoiceChannel.Name.StartsWith("Quatuor")))
                {
                    var channel = oldVoiceState.VoiceChannel.Guild.TextChannels.First(chan => string.Equals(chan.Name, oldVoiceState.VoiceChannel.Name, StringComparison.OrdinalIgnoreCase));
                    await channel.RemovePermissionOverwriteAsync(user);

                    if (oldVoiceState.VoiceChannel.Users.Count == 0)
                        while (await channel.GetMessageAsync(1) is not null)
                            await ChatUtils.CleanChannel(channel, 100);
                }
#endif
            }
        }

        internal async Task ClearTempChans(SocketVoiceChannel vchannel)
        {
            if (vchannel is not null && vchannel.Category.Name.StartsWith("tmp-") && vchannel.Users.Count == 0)
            {
                // Delete text channel
                await vchannel.Guild.TextChannels.First(chan => string.Equals(chan.Name, vchannel.Name, StringComparison.OrdinalIgnoreCase)).DeleteAsync();
                // Ensure that we delete the category in the end
                var cat = vchannel.Category;
                // Delete voice channel
                await vchannel.DeleteAsync();
                // Finally delete the category
                await cat.DeleteAsync();
            }
        }
    }
}