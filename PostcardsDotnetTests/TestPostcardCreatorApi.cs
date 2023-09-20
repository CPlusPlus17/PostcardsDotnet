using PostcardDotnet.Common;
using PostcardDotnet.Services;
using PostcardsDotnet.API;

namespace PostcardsDotnetTests;

public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public async Task Login()
    {
        var postcardCreatorApi = new SwissPostcardCreatorApi(new SwissIdLoginService());
        await postcardCreatorApi.Login("", "");
        await postcardCreatorApi.RefreshToken();
    }

    [Test]
    public async Task Refresh()
    {
        var postcardCreatorApi = new SwissPostcardCreatorApi(new SwissIdLoginService());
        await postcardCreatorApi.Login("", "");
        await postcardCreatorApi.RefreshToken();
    }

    [Test]
    public async Task ScaleImage()
    {
        var image = ImageHelper.Scale(await File.ReadAllBytesAsync("Assets/sample.jpg"));
        await File.WriteAllBytesAsync("output.png", Convert.FromBase64String(image));
    }
}
