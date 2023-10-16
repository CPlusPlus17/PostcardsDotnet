using System.Text.Json.Serialization;

namespace PostcardDotnet.Common;

/// <summary>
/// 
/// </summary>
public record PostcardCreatorCardUpload
{
    [property: JsonPropertyName("lang")] 
    public required string Lang { get; set; }
    
    [property: JsonPropertyName("paid")]
    public required bool? Paid { get; set; }
    
    [property: JsonPropertyName("recipient")]
    public required RecipientAddressRecord Recipient { get; set; }
    
    [property: JsonPropertyName("sender")]
    public required SenderAddressRecord Sender { get; set; }
    
    [property: JsonPropertyName("text")]
    public required string Text { get; set; }
    
    [property: JsonPropertyName("textImage")]
    public required string? TextImage { get; set; }
    
    [property: JsonPropertyName("image")]
    public required string Image { get; set; }
    
    [property: JsonPropertyName("stamp")]
    public required string? Stamp { get; set; }
}