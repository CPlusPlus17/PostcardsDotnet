using PostcardDotnet.Common;
using PostcardDotnet.Contracts;
using PostcardDotnet.Data.PostcardsCreator;

namespace PostcardsDotnet.API;

/// <summary>
/// Swiss postcard creator api
/// </summary>
public sealed class SwissPostcardCreatorApi
{
    /// <summary>
    /// Token service
    /// </summary>
    private readonly ITokenService _tokenService;

    /// <summary>
    ///
    /// </summary>
    private readonly PostcardsCreatorApi _postcardsCreatorApi = new();

    /// <summary>
    /// Token after login
    /// </summary>
    private IToken? _token;

    /// <summary>
    /// Sender address
    /// </summary>
    private SenderAddressRecord? _senderAddress;

    /// <summary>
    /// Recipient address
    /// </summary>
    private RecipientAddressRecord? _recipientAddress;

    /// <summary>
    /// Constructor with mandatory token service
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
        _postcardsCreatorApi.SetAccessToken(_token.AccessToken);
    }

    /// <summary>
    /// Refresh token after login
    /// </summary>
    public async Task RefreshToken()
    {
        if (_token is null) throw new("No valid token, can't refresh");
        _token = await _tokenService.RefreshToken(_token.RefreshToken);
        _postcardsCreatorApi.SetAccessToken(_token.AccessToken);
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
    /// <param name="senderAddressAddress"></param>
    public void SetSender(SenderAddressRecord senderAddressAddress)
    {
        _senderAddress = senderAddressAddress;
    }

    /// <summary>
    /// Set recipient address
    /// </summary>
    /// <param name="recipientAddress"></param>
    public void SetRecipient(RecipientAddressRecord recipientAddress)
    {
        _recipientAddress = recipientAddress;
    }

    /// <summary>
    /// Send postcard with image and optinal text
    /// </summary>
    /// <param name="image"></param>
    /// <param name="message"></param>
    public async Task<bool> SendPostcard(byte[] image, string? message = null)
    {
        return await _postcardsCreatorApi.CardUpload(_recipientAddress, _senderAddress, ImageHelper.ScaleAndConvertToBase64(image), message);
    }

    /// <summary>
    /// Get current quota
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    public async Task<PostcardCreatorQuota> GetQuota()
    {
        return await _postcardsCreatorApi.UserQuota();
    }

    /// <summary>
    /// Get logged-in user information
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    public async Task<PostcardCreatorUser> GetUserInformation()
    {
        return await _postcardsCreatorApi.UserCurrent();
    }

    /// <summary>
    /// Get account balance from logged-in user
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    public async Task<PostcardCreatorBalance> GetAccountBalance()
    {
        return await _postcardsCreatorApi.BillingAccountBalance();
    }

    /// <summary>
    /// Check if a free card is available to send
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task<bool> FreeCardAvailable()
    {
        return (await _postcardsCreatorApi.UserQuota()).Available;
    }

    /// <summary>
    /// Get date and time when next free card can be send
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task<DateTimeOffset?> NextFreeCardAvailableAt()
    {
        return (await _postcardsCreatorApi.UserQuota()).NextDate;
    }
}
