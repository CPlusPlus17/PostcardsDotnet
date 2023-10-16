using PostcardDotnet.Common;
using PostcardDotnet.Services;
using PostcardsDotnet.API;

namespace PostcardsDotnetTests;

public class Tests
{
    private string _username = string.Empty;
    private string _password = string.Empty;
    
    private string _senderFirstName = string.Empty;
    private string _senderLastName = string.Empty;
    private string _senderStreet = string.Empty;
    private string _senderZip = string.Empty;
    private string _senderCity = string.Empty;
    
    private string _recipientFirstName = string.Empty;
    private string _recipientLastName = string.Empty;
    private string _recipientStreet = string.Empty;
    private string _recipientZip = string.Empty;
    private string _recipientCity = string.Empty;
    
    [SetUp]
    public void Setup()
    {
        _username = Environment.GetEnvironmentVariable("PCD_USERNAME") ?? string.Empty;
        _password = Environment.GetEnvironmentVariable("PCD_PASSWORD") ?? string.Empty;
        
        _senderFirstName = Environment.GetEnvironmentVariable("PCD_SENDERFIRSTNAME") ?? string.Empty;
        _senderLastName = Environment.GetEnvironmentVariable("PCD_SENDERLASTNAME") ?? string.Empty;
        _senderStreet = Environment.GetEnvironmentVariable("PCD_SENDERSTREET") ?? string.Empty;
        _senderZip = Environment.GetEnvironmentVariable("PCD_SENDERZIP") ?? string.Empty;
        _senderCity = Environment.GetEnvironmentVariable("PCD_SENDERCITY") ?? string.Empty;
        
        _recipientFirstName = Environment.GetEnvironmentVariable("PCD_RECIPIENTFIRSTNAME") ?? string.Empty;
        _recipientLastName = Environment.GetEnvironmentVariable("PCD_RECIPIENTLASTNAME") ?? string.Empty;
        _recipientStreet = Environment.GetEnvironmentVariable("PCD_RECIPIENTSTREET") ?? string.Empty;
        _recipientZip = Environment.GetEnvironmentVariable("PCD_RECIPIENTZIP") ?? string.Empty;
        _recipientCity = Environment.GetEnvironmentVariable("PCD_RECIPIENTCITY") ?? string.Empty;
        
        if (string.IsNullOrEmpty(_username) || string.IsNullOrEmpty(_password))
        {
            throw new Exception("No username or password provided");
        }
        
        if (string.IsNullOrEmpty(_senderFirstName) || string.IsNullOrEmpty(_senderLastName) || string.IsNullOrEmpty(_senderStreet) || string.IsNullOrEmpty(_senderZip) || string.IsNullOrEmpty(_senderCity))
        {
            throw new Exception("No sender address provided");
        }
        
        if (string.IsNullOrEmpty(_recipientFirstName) || string.IsNullOrEmpty(_recipientLastName) || string.IsNullOrEmpty(_recipientStreet) || string.IsNullOrEmpty(_recipientZip) || string.IsNullOrEmpty(_recipientCity))
        {
            throw new Exception("No recipient address provided");
        }
    }

    [Test]
    public async Task Login()
    {
        var postcardCreatorApi = new SwissPostcardCreatorApi(new SwissIdLoginService());
        await postcardCreatorApi.Login(_username, _password);
    }

    [Test]
    public async Task Refresh()
    {
        var postcardCreatorApi = new SwissPostcardCreatorApi(new SwissIdLoginService());
        await postcardCreatorApi.Login(_username, _password);
        await postcardCreatorApi.RefreshToken();
    }

    [Test]
    public async Task ScaleImage()
    {
        var image = ImageHelper.ScaleAndConvertToBase64(await File.ReadAllBytesAsync("Assets/sample.jpg"));
        await File.WriteAllBytesAsync("output.png", Convert.FromBase64String(image));
    }

    [Test]
    public async Task GetQuota()
    {
        var postcardCreatorApi = new SwissPostcardCreatorApi(new SwissIdLoginService());
        await postcardCreatorApi.Login(_username, _password);
        var quota = await postcardCreatorApi.GetQuota();
    }
    
    [Test]
    public async Task GetUserInformation()
    {
        var postcardCreatorApi = new SwissPostcardCreatorApi(new SwissIdLoginService());
        await postcardCreatorApi.Login(_username, _password);
        var userInformation = await postcardCreatorApi.GetUserInformation();
    }
    
    [Test]
    public async Task GetAccountBalance()
    {
        var postcardCreatorApi = new SwissPostcardCreatorApi(new SwissIdLoginService());
        await postcardCreatorApi.Login(_username, _password);
        var balance = await postcardCreatorApi.GetAccountBalance();
    }
    
    [Test]
    public async Task FreeCardAvailable()
    {
        var postcardCreatorApi = new SwissPostcardCreatorApi(new SwissIdLoginService());
        await postcardCreatorApi.Login(_username, _password);
        var isFreeCardAvailable = await postcardCreatorApi.FreeCardAvailable();
    }
    
    [Test]
    public async Task NextFreeCardAvailableAt()
    {
        var postcardCreatorApi = new SwissPostcardCreatorApi(new SwissIdLoginService());
        await postcardCreatorApi.Login(_username, _password);
        var freeCardAvailableAt = await postcardCreatorApi.NextFreeCardAvailableAt();
    }

    [Test]
    public async Task SendPostcard()
    {
        var postcardCreatorApi = new SwissPostcardCreatorApi(new SwissIdLoginService());
        await postcardCreatorApi.Login(_username, _password);
        
        postcardCreatorApi.SetRecipient(new ()
        {
            FirstName = _recipientFirstName,
            LastName = _recipientLastName,
            Street = _recipientStreet,
            Zip = _recipientZip,
            City = _recipientCity
        });
        
        postcardCreatorApi.SetSender(new()
        {
            FirstName = _senderFirstName,
            LastName = _senderLastName,
            Street = _senderStreet,
            Zip = _senderZip,
            City = _senderCity 
        });
        
        //postcardCreatorApi.SendPostcard(await File.ReadAllBytesAsync("Assets/sample.jpg"), "API Test");
    }
}
