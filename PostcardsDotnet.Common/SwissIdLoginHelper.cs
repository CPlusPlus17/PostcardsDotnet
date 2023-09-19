using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;

namespace PostcardDotnet.Common;

public static partial class SwissIdLoginHelper
{
    /// <summary>
    /// App redirect uri
    /// </summary>
    public const string RedirectUri = "ch.post.pcc://auth/1016c75e-aa9c-493e-84b8-4eb3ba6177ef";

    /// <summary>
    /// User Agent to use for requests
    /// </summary>
    public const string UserAgent = "Mozilla/5.0 (Linux; Android 6.0.1; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/52.0.2743.98 Mobile Safari/537.36";

    /// <summary>
    /// PostCardApp client id
    /// </summary>
    public const string ClientId = "ae9b9894f8728ca78800942cda638155";

    /// <summary>
    /// PostCardApp client secret
    /// </summary>
    public const string ClientSecret = "89ff451ede545c3f408d792e8caaddf0";

    /// <summary>
    /// Regex to extract goto
    /// </summary>
    /// <returns></returns>
    [GeneratedRegex("goto=(.*?)$")]
    public static partial Regex GoToRegex();

    /// <summary>
    /// Regex to extraction action
    /// </summary>
    /// <returns></returns>
    [GeneratedRegex("""
                    action="([^"]+)"
                    """)]
    public static partial Regex FormUrlRegex();

    /// <summary>
    /// Regex to extract SAMLResponse value
    /// </summary>
    /// <returns></returns>
    [GeneratedRegex("""
                    name="SAMLResponse" value="([^"]+)"
                    """)]
    public static partial Regex SamlTokenRegex();

    /// <summary>
    /// Regex to extract RelayState value
    /// </summary>
    /// <returns></returns>
    [GeneratedRegex("""
                    name="RelayState" value="([^"]+)"
                    """)]
    public static partial Regex RelayStateRegex();

    /// <summary>
    /// Prepare a http client with disabled redirect and optional cookie support
    /// </summary>
    /// <param name="useCookies"></param>
    /// <returns></returns>
    public static HttpClient PrepareHttpClient(bool useCookies)
    {
        var handler = new HttpClientHandler()
        {
            // Do not redirect, we need to do this manually to extract data from redirect location
            AllowAutoRedirect = false
        };

        if (useCookies) handler.CookieContainer = new();

        // Set user agent to simulate a mobile device
        var httpClient = new HttpClient(handler)
        {
            DefaultRequestHeaders =
            {
                {
                    "User-Agent", UserAgent
                },
            }
        };

        return httpClient;
    }

    /// <summary>
    /// Create random token with 64 bytes
    /// </summary>
    /// <returns></returns>
    public static Tuple<string, string> CreateRandomToken()
    {
        var randomBytes = new byte[64];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }

        var randomString = UrlSafeBase64Encode(randomBytes);
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(randomString));

        return new(randomString, UrlSafeBase64Encode(hashBytes));
    }

    /// <summary>
    /// Get url safe base64 string
    /// </summary>
    /// <param name="bytes"></param>
    /// <returns></returns>
    private static string UrlSafeBase64Encode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=') // Remove padding
            .Replace('+', '-')
            .Replace('/', '_');
    }

    /// <summary>
    /// Follow a redirect chain until there are no future redirects
    /// </summary>
    /// <param name="responseMessage"></param>
    /// <param name="requestMessage"></param>
    /// <param name="httpClient"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static async Task<IList<HttpResponseMessage>> FollowRedirect(
        HttpResponseMessage responseMessage,
        HttpRequestMessage requestMessage,
        HttpClient httpClient)
    {
        var localRequestMessage = requestMessage;
        var res = new List<HttpResponseMessage> {responseMessage};

        while (responseMessage is {StatusCode: HttpStatusCode.Redirect})
        {
            var redirectUriReceived = responseMessage.Headers.Location;
            if (redirectUriReceived != null && !redirectUriReceived.IsAbsoluteUri)
            {
                redirectUriReceived = new (localRequestMessage.RequestUri?.GetLeftPart(UriPartial.Authority) + redirectUriReceived);
            }

            localRequestMessage = new()
            {
                Content = localRequestMessage.Content,
                Method = localRequestMessage.Method,
                RequestUri = redirectUriReceived,
                Version = localRequestMessage.Version,
                VersionPolicy = localRequestMessage.VersionPolicy
            };

            responseMessage = await httpClient.SendAsync(localRequestMessage);
            res.Add(responseMessage);
        }

        return res;
    }
}
