using PostcardDotnet.Contracts;

namespace PostcardDotnet.Common;

/// <summary>
/// Token record with oauth tokens
/// </summary>
public record SwissIdTokenRecord : IToken
{
    /// <summary>
    /// <inheritdoc />
    /// </summary>
    public required string AccessToken { get; set; }

    /// <summary>
    /// <inheritdoc />
    /// </summary>
    public required string RefreshToken { get; set; }

    /// <summary>
    /// <inheritdoc />
    /// </summary>
    public required int ExpiresInSeconds { get; set; }

    /// <summary>
    /// <inheritdoc />
    /// </summary>
    public required DateTimeOffset ExpiresAt { get; set; }
}
