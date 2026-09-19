namespace Edp.DigitalSignature.Domain.Enums;

/// <summary>
/// Determines how multiple signers sign the document.
/// </summary>
public enum SigningMode
{
    /// <summary>Signers must sign in a specific order, one after another.</summary>
    Sequential = 0,

    /// <summary>Signers can sign simultaneously without a specific order.</summary>
    Parallel = 1
}
