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

    public class Week
    {
        [JsonPropertyName("days")]
        public List<Day> Days { get; set; }
    }

    public class Day
    {
        [JsonPropertyName("events")]
        public List<Event> Events { get; set; }
    }

    public class Event
    {
        [JsonPropertyName("summary")]
        public string Summary { get; set; }

        public string dtstart;

        [JsonIgnore]
        public DateTime Dtstart
        {
            get
            {
                return DateTime.ParseExact(dtstart, "yyyyMMddTHHmmss", null);
            }
            set => dtstart = value.ToString("yyyyMMddTHHmmss");
        }

        public string dtend;

        [JsonIgnore]
        public DateTime Dtend
        {
            get
            {
                return DateTime.ParseExact(dtend, "yyyyMMddTHHmmss", null);
            }
            set => dtend = value.ToString("yyyyMMddTHHmmss");
        }

        [JsonPropertyName("location")]
        public string Location { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("transp")]
        public string Transp { get; set; }

        public override string ToString()
        {
            return Summary + " @" + Dtstart + " -> " + Dtend;
        }
    }
}