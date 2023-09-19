using System.Text.Json.Nodes;

namespace PostcardDotnet.Contracts;

public interface ITokenService
{
    /// <summary>
    ///
    /// </summary>
    /// <returns></returns>
    public Task<IToken> GetToken(string username, string password);

    /// <summary>
    ///
    /// </summary>
    /// <returns></returns>
    public Task<IToken> RefreshToken();
}
