using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Assembly_Bot.Models
{
    public class Edt
    {
        [JsonPropertyName("weeks")]
        public List<Week> Weeks { get; set; }

        [JsonPropertyName("success")]
        public string Success { get; set; }

        [JsonIgnore]
        public int RawJsonCode { get; set; }
    }

    public record Week
    {
        [JsonPropertyName("days")]
        public List<Day> Days { get; init; }
    }

    public record Day
    {
        [JsonPropertyName("events")]
        public List<Event> Events { get; init; }
    }

    public record Event()
    {
        [JsonPropertyName("summary")]
        public string Summary { get; init; }

        public string dtstart;

        [JsonIgnore]
        public DateTime Dtstart
        {
            get
            {
                return DateTime.ParseExact(dtstart, "yyyyMMddTHHmmss", null);
            }
            init => dtstart = value.ToString("yyyyMMddTHHmmss");
        }

        public string dtend;

        [JsonIgnore]
        public DateTime Dtend
        {
            get
            {
                return DateTime.ParseExact(dtend, "yyyyMMddTHHmmss", null);
            }
            init => dtend = value.ToString("yyyyMMddTHHmmss");
        }

        [JsonPropertyName("location")]
        public string Location { get; init; }

        [JsonPropertyName("description")]
        public string Description { get; init; }

        [JsonPropertyName("transp")]
        public string Transp { get; init; }

        public override string ToString() => Summary + " @" + Dtstart + " -> " + Dtend;
    }
}