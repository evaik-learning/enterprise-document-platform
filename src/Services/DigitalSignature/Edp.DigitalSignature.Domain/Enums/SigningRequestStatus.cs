namespace Edp.DigitalSignature.Domain.Enums;

/// <summary>
/// Represents the status of a signing request throughout its lifecycle.
/// </summary>
public enum SigningRequestStatus
{
    /// <summary>Created but not yet ready for signing.</summary>
    Draft = 0,

    /// <summary>Ready for signing process to begin.</summary>
    Pending = 1,

    /// <summary>Signing process has been activated and is currently in progress.</summary>
    InProgress = 2,

    /// <summary>Some signers have signed but others have not.</summary>
    PartiallySigned = 3,

    /// <summary>All required signers have signed.</summary>
    Completed = 4,

    /// <summary>One or more signers declined to sign.</summary>
    Declined = 5,

    /// <summary>Request has expired and is no longer valid.</summary>
    Expired = 6,

    /// <summary>Signing request was cancelled by the requester.</summary>
    Cancelled = 7,

    /// <summary>An error occurred during the signing process.</summary>
    Failed = 8
}
