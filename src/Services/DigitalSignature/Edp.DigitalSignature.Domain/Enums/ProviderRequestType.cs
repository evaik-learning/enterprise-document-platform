namespace Edp.DigitalSignature.Domain.Enums;

/// <summary>
/// Types of requests sent to the signature provider.
/// </summary>
public enum ProviderRequestType
{
    /// <summary>Request to create a new signing envelope.</summary>
    CreateEnvelope = 0,

    /// <summary>Request for a signer to sign the document.</summary>
    Sign = 1,

    /// <summary>Request to get current status of a signing session.</summary>
    GetStatus = 2,

    /// <summary>Request to cancel an in-progress signing session.</summary>
    Cancel = 3
}
