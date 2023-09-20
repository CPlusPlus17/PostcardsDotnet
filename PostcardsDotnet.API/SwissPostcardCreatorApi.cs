using PostcardDotnet.Contracts;

namespace PostcardsDotnet.API;

public sealed class SwissPostcardCreatorApi
{
    /// <summary>
    /// Token service
    /// </summary>
    private readonly ITokenService _tokenService;

    /// <summary>
    /// Token after login
    /// </summary>
    private IToken? _token;

    /// <summary>
    /// Constructor with mandatory token servie
    /// </summary>
    /// <param name="tokenService"></param>
    /// <exception cref="Exception"></exception>
    public SwissPostcardCreatorApi(ITokenService tokenService)
    {
        _tokenService = tokenService ?? throw new("No ITokenService provided");
    }

    /// <summary>
    /// Try to login and get a token
    /// </summary>
    /// <param name="username"></param>
    /// <param name="password"></param>
    public async Task Login(string username, string password)
    {
        _token = await _tokenService.GetToken(username, password);
    }

    /// <summary>
    /// Refresh token after login
    /// </summary>
    public async Task RefreshToken()
    {
        if (_token is null) throw new("No valid token, can't refresh");
        _token = await _tokenService.RefreshToken();
    }

    /// <summary>
    /// Get DateTimeOffset when token expires at
    /// </summary>
    /// <returns></returns>
    public DateTimeOffset? GetTokenExpiresAt()
    {
        return _token?.ExpiresAt;
    }

    /// <summary>
    /// Set sender address
    /// </summary>
    public void SetSender()
    {

    }

    /// <summary>
    /// Set recipient address
    /// </summary>
    public void SetRecipient()
    {

    }

    /// <summary>
    /// Send postcard with image and optinal text
    /// </summary>
    /// <param name="image"></param>
    /// <param name="message"></param>
    public void SendPostcard(byte[] image, string? message)
    {

    }
}
