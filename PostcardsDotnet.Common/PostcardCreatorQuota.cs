using System.Text.Json.Serialization;

namespace PostcardDotnet.Common;

/// <summary>
/// Quota object
/// </summary>
public record PostcardCreatorQuota
{
    [property: JsonPropertyName("quota")]
    public int Quota { get; set; }

    [property: JsonPropertyName("end")]
    public DateTimeOffset EndDate { get; set; }

    [property: JsonPropertyName("retentionDays")]
    public int RetentionDays { get; set; }

    [property: JsonPropertyName("available")]
    public bool Available { get; set; }

    [property: JsonPropertyName("next")]
    public DateTimeOffset? NextDate { get; set; }
};
