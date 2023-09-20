using PostcardDotnet.Contracts;

namespace PostcardDotnet.Common;

/// <summary>
/// Sender address
/// </summary>
public record SenderRecord : IAddress
{
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
    public required string Company { get; init; }
}
