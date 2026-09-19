namespace Edp.DigitalSignature.Domain.Enums;

/// <summary>
/// Status of a provider transaction attempt.
/// </summary>
public enum ProviderTransactionStatus
{
    /// <summary>Transaction is awaiting first attempt.</summary>
    Pending = 0,

    /// <summary>Transaction completed successfully.</summary>
    Success = 1,

    /// <summary>Transaction failed and is not being retried.</summary>
    Failed = 2,

    /// <summary>Transaction failed but will be retried.</summary>
    Retrying = 3
}
