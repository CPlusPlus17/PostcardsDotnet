using PostcardDotnet.Contracts;

namespace PostcardDotnet.Common;

/// <summary>
/// Recipient address
/// </summary>
public class RecipientAddressRecord : IAddress
{
    /// <summary>
    /// Country, fix set to switzerland
    /// </summary>
    public const string Country = "SWITZERLAND";

    /// <summary>
    /// <inheritdoc />
    /// </summary>
    public required string FirstName { get; init; }

    /// <summary>
    /// <inheritdoc />
    /// </summary>
    public required string LastName { get; init; }

    /// <summary>
    /// <inheritdoc />
    /// </summary>
    public required string Street { get; init; }

    /// <summary>
    /// <inheritdoc />
    /// </summary>
    public required string Zip { get; init; }

    /// <summary>
    /// <inheritdoc />
    /// </summary>
    public required string City { get; init; }

    /// <summary>
    /// <inheritdoc />
    /// </summary>
    public string? Company { get; init; }

    /// <summary>
    /// Company addon
    /// </summary>
    public string? CompanyAddon { get; init; }

    /// <summary>
    /// Title
    /// </summary>
    public string? Title { get; init; }
}
