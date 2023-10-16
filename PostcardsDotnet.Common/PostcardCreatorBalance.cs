using System.Text.Json.Serialization;

namespace PostcardDotnet.Common;

/// <summary>
/// 
/// </summary>
public record PostcardCreatorBalance
{
    [property: JsonPropertyName("forecastSaldo")]
    public required double? ForecastBalance { get; set; }
}