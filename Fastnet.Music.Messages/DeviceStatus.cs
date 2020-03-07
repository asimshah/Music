using Fastnet.Core;
using Fastnet.Music.Core;
using Newtonsoft.Json;
using System;
using System.Diagnostics;

namespace Fastnet.Music.Messages
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class TimeSpanToSeconds : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return true;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            //Debugger.Break();
            Debug.Assert(objectType == typeof(TimeSpan));
            var val = reader.Value;
            long seconds = 0L;
            if (val != null)
            {
                switch (val)
                {
                    default:
                        seconds = 0L;
                        Debug.WriteLine($"TimeSpanToSeconds, ReadJson, unknown type {val.GetType().Name}");
                        break;
                    case string s:
                        seconds = 0L;
                        if (!Int64.TryParse(s, out seconds))
                        {
                            Debug.WriteLine($"TimeSpanToSeconds, ReadJson, string {val.ToString()} not convertible to long");
                        }
                        break;
                    case int i:
                        seconds = (long)i;
                        break;
                    case long l:
                        seconds = l;
                        break;
                    case float f:
                        seconds = (long)Math.Round(f);
                        break;
                    case double d:
                        seconds = (long)Math.Round(d);
                        break;
                }
            }
            return TimeSpan.FromSeconds(seconds);
            //return null;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            //Debugger.Break();
            var ts = (TimeSpan)value;
            writer.WriteValue(ts.TotalSeconds);
        }
    }
    public class DeviceStatus : MessageBase
    {
        public string Key { get; set; }
        public PlayerStates State { get; set; }
        [JsonConverter(typeof(TimeSpanToSeconds))]
        public TimeSpan CurrentTime { get; set; }
        [JsonConverter(typeof(TimeSpanToSeconds))]
        public TimeSpan TotalTime { get; set; }
        /// <summary>
        /// range is 0.0% to 100%
        /// </summary>
        public float Volume { get; set; } // 0.0% to 100.0%
        public override string ToString()
        {
            return $"{Key}: {State}, current {CurrentTime.TotalSeconds}, total {TotalTime.TotalSeconds}, vol {Volume}";
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
