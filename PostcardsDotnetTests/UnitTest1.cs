using PostcardsDotnet;

namespace PostcardsDotnetTests;

public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public async Task Test1()
    {
        var swissApi = new SwissPostcardCreatorApi();
        var token = await SwissPostcardCreatorApi.LoginSwissId("",  "");
        var refreshToken = await SwissPostcardCreatorApi
            .PccWebRefreshToken(token["refresh_token"]!.AsValue().ToString());
    }
}
