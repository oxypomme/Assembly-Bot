using Assembly_Bot.Extensions;
using Assembly_Bot.Models;
using Discord;
using Discord.Rest;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Assembly_Bot
{
    internal class Edt
    {
        public List<Models.Edt> edts = new List<Models.Edt>();

        private static readonly string[] edtCodes = { "4352c5485001785", "1c57595e2401824" };

        private RestUserMessage[] _edtMessages = new RestUserMessage[edtCodes.Length];
        private bool _edtIsSundayAlreadyPosted;

        private Logs _logger;

        public Edt(IServiceProvider services)
        {
            _logger = services.GetRequiredService<Logs>();
        }

        public async Task ReloadEdt(bool forceJSON = false, bool forceDOWN = false)
        {
            while (edts.Count < edtCodes.Length) // If the timetable doesn't exist
                edts.Add(new Models.Edt());

            await Task.WhenAll(edtCodes.Select(async (code, i) =>
            {
                try
                {
                    // Setup a new WebClient for each task
                    using var client = new HttpClient();
                    var task = client.GetAsync(GetJSONUriFromCode(code));

                    // Download the JSON in a specified delay
                    if (await Task.WhenAny(task, Task.Delay(Program.Timeout)) == task)
                        try
                        {
                            var json = await task.Result.Content.ReadAsStringAsync();
                            //! A JSON update occurs only if there's a change in the current week or a force update
                            bool isJsonUpdated = forceJSON || edts[i].RawJsonCode == 0 || json.GetHashCode(StringComparison.OrdinalIgnoreCase) != edts[i].RawJsonCode;
                            if (isJsonUpdated)
                            {
                                // Converts the JSON to Objects
                                edts[i] = JsonConvert.DeserializeObject<Models.Edt>(json);
                                edts[i].RawJsonCode = json.GetHashCode(StringComparison.OrdinalIgnoreCase);
                            }

                            int offset = 0;
                            string imgGeneretadApi = "";
                            bool isEdtDownloaded = forceDOWN || isJsonUpdated || (DateTime.Today.DayOfWeek == DayOfWeek.Sunday && !_edtIsSundayAlreadyPosted);
                            if (isEdtDownloaded)
                            {
                                // Download the table
                                if (DateTime.Today.DayOfWeek == DayOfWeek.Sunday)
                                {
                                    _edtIsSundayAlreadyPosted = true;
                                    offset = 1;
                                }

                                var imgTask = client.GetAsync(GetIMGUriFromCode(code, offset));
                                if (await Task.WhenAny(imgTask, Task.Delay(Program.Timeout)) == imgTask)
                                {
                                    using (Stream httpStream = await imgTask.Result.Content.ReadAsStreamAsync(), imgStream = new FileStream(code + ".png", FileMode.Create, FileAccess.Write))
                                        await httpStream.CopyToAsync(imgStream);
                                    imgGeneretadApi = imgTask.Result.RequestMessage.RequestUri.ToString();
                                }
                            }

                            if (isEdtDownloaded) //TODO: force upload
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
                                                new EmbedFieldBuilder() { IsInline = true, Name="avec :hearts:", Value = imgGeneretadApi != "" ? $"[Lien direct]({imgGeneretadApi})" : ":hearts:" }
                                            },
                                            ImageUrl = $"attachment://{code}.png"
                                        }
                                    )
                                );
                            }
                        }
                        catch (Exception e) { await _logger.Log(new LogMessage(LogSeverity.Error, "ReloadEdt", e.Message, e)); }
                    else
                        throw new TimeoutException("Can't get distant JSON");
                }
                catch (Exception e) { await _logger.Log(new LogMessage(LogSeverity.Error, "ReloadEdt", e.Message, e)); }
            }));
            // If today is not a Sunday, allow edts update from Sundays
            if (DateTime.Today.DayOfWeek != DayOfWeek.Sunday)
                _edtIsSundayAlreadyPosted = false;

            static Uri GetJSONUriFromCode(string id) => new Uri("http://wildgoat.fr/api/ical-json.php?url=" + System.Web.HttpUtility.UrlEncode("https://dptinfo.iutmetz.univ-lorraine.fr/lna/agendas/ical.php?ical=" + id) + "&week=1");
            static Uri GetIMGUriFromCode(string id, int offset = 0) => new Uri("http://wildgoat.fr/api/ical-png.php?url=" + System.Web.HttpUtility.UrlEncode("https://dptinfo.iutmetz.univ-lorraine.fr/lna/agendas/ical.php?ical=" + id) + "&regex=" + System.Web.HttpUtility.UrlEncode("/^(.*) ?- ?.* ?- ?.* ?- ?(.*)$/") + "&offset=" + offset);
        }
    }
}