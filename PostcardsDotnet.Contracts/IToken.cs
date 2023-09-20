namespace PostcardDotnet.Contracts;

/// <summary>
/// Token interface
/// </summary>
public interface IToken
{
    /// <summary>
    /// Access token for accessing resources
    /// </summary>
    public string AccessToken { get; set; }

    /// <summary>
    /// Refresh token for getting a new access token
    /// </summary>
    public string RefreshToken { get; set; }

    /// <summary>
    /// Lifetime for the access token in seconds
    /// </summary>
    public int ExpiresInSeconds { get; set; }

    /// <summary>
    /// Date when the access token expires and must be refreshed
    /// </summary>
    public DateTimeOffset ExpiresAt { get; set; }
}
