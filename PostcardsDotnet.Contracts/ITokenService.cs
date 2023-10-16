using System.Text.Json.Nodes;

namespace PostcardDotnet.Contracts;

/// <summary>
/// Token service interface
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Login and token generation
    /// </summary>
    /// <returns></returns>
    public Task<IToken> GetToken(string username, string password);

    /// <summary>
    /// Refresh token with a before received token
    /// </summary>
    /// <returns></returns>
    public Task<IToken> RefreshToken(string refreshToken);
}
