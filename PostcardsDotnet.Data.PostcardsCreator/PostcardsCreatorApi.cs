using System.Text.Json;
using System.Text.Json.Nodes;
using PostcardDotnet.Common;

namespace PostcardDotnet.Data.PostcardsCreator;

/// <summary>
/// Postcard api
/// </summary>
public sealed class PostcardsCreatorApi
{
    /// <summary>
    /// Http client for accessing rest api
    /// </summary>
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Access token
    /// </summary>
    private string? _accessToken;

    /// <summary>
    ///
    /// </summary>
    /// <param name="accessToken"></param>
    public PostcardsCreatorApi(string? accessToken = null)
    {
        _accessToken = accessToken;

        _httpClient = new()
        {
            BaseAddress = new("https://pccweb.api.post.ch/secure/api/mobile/v1/"),
            DefaultRequestHeaders =
            {
                { "User-Agent", SwissIdLoginHelper.UserAgent },
                { "Authorization", $"Bearer {accessToken}"}
            }
        };
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="accessToken"></param>
    public void SetAccessToken(string accessToken)
    {
        _accessToken = accessToken;
        _httpClient.DefaultRequestHeaders.Authorization = new("Bearer", accessToken);
    }

    /// <summary>
    /// Get user quota
    /// </summary>
    public async Task<PostcardCreatorQuota> UserQuota()
    {
        var res = JsonSerializer
            .Deserialize<JsonObject>(await _httpClient.GetStringAsync("user/quota"))
            ?? throw new("Invalid response");

        return res["model"].Deserialize<PostcardCreatorQuota>() ?? throw new("Invalid json");
    }

    /// <summary>
    /// Get current user information
    /// </summary>
    public async void UserCurrent()
    {

    }

    /// <summary>
    /// Get account balance
    /// </summary>
    public async void BillingAccountSaldo()
    {

    }

    /// <summary>
    /// Upload a card
    /// </summary>
    public async void CardUpload()
    {

    }
}
