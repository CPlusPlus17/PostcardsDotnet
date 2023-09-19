using PostcardDotnet.Contracts;

namespace PostcardDotnet.Common;

public record SwissIdTokenRecord : IToken
{
    public required string AccessToken { get; set; }
    public required string RefreshToken { get; set; }
    public required int ExpiresInSeconds { get; set; }
    public required DateTimeOffset ExpiresAt { get; set; }
}
