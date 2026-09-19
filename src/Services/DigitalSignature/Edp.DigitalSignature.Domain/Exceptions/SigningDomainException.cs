namespace Edp.DigitalSignature.Domain.Exceptions;

/// <summary>
/// Base exception for all domain-level signing errors.
/// </summary>
public abstract class SigningDomainException : Exception
{
    /// <summary>
    /// Gets the error code for this exception.
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// Gets the HTTP status code associated with this exception.
    /// </summary>
    public int HttpStatusCode { get; protected set; }

    protected SigningDomainException(string errorCode, string message, int httpStatusCode = 400)
        : base(message)
    {
        ErrorCode = errorCode;
        HttpStatusCode = httpStatusCode;
    }
}

/// <summary>
/// Thrown when a signing request cannot be found.
/// </summary>
public sealed class SigningRequestNotFoundException : SigningDomainException
{
    public SigningRequestNotFoundException(Guid signingRequestId)
        : base(
            "SIGNING_REQUEST_NOT_FOUND",
            $"Signing request '{signingRequestId}' was not found.",
            404)
    {
    }
}

/// <summary>
/// Thrown when a signer cannot be found.
/// </summary>
public sealed class SignerNotFoundException : SigningDomainException
{
    public SignerNotFoundException(Guid signerId)
        : base(
            "SIGNER_NOT_FOUND",
            $"Signer '{signerId}' was not found.",
            404)
    {
    }
}

/// <summary>
/// Thrown when attempting an invalid state transition.
/// </summary>
public sealed class InvalidSigningStateException : SigningDomainException
{
    public InvalidSigningStateException(string currentState, string attemptedAction)
        : base(
            "INVALID_SIGNING_STATE",
            $"Cannot perform '{attemptedAction}' in state '{currentState}'.",
            409)
    {
    }
}

/// <summary>
/// Thrown when a signer is not authorized to sign.
/// </summary>
public sealed class SignerNotAuthorizedToSignException : SigningDomainException
{
    public SignerNotAuthorizedToSignException(Guid signerId, string reason)
        : base(
            "SIGNER_NOT_AUTHORIZED",
            $"Signer '{signerId}' is not authorized to sign: {reason}.",
            403)
    {
    }
}

/// <summary>
/// Thrown when a signing request has expired.
/// </summary>
public sealed class SigningRequestExpiredException : SigningDomainException
{
    public SigningRequestExpiredException(Guid signingRequestId)
        : base(
            "SIGNING_REQUEST_EXPIRED",
            $"Signing request '{signingRequestId}' has expired.",
            410)
    {
    }
}

/// <summary>
/// Thrown when the document version doesn't match.
/// </summary>
public sealed class DocumentVersionMismatchException : SigningDomainException
{
    public DocumentVersionMismatchException(Guid documentVersionId, string expectedHash, string actualHash)
        : base(
            "DOCUMENT_VERSION_MISMATCH",
            $"Document version '{documentVersionId}' hash mismatch. Expected: {expectedHash}, Actual: {actualHash}.",
            400)
    {
    }
}

/// <summary>
/// Thrown when the signing order is invalid.
/// </summary>
public sealed class InvalidSigningOrderException : SigningDomainException
{
    public InvalidSigningOrderException(string reason)
        : base(
            "INVALID_SIGNING_ORDER",
            $"Invalid signing order: {reason}.",
            400)
    {
    }
}

/// <summary>
/// Thrown when a duplicate signature is detected.
/// </summary>
public sealed class DuplicateSignatureException : SigningDomainException
{
    public DuplicateSignatureException(Guid signingRequestId, Guid signerId)
        : base(
            "DUPLICATE_SIGNATURE",
            $"Signer '{signerId}' has already signed request '{signingRequestId}'.",
            409)
    {
    }
}

/// <summary>
/// Thrown when attempting to sign a cancelled request.
/// </summary>
public sealed class SigningRequestCancelledException : SigningDomainException
{
    public SigningRequestCancelledException(Guid signingRequestId)
        : base(
            "SIGNING_REQUEST_CANCELLED",
            $"Signing request '{signingRequestId}' has been cancelled.",
            409)
    {
    }
}
