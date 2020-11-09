﻿using Assembly_Bot.Models;
using Discord;
using Discord.Rest;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Assembly_Bot
{
    internal static class EdtUtils
    {
        public static List<Edt> edts = new List<Edt>();

        private static readonly string[] edtCodes = { "4352c5485001785", "1c57595e2401824" };

        private static RestUserMessage[] _edtMessages = new RestUserMessage[edtCodes.Length];
        private static bool _edtIsSundayAlreadyPosted;

        public static async Task ReloadEdt(bool forceJSON = false, bool forceUP = false)
        {
            while (edts.Count < edtCodes.Length) // If the timetable doesn't exist
                edts.Add(new Edt());

            await Task.WhenAll(edtCodes.Select(async (code, i) =>
            {
                try
                {
                    // Setup a new WebClient for each task
                    using var client = new WebClient();
                    var task = client.DownloadStringTaskAsync(GetJSONUriFromCode(code));

                    // Download the JSON in a specified delay
                    if (await Task.WhenAny(task, Task.Delay(Program.Timeout)) == task)
                        try
                        {
                            var json = task.Result;
                            //! A JSON update occurs only if there's a change in the current week or a force update
                            bool isJsonUpdated = forceJSON || edts[i].RawJsonCode == 0 || json.GetHashCode(StringComparison.OrdinalIgnoreCase) != edts[i].RawJsonCode;
                            if (isJsonUpdated)
                            {
                                // Converts the JSON to Objects
                                edts[i] = JsonConvert.DeserializeObject<Edt>(json);
                                edts[i].RawJsonCode = json.GetHashCode(StringComparison.OrdinalIgnoreCase);
                            }

                            int offset = 0;
                            bool isEdtDownloaded = forceUP || isJsonUpdated || (DateTime.Today.DayOfWeek == DayOfWeek.Sunday && !_edtIsSundayAlreadyPosted);
                            if (isEdtDownloaded)
                            {
                                // Download the table
                                if (DateTime.Today.DayOfWeek == DayOfWeek.Sunday)
                                {
                                    _edtIsSundayAlreadyPosted = true;
                                    offset = 1;
                                }

                                await client.DownloadFileTaskAsync(GetIMGUriFromCode(code, offset), code + ".png");
                            }

                            if (isEdtDownloaded)
                            {
                                // Send it to the correct channel
                                if (_edtMessages[i] != null)
                                    await _edtMessages[i].DeleteAsync();
#if DEBUG
                                else
                                    await ChatUtils.CleanChannel(Sandbox.main, 5);
                                _edtMessages[i] = await Sandbox.main
#else
                                else
                                    await ChatUtils.CleanChannel(Apsu.edts[i], 5);
                                _edtMessages[i] = await Apsu.edts[i]
#endif
                                .SendFileAsync(
                                    code + ".png", "",
                                    embed: ChatUtils.CreateEmbed(
                                        new EmbedBuilder()
                                        {
                                            Title = ":date: Groupe 3." + (i + 1),
                                            Description = $"Semaine du {DateTime.Today.AddDays(offset * 7).StartOfWeek(DayOfWeek.Monday):dd/MM} au {DateTime.Today.AddDays(offset * 7).EndOfWeek(DayOfWeek.Monday):dd/MM}.",
                                            Fields = new List<EmbedFieldBuilder>() {
                                                new EmbedFieldBuilder() { IsInline = true, Name="Généré", Value = "par [Wildgoat#6969](https://github.com/WildGoat07)" },
                                                new EmbedFieldBuilder() { IsInline = true, Name="avec :hearts:", Value = $"[Lien direct]({GetIMGUriFromCode(code, offset)})" }
                                            },
                                            ImageUrl = $"attachment://{code}.png"
                                        }
                                    )
                                );
                            }
                        }
                        catch (Exception e) { await Program.services.GetRequiredService<GlobalBehaviour>().Log(new LogMessage(LogSeverity.Error, "ReloadEdt", e.Message, e)); }
                    else
                        throw new TimeoutException("Can't get distant JSON");
                }
                catch (Exception e) { await Program.services.GetRequiredService<GlobalBehaviour>().Log(new LogMessage(LogSeverity.Error, "ReloadEdt", e.Message, e)); }
            }));
            // If today is not a Sunday, allow edts update from Sundays
            if (DateTime.Today.DayOfWeek != DayOfWeek.Sunday)
                _edtIsSundayAlreadyPosted = false;

            static Uri GetJSONUriFromCode(string id) => new Uri("http://wildgoat.fr/api/ical-json.php?url=" + System.Web.HttpUtility.UrlEncode("https://dptinfo.iutmetz.univ-lorraine.fr/lna/agendas/ical.php?ical=" + id) + "&week=1");
            static Uri GetIMGUriFromCode(string id, int offset = 0) => new Uri("http://wildgoat.fr/api/ical-png.php?url=" + System.Web.HttpUtility.UrlEncode("https://dptinfo.iutmetz.univ-lorraine.fr/lna/agendas/ical.php?ical=" + id) + "&regex=" + System.Web.HttpUtility.UrlEncode("/^(.*) ?- ?.* ?- ?.* ?- ?(.*)$/") + "&offset=" + offset);
        }
    }
}