using Assembly_Bot.Models;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assembly_Bot
{
    internal class Behaviour
    {
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
                catch (Exception e) { await Program.services.GetRequiredService<Logs>().Log(new LogMessage(LogSeverity.Error, "AlertStudents", e.Message, e)); }
            });
        }

        public async Task GroupChatToClean(SocketUser user, SocketVoiceState oldVoiceState, SocketVoiceState newVoiceState)
        {
            if (newVoiceState.VoiceChannel != null
                && (newVoiceState.VoiceChannel.Guild.Id == Apsu.server.Id || newVoiceState.VoiceChannel.Guild.Id == Sandbox.server.Id))
            // Just activate this functionality on the APSU and my test server
            {
#if DEBUG
                if (newVoiceState.VoiceChannel.Name.StartsWith("VocalABot"))
                {
                    var channel = newVoiceState.VoiceChannel.Guild.TextChannels.First(chan => chan.Name == newVoiceState.VoiceChannel.Name.ToLower());
                    await channel.AddPermissionOverwriteAsync(user, new OverwritePermissions(viewChannel: PermValue.Allow));
                }
#else
                if (newVoiceState.VoiceChannel.Name.StartsWith("Duo") || newVoiceState.VoiceChannel.Name.StartsWith("Trio") || newVoiceState.VoiceChannel.Name.StartsWith("Quatuor"))
                {
                    var channel = newVoiceState.VoiceChannel.Guild.TextChannels.First(chan => chan.Name == newVoiceState.VoiceChannel.Name.ToLower());
                    await channel.AddPermissionOverwriteAsync(user, new OverwritePermissions(viewChannel: PermValue.Allow));
                }
#endif
            }
            if (oldVoiceState.VoiceChannel != null
                && (oldVoiceState.VoiceChannel.Guild.Id == Apsu.server.Id || oldVoiceState.VoiceChannel.Guild.Id == Sandbox.server.Id))
            // Just activate this functionality on the APSU and my test server
            {
#if DEBUG
                if (oldVoiceState.VoiceChannel.Name.StartsWith("VocalABot") && oldVoiceState.VoiceChannel.Users.Count == 0)
                {
                    var channel = oldVoiceState.VoiceChannel.Guild.TextChannels.First(chan => chan.Name == oldVoiceState.VoiceChannel.Name.ToLower());
                    await channel.RemovePermissionOverwriteAsync(user);
                    while (await channel.GetMessagesAsync(1).FlattenAsync() != null)
                    {
                        await ChatUtils.CleanChannel(channel, 1);
                    }
                }
#else
                if ((oldVoiceState.VoiceChannel.Name.StartsWith("Duo") || oldVoiceState.VoiceChannel.Name.StartsWith("Trio") || oldVoiceState.VoiceChannel.Name.StartsWith("Quatuor")) && oldVoiceState.VoiceChannel.Users.Count == 0)
                {
                    var channel = oldVoiceState.VoiceChannel.Guild.TextChannels.First(chan => chan.Name == oldVoiceState.VoiceChannel.Name.ToLower());
                    await channel.RemovePermissionOverwriteAsync(user);
                    while (await channel.GetMessageAsync(1) != null)
                    {
                        await ChatUtils.CleanChannel(channel, 100);
                    }
                }
#endif
            }
        }
    }
}