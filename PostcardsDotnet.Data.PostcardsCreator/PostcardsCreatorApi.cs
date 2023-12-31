using System.Net.Http.Json;
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
                { "Authorization", $"Bearer {accessToken}" }
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
        var res = await _httpClient.GetFromJsonAsync<JsonObject>("user/quota")
                  ?? throw new("Invalid response");

        return res["model"].Deserialize<PostcardCreatorQuota>() ?? throw new("Invalid json");
    }

    /// <summary>
    /// Get current user information
    /// </summary>
    public async Task<PostcardCreatorUser> UserCurrent()
    {
        var res = await _httpClient.GetFromJsonAsync<JsonObject>("user/current")
                  ?? throw new("Invalid response");

        return res["model"].Deserialize<PostcardCreatorUser>() ?? throw new("Invalid json");
    }

    /// <summary>
    /// Get account balance
    /// </summary>
    public async Task<PostcardCreatorBalance> BillingAccountBalance()
    {
        var res = await _httpClient.GetFromJsonAsync<JsonObject>("billingOnline/accountSaldo")
                  ?? throw new("Invalid response");

        return res["model"].Deserialize<PostcardCreatorBalance>() ?? throw new("Invalid json");
    }

    /// <summary>
    /// Upload a card aka send
    /// </summary>
    public async Task<bool> CardUpload(RecipientAddressRecord? recipient, SenderAddressRecord? sender, string imageBase64,
        string? message)
    {
        if(recipient is null || sender is null || string.IsNullOrEmpty(imageBase64)) throw new("Invalid recipient, sender or image");

        var res = await _httpClient.PostAsJsonAsync<object>("card/upload", JsonSerializer.Serialize(
                      new PostcardCreatorCardUpload
                      {
                          Lang = "en",
                          Paid = false,
                          Recipient = recipient,
                          Sender = sender,
                          Text = message ?? string.Empty,
                          Image = imageBase64,
                          Stamp = null,
                          TextImage = null
                      }));

        return res.IsSuccessStatusCode; // Todo: Add logging
    }
}
