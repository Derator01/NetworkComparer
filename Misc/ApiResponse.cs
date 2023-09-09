using System.Text.Json.Serialization;

namespace PriceComparer;

public static partial class Program
{
    public class ApiResponse
    {
        [JsonPropertyName("next")]
        public string Next { get; set; }

        [JsonPropertyName("previous")]
        public object Previous { get; set; }

        [JsonPropertyName("results")]
        public List<Result> Results { get; set; }
    }
}
