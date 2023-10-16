namespace PostcardDotnet.Contracts;

/// <summary>
/// Address interface
/// </summary>
public interface IAddress
{
    /// <summary>
    /// First name
    /// </summary>
    public string FirstName { get; init; }

    /// <summary>
    /// Last name
    /// </summary>
    public string LastName { get; init; }

    /// <summary>
    /// Street with number
    /// </summary>
    public string Street { get; init; }

    /// <summary>
    /// Zip code
    /// </summary>
    public string Zip { get; init; }

    /// <summary>
    /// City
    /// </summary>
    public string City { get; init; }

    /// <summary>
    ///  Company
    /// </summary>
    public string? Company { get; init; }
}
