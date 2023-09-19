using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Web;
using PostcardDotnet.Common;
using PostcardDotnet.Contracts;

namespace PostcardDotnet.Services;

public sealed class SwissIdLoginService : ITokenService
{
    public async Task<IToken> GetToken(string username, string password)
    {
        return await LoginSwissId(username, password);
    }

    public async Task<IToken> RefreshToken()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Login with swiss id
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    private static async Task<IToken> LoginSwissId(string username, string password)
    {
        // Prepare client with cookie support and correct headers
        var httpClientWithCookies = SwissIdLoginHelper.PrepareHttpClient(true);
        var (codeVerifier, codeChallenge) = SwissIdLoginHelper.CreateRandomToken();

        // PccWeb authorization - cookies
        await PccWebAuthorization(codeChallenge, httpClientWithCookies);

        // Swiss post login - goto parameter extraction
        var swissPostGoToParameter = await SwissPostLogin(httpClientWithCookies);

        // Set query string
        var urlQueryString = $"locale=en" +
                             $"&goto={swissPostGoToParameter}" +
                             $"&acr_values=loa-1" +
                             $"&realm=%2Fsesam" +
                             $"&service=qoa1";

        // Swiss id api login step 1 - cookies
        await SwissIdApiLoginToken(httpClientWithCookies, swissPostGoToParameter);

        // Swiss id api login step 2 - cookies
        await SwissIdApiLoginWelcomePack(httpClientWithCookies, swissPostGoToParameter);

        // Swiss id login init - authId
        var authId = await SwissIdLoginAuthenticateInit(httpClientWithCookies, swissPostGoToParameter);

        // Swiss id login basic - Get next action type
        (var nextActionType, authId) = await SwissIdLoginAuthenticateBasic(httpClientWithCookies,
            urlQueryString, authId, username, password );

        // Wait two factor FA if needed
        if (nextActionType.Equals("WAIT_FOR_ASYNC_SWISS_ID_APP_AUTHENTICATION"))
        {
            authId = await SwissIdLoginCheckTwoFaStatus(httpClientWithCookies, nextActionType,
                urlQueryString, authId);
        }
        else
        {
            throw new NotImplementedException();
        }

        // Send anomaly detection request - get next url for saml
        var nextUrl = await SwissIdAnomalyDetection(httpClientWithCookies, authId, urlQueryString);

        // Get next url from next url
        nextUrl = await SwissIdGetNextUrl(httpClientWithCookies, nextUrl);

        // Get saml answer - token and relay state
        var (samlToken, relayState) = await SwissIdGetTokenAndRelayState(httpClientWithCookies, nextUrl);

        // PccWeb OAuth - get code
        var code = await PccWebOAuth(httpClientWithCookies, samlToken, relayState);

        // PccWeb token - getting access and refresh token
        var tokenObject = await PccWebToken(code, codeVerifier);

        return new SwissIdTokenRecord()
        {
            AccessToken = tokenObject["access_token"]?.AsValue().ToString()
                          ?? throw new("Missing access token attribute"),
            RefreshToken = tokenObject["refresh_token"]?.AsValue().ToString()
                           ?? throw new("Missing refresh token attribute"),
            ExpiresInSeconds = tokenObject["expires_in"]?.AsValue().GetValue<int>()
                               ?? throw new("Missing expires in attribute"),
            ExpiresAt = DateTimeOffset.Now.AddSeconds(tokenObject["expires_in"]?.AsValue().GetValue<double?>()
                                                      ?? throw new("Can't calculate expires at"))
        };
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="codeChallenge"></param>
    /// <param name="httpClient"></param>
    /// <exception cref="Exception"></exception>
    private static async Task PccWebAuthorization(string codeChallenge, HttpClient httpClient)
    {
        // Prepare login data
        var loginData = new SwissIdLoginRecord
        {
            ClientId = SwissIdLoginHelper.ClientId,
            Lang = "en",
            Scope = "PCCWEB offline_access",
            State = "abcd",
            CodeChallenge = codeChallenge,
            RedirectUri = SwissIdLoginHelper.RedirectUri,
            ResponseMode = "query",
            ResponseType = "code",
            CodeChallengeMethod = "S256",
        };

        var pccWebRequest = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new ($"https://pccweb.api.post.ch/OAuth/authorization?{ObjToQueryString(loginData)}"),
        };

        var resPccWebResponseList = await SwissIdLoginHelper.FollowRedirect(
            await httpClient.SendAsync(pccWebRequest), pccWebRequest, httpClient);

        if (resPccWebResponseList.Last().StatusCode != HttpStatusCode.OK) throw new("PccWebAuthorization failed");
    }

    /// <summary>
    /// Start login with swiss post idp
    /// </summary>
    /// <param name="httpClient"></param>
    private static async Task<string> SwissPostLogin(HttpClient httpClient)
    {
        // Prepare swissid login
        var accountPostRequest = new HttpRequestMessage()
        {
            Method = HttpMethod.Post,
            RequestUri = new("https://account.post.ch/idp/?login" +
                             "&targetURL=https://pccweb.api.post.ch/SAML/ServiceProvider/" +
                             $"?redirect_uri={SwissIdLoginHelper.RedirectUri}" +
                             "&profile=default" +
                             "&app=pccwebapi" +
                             "&inMobileApp=true" +
                             "&layoutType=standard"),
            Content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("externalIDP", "externalIDP")
            })
        };

        var resAccountPostResponseList = await SwissIdLoginHelper.FollowRedirect(
            await httpClient.SendAsync(accountPostRequest), accountPostRequest, httpClient);

        // Get goto parameter
        var gotoParameterFoundUri = resAccountPostResponseList
            .Where(x => x.Headers.Location is not null)
            .Select( x => x.Headers.Location?.Query)
            .Last(x => !string.IsNullOrEmpty(x) && x.Contains("goto=")) ?? throw new("No goto parameter found");

        return SwissIdLoginHelper.GoToRegex().Match(gotoParameterFoundUri).Groups[1].Value.Split("&")[0];
    }

    /// <summary>
    /// Additional cookie for our session
    /// </summary>
    private static async Task SwissIdApiLoginToken(HttpClient httpClient, string gotoParameter)
    {
        var swissLoginPart1Request = new HttpRequestMessage()
        {
            RequestUri = new($"https://login.swissid.ch/api-login/authenticate/token/status?" +
                             $"locale=en" +
                             $"&goto={gotoParameter}" +
                             $"&acr_values=loa-1" +
                             $"&realm=%2Fsesam" +
                             $"&service=qoa1"),
            Method = HttpMethod.Get
        };

        await SwissIdLoginHelper.FollowRedirect(
            await httpClient.SendAsync(swissLoginPart1Request), swissLoginPart1Request, httpClient);
    }

    /// <summary>
    /// Additional cookies for our session
    /// </summary>
    /// <param name="httpClient"></param>
    /// <param name="gotoParameter"></param>
    private static async Task SwissIdApiLoginWelcomePack(HttpClient httpClient, string gotoParameter)
    {
        var swissLoginPart2Request = new HttpRequestMessage()
        {
            RequestUri = new($"https://login.swissid.ch/api-login/welcome-pack?" +
                             $"locale=en" +
                             $"&{gotoParameter}" +
                             $"&acr_values=loa-1" +
                             $"&realm=%2Fsesam&service=qoa1"),
            Method = HttpMethod.Get
        };

        await SwissIdLoginHelper.FollowRedirect(await httpClient.SendAsync(swissLoginPart2Request), swissLoginPart2Request, httpClient);
    }

    /// <summary>
    /// Get authId
    /// </summary>
    /// <returns></returns>
    private static async Task<string> SwissIdLoginAuthenticateInit(HttpClient httpClient, string gotoParameter)
    {
        var swissLoginPart3Request = new HttpRequestMessage()
        {
            RequestUri = new($"https://login.swissid.ch/api-login/authenticate/init?" +
                             $"locale=en" +
                             $"&goto={gotoParameter}" +
                             $"&acr_values=loa-1" +
                             $"&realm=%2Fsesam" +
                             $"&service=qoa1"),
            Method = HttpMethod.Post
        };

        var resSwissLoginPart3ResponseList = await SwissIdLoginHelper.FollowRedirect(
            await httpClient.SendAsync(swissLoginPart3Request), swissLoginPart3Request, httpClient);

        // Swissid login part 4
        return JsonSerializer.Deserialize<JsonObject>
            (await resSwissLoginPart3ResponseList.Last()
                .Content.ReadAsStringAsync())?["tokens"]?["authId"]?.AsValue().ToString() ?? throw new("Missing authId");

    }

    /// <summary>
    /// Get next action type
    /// </summary>
    /// <param name="httpClient"></param>
    /// <param name="urlQueryString"></param>
    /// <param name="authId"></param>
    /// <param name="username"></param>
    /// <param name="password"></param>
    /// <returns></returns>
    private static async Task<Tuple<string, string>> SwissIdLoginAuthenticateBasic(
        HttpClient httpClient, string urlQueryString, string authId, string username, string password)
    {
        var swissLoginPart4Request = new HttpRequestMessage()
        {
            RequestUri = new($"https://login.swissid.ch/api-login/authenticate/basic?{urlQueryString}"),
            Headers = { {"authId", authId}},
            Method = HttpMethod.Post,
            Content = JsonContent.Create(
                new {username, password } )
        };

        var resSwissLoginPart4ResponseList = await SwissIdLoginHelper.FollowRedirect(
            await httpClient.SendAsync(swissLoginPart4Request), swissLoginPart4Request, httpClient);

        var content = await resSwissLoginPart4ResponseList.Last().Content.ReadAsStringAsync();
        var nextAction = JsonSerializer
            .Deserialize<JsonObject>(content)?["nextAction"]?["type"]?.AsValue().ToString()
               ?? throw new ("Next action type not found");
        var authIdNex = JsonSerializer
                            .Deserialize<JsonObject>(content)?["tokens"]?["authId"]?.AsValue().ToString()
                        ?? throw new ("AuthId not found");

        return new(nextAction, authIdNex);
    }

    /// <summary>
    /// Transform object to query string
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    private static string ObjToQueryString(object obj)
    {
        var serializedObject = JsonSerializer.Serialize(obj);
        var deserializeObject = JsonSerializer.Deserialize<IDictionary<string, string>>(serializedObject);
        var encodedObject = deserializeObject
            ?.Select(x =>
                WebUtility.UrlEncode(x.Key) + "=" + WebUtility.UrlEncode(x.Value))
                            ?? throw new("Object couldn't be deserialized");

        return string.Join("&", encodedObject);
    }

    /// <summary>
    /// Try to wait until two FA is succeeded
    /// </summary>
    /// <param name="httpClient"></param>
    /// <param name="nextActionType"></param>
    /// <param name="urlQueryString"></param>
    /// <param name="authId"></param>
    /// <exception cref="Exception"></exception>
    private static async Task<string> SwissIdLoginCheckTwoFaStatus(
        HttpClient httpClient,
        string nextActionType,
        string urlQueryString,
        string authId)
    {
        var waitTimeStarted = DateTime.Now;

        while (nextActionType.Equals("WAIT_FOR_ASYNC_SWISS_ID_APP_AUTHENTICATION")
               || waitTimeStarted.AddMinutes(2) < DateTime.Now)
        {
            var statusRequest = new HttpRequestMessage()
            {
                RequestUri = new($"https://login.swissid.ch/api-login/authenticate/swiss-id-app/status?{urlQueryString}"),
                Headers = { {"authId", authId}},
                Method = HttpMethod.Get
            };

            var statusResponse = await SwissIdLoginHelper.FollowRedirect(
                await httpClient.SendAsync(statusRequest),
                statusRequest, httpClient);

            var content = await statusResponse.Last().Content.ReadAsStringAsync();

            authId = JsonSerializer
                         .Deserialize<JsonObject>(content)?["tokens"]?["authId"]?.AsValue().ToString()
                     ?? throw new("Missing authId");

            nextActionType = JsonSerializer
                                 .Deserialize<JsonObject>(content)?["nextAction"]?["type"]?.AsValue().ToString()
                             ?? throw new ("Next action type not found");

            await Task.Delay(3000);
        }

        return authId;
    }

    /// <summary>
    /// Protection to get next valid url to login
    /// </summary>
    /// <param name="httpClient"></param>
    /// <param name="authId"></param>
    /// <param name="urlQueryString"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    private static async Task<string> SwissIdAnomalyDetection(
        HttpClient httpClient,
        string authId,
        string urlQueryString)
    {
        var payloadAnomaly = new
        {
            appCodeName = "Mozilla",
            appName = "Netscape",
            appVersion = SwissIdLoginHelper.UserAgent.Replace("Mozilla/", ""),
            fonts = new Dictionary<string, string>()
            {
                {"installedFonts", "cursive;monospace;serif;sans-serif;fantasy;default;" +
                                   "Arial;Courier;Courier New;Georgia;Tahoma;Times;Times New Roman;Verdana"},
            },
            language = "de",
            platform = "Linux x86_64",
            plugins = new Dictionary<string, string>() {
                { "installedPlugins", "" }
            },
            product = "Gecko",
            productSub = "20030107",
            screen = new Dictionary<string, int>()
            {
                { "screenColourDepth", 24 },
                { "screenHeight", 732 },
                { "screenWidth", 412 }
            },
            timezone = new Dictionary<string, int>()
            {
                {"timezone", -120},
            },
            userAgent = SwissIdLoginHelper.UserAgent,
            vendor =  "Google Inc."
        };

        var swissLoginAnomalyDetectionRequest = new HttpRequestMessage()
        {
            RequestUri = new($"https://login.swissid.ch/api-login/anomaly-detection/device-print?{urlQueryString}"),
            Content = JsonContent.Create(payloadAnomaly),
            Headers = { {"authId", authId} },
            Method = HttpMethod.Post
        };

        var resSwissLoginAnomalyResponse = await httpClient.SendAsync(swissLoginAnomalyDetectionRequest);

        return JsonSerializer
            .Deserialize<JsonObject>(
                await resSwissLoginAnomalyResponse.Content.ReadAsStringAsync())?["nextAction"]?["successUrl"]
            ?.AsValue().ToString() ?? throw new("SuccessUrl not found");
    }

    /// <summary>
    /// Get next url to follow
    /// </summary>
    /// <param name="httpClient"></param>
    /// <param name="url"></param>
    /// <returns></returns>
    private static async Task<string> SwissIdGetNextUrl(HttpClient httpClient, string url)
    {
        var swissLoginSamlRequest = new HttpRequestMessage()
        {
            RequestUri = new(url),
            Method = HttpMethod.Get
        };

        var resSwissLoginSamlResponseList = await SwissIdLoginHelper.FollowRedirect(
            await httpClient.SendAsync(swissLoginSamlRequest),
            swissLoginSamlRequest, httpClient);

        var contentToParse = await resSwissLoginSamlResponseList.Last().Content.ReadAsStringAsync();
        var nextUrlSaml = SwissIdLoginHelper.FormUrlRegex().Match(contentToParse).Groups[1].Value;
        return Regex.Replace(nextUrlSaml, @"\s+", "");
    }

    /// <summary>
    /// Get token and relay state
    /// </summary>
    /// <param name="httpClient"></param>
    /// <param name="nextUrl"></param>
    /// <returns></returns>
    private static async Task<Tuple<string, string>> SwissIdGetTokenAndRelayState(HttpClient httpClient, string nextUrl)
    {
        var resSwissLoginSamlFinalRequest = new HttpRequestMessage()
        {
            RequestUri = new(nextUrl),
            Method = HttpMethod.Post
        };

        var resSwissLoginSamlFinalResponse = await httpClient.SendAsync(resSwissLoginSamlFinalRequest);

        var contentFromSamlFinal = await resSwissLoginSamlFinalResponse.Content.ReadAsStringAsync();
        var samlToken = SwissIdLoginHelper.SamlTokenRegex().Match(contentFromSamlFinal).Groups[1].Value;
        var relayState = SwissIdLoginHelper.RelayStateRegex().Match(contentFromSamlFinal).Groups[1].Value;

        return new(samlToken, relayState);
    }

    /// <summary>
    /// PccWeb OAuth for receiving code
    /// </summary>
    /// <param name="httpClient"></param>
    /// <param name="samlToken"></param>
    /// <param name="relayState"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    private static async Task<string> PccWebOAuth(HttpClient httpClient, string samlToken, string relayState)
    {
        var postCardLoginSamlRequest = new HttpRequestMessage()
        {
            RequestUri = new("https://pccweb.api.post.ch/OAuth/"),
            Content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("RelayState", relayState),
                new KeyValuePair<string, string>("SAMLResponse", samlToken),
            }),
            Headers =
            {
                { "Origin", "https://account.post.ch"},
                { "X-Requested-With", "ch.post.it.pcc" },
                { "Upgrade-Insecure-Requests", "1"}
            },
            Method = HttpMethod.Post
        };

        // Explicit no redirect
        var postCardLoginResponse = await httpClient.SendAsync(postCardLoginSamlRequest);
        var queryString = postCardLoginResponse.Headers.Location?.Query ?? throw new("Query not found");
        var parsedQueryString = HttpUtility.ParseQueryString(queryString);

        // Get the value of the code parameter
        return parsedQueryString["code"] ?? throw new("Code not found");
    }

    /// <summary>
    /// Get token from PccWeb
    /// </summary>
    /// <param name="code"></param>
    /// <param name="codeVerifier"></param>
    /// <returns></returns>
    private static async Task<JsonObject> PccWebToken(string code, string codeVerifier)
    {
        var postFinalStepRequest = new HttpRequestMessage()
        {
            RequestUri = new("https://pccweb.api.post.ch/OAuth/token"),
            Content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("client_id", SwissIdLoginHelper.ClientId),
                new KeyValuePair<string, string>("client_secret", SwissIdLoginHelper.ClientSecret),
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("code_verifier", codeVerifier),
                new KeyValuePair<string, string>("redirect_uri", SwissIdLoginHelper.RedirectUri)
            }),
            Method = HttpMethod.Post
        };

        // New session needed
        var postFinalResponse = await new HttpClient(new HttpClientHandler()
        {
            AllowAutoRedirect = false,
        }).SendAsync(postFinalStepRequest);
        var finalContent = await postFinalResponse.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<JsonObject>(finalContent) ?? throw new("Token not found");
    }

    /// <summary>
    /// Refresh token
    /// </summary>
    /// <param name="refreshToken"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    private static async Task<JsonObject> PccWebRefreshToken(string? refreshToken)
    {
        var postFinalStepRefreshRequest = new HttpRequestMessage
        {
            RequestUri = new("https://pccweb.api.post.ch/OAuth/token"),
            Content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string?>("grant_type", "refresh_token"),
                new KeyValuePair<string, string?>("client_id", SwissIdLoginHelper.ClientId),
                new KeyValuePair<string, string?>("client_secret", SwissIdLoginHelper.ClientSecret),
                new KeyValuePair<string, string?>("refresh_token", refreshToken),
            }),
            Method = HttpMethod.Post
        };

        var postFinalRefreshResponse = await new HttpClient(new HttpClientHandler()
        {
            AllowAutoRedirect = false,
        }).SendAsync(postFinalStepRefreshRequest);

        return JsonSerializer.Deserialize<JsonObject>(
            await postFinalRefreshResponse.Content.ReadAsStringAsync()) ?? throw new("Token not found");
    }
}
