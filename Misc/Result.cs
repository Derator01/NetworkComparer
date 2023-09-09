using System.Text.Json.Serialization;

namespace PriceComparer;

public static partial class Program
{
    public class Result
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("mech")]
        public object Mech { get; set; }

        [JsonPropertyName("img_link")]
        public string ImgLink { get; set; }

        [JsonPropertyName("plu")]
        public long Plu { get; set; }

        [JsonPropertyName("promo")]
        public Promo Promo { get; set; }

        [JsonPropertyName("current_prices")]
        public CurrentPrices CurrentPrices { get; set; }

        [JsonPropertyName("store_name")]
        public object StoreName { get; set; }
    }
}
