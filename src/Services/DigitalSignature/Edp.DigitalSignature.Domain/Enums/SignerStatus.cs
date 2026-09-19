namespace Edp.DigitalSignature.Domain.Enums;

/// <summary>
/// Represents the status of an individual signer in a signing request.
/// </summary>
public enum SignerStatus
{
    /// <summary>Signer has been added but invitation not yet sent.</summary>
    Pending = 0,

    /// <summary>Invitation has been sent to the signer.</summary>
    Invited = 1,

    /// <summary>Signer has viewed the document.</summary>
    Viewed = 2,

    /// <summary>Signer is ready to provide their signature.</summary>
    ReadyToSign = 3,

    /// <summary>Signer has successfully signed the document.</summary>
    Signed = 4,

    /// <summary>Signer declined to sign.</summary>
    Declined = 5,

    /// <summary>Signer's invitation has expired.</summary>
    Expired = 6,

    /// <summary>Signer's participation has been cancelled.</summary>
    Cancelled = 7
}
