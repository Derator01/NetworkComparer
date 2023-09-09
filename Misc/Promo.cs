using System.Text.Json.Serialization;

namespace PriceComparer;

public static partial class Program
{
    public class Promo
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("date_begin")]
        public DateTime DateBegin { get; set; }

        [JsonPropertyName("date_end")]
        public DateTime DateEnd { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("kind")]
        public string Kind { get; set; }

        [JsonPropertyName("expired_at")]
        public long ExpiredAt { get; set; }
    }
}
