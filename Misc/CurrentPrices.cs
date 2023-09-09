using System.Text.Json.Serialization;

namespace PriceComparer;

public static partial class Program
{
    public class CurrentPrices
    {
        [JsonPropertyName("price_reg__min")]
        public double PriceRegMin { get; set; }

        [JsonPropertyName("price_promo__min")]
        public double PricePromoMin { get; set; }
    }
}
