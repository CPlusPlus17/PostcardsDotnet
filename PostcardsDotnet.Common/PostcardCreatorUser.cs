using System.Text.Json.Serialization;

namespace PostcardDotnet.Common;

/// <summary>
/// User object
/// </summary>
public record PostcardCreatorUser
{
        [property: JsonPropertyName("company")]
        public required string Company { get; set; }
        
        [property: JsonPropertyName("firstName")]
        public required string FirstName { get; set; }
        
        [property: JsonPropertyName("name")]
        public required string Name { get; set; }
        
        [property: JsonPropertyName("street")]
        public required string Street { get; set; }
        
        [property: JsonPropertyName("zip")] 
        public required string Zip { get; set; }
        
        [property: JsonPropertyName("city")]
        public required string City { get; set; }
}
