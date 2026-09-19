namespace Edp.DigitalSignature.Domain.Enums;

/// <summary>
/// Types of actions that can be recorded for audit trail purposes.
/// </summary>
public enum SignatureActionType
{
    /// <summary>Signer has viewed the document.</summary>
    Viewed = 0,

    /// <summary>Signer has signed the document.</summary>
    Signed = 1,

    /// <summary>Signer has declined to sign.</summary>
    Declined = 2,

    /// <summary>A reminder notification was sent to the signer.</summary>
    Reminded = 3
}
