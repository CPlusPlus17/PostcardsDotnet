namespace PostcardDotnet.Contracts;

public interface IToken
{
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
    public int ExpiresInSeconds { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}
