using System.Text.Json.Serialization;

namespace PostcardDotnet.Common;

/// <summary>
/// SwissId login object
/// </summary>
public record SwissIdLoginRecord
{
    [property: JsonPropertyName("client_id")]
    public required string ClientId { get; init; }

    [property: JsonPropertyName("response_type")]
    public required string ResponseType { get; init; }

    [property: JsonPropertyName("redirect_uri")]
    public required string RedirectUri { get; init; }

    [property: JsonPropertyName("scope")]
    public required string Scope { get; init; }

    [property: JsonPropertyName("response_mode")]
    public required string ResponseMode { get; init; }

    [property: JsonPropertyName("state")]
    public required string State { get; init; }

    [property: JsonPropertyName("code_challenge")]
    public required string CodeChallenge { get; init; }

    [property: JsonPropertyName("code_challenge_method")]
    public required string CodeChallengeMethod { get; init; }

    [property: JsonPropertyName("lang")]
    public required string Lang { get; init; }
}
