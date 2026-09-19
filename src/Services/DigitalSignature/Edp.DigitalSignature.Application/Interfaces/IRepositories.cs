namespace Edp.DigitalSignature.Application.Interfaces;

using Edp.DigitalSignature.Application.Contracts;
using Edp.DigitalSignature.Domain.Entities;

/// <summary>
/// Repository interface for SigningRequest entities.
/// </summary>
public interface ISigningRequestRepository
{
    /// <summary>
    /// Gets a signing request by ID.
    /// </summary>
    Task<SigningRequest?> GetByIdAsync(Guid signingRequestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a signing request including all related entities (navigation).
    /// </summary>
    Task<SigningRequest?> GetDetailedAsync(Guid signingRequestId, CancellationToken cancellationToken = default);

    Task<List<SigningRequest>> ListAsync(ListSigningRequestsFilter filter, CancellationToken cancellationToken = default);

    Task<int> CountAsync(ListSigningRequestsFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new signing request.
    /// </summary>
    void Add(SigningRequest signingRequest);

    /// <summary>
    /// Updates an existing signing request.
    /// </summary>
    void Update(SigningRequest signingRequest);

    /// <summary>
    /// Finds signing requests by status.
    /// </summary>
    Task<List<SigningRequest>> FindByStatusAsync(int status, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds expired signing requests.
    /// </summary>
    Task<List<SigningRequest>> FindExpiredAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository interface for Signer entities.
/// </summary>
public interface ISignerRepository
{
    /// <summary>
    /// Gets a signer by ID.
    /// </summary>
    Task<Signer?> GetByIdAsync(Guid signerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all signers for a signing request.
    /// </summary>
    Task<List<Signer>> GetBySigningRequestIdAsync(Guid signingRequestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new signer.
    /// </summary>
    void Add(Signer signer);

    /// <summary>
    /// Updates a signer.
    /// </summary>
    void Update(Signer signer);

    /// <summary>
    /// Adds multiple signers.
    /// </summary>
    void AddRange(IEnumerable<Signer> signers);
}

/// <summary>
/// Repository interface for SignatureField entities.
/// </summary>
public interface ISignatureFieldRepository
{
    /// <summary>
    /// Gets a signature field by ID.
    /// </summary>
    Task<SignatureField?> GetByIdAsync(Guid signatureFieldId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all signature fields for a signing request.
    /// </summary>
    Task<List<SignatureField>> GetBySigningRequestIdAsync(Guid signingRequestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new signature field.
    /// </summary>
    void Add(SignatureField signatureField);

    /// <summary>
    /// Adds multiple signature fields.
    /// </summary>
    void AddRange(IEnumerable<SignatureField> fields);

    /// <summary>
    /// Updates a signature field.
    /// </summary>
    void Update(SignatureField signatureField);
}

/// <summary>
/// Repository interface for SignatureAction entities.
/// </summary>
public interface ISignatureActionRepository
{
    /// <summary>
    /// Gets a signature action by ID.
    /// </summary>
    Task<SignatureAction?> GetByIdAsync(Guid signatureActionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all audit actions for a signing request.
    /// </summary>
    Task<List<SignatureAction>> GetBySigningRequestIdAsync(Guid signingRequestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all audit actions for a specific signer.
    /// </summary>
    Task<List<SignatureAction>> GetBySignerIdAsync(Guid signerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new audit action.
    /// </summary>
    void Add(SignatureAction signatureAction);
}

/// <summary>
/// Repository interface for SigningProviderTransaction entities.
/// </summary>
public interface ISigningProviderTransactionRepository
{
    /// <summary>
    /// Gets a provider transaction by ID.
    /// </summary>
    Task<SigningProviderTransaction?> GetByIdAsync(Guid transactionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a provider transaction by provider request ID (idempotency).
    /// </summary>
    Task<SigningProviderTransaction?> GetByProviderRequestIdAsync(string providerRequestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all transactions for a signing request.
    /// </summary>
    Task<List<SigningProviderTransaction>> GetBySigningRequestIdAsync(Guid signingRequestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new provider transaction record.
    /// </summary>
    void Add(SigningProviderTransaction transaction);

    /// <summary>
    /// Updates a provider transaction.
    /// </summary>
    void Update(SigningProviderTransaction transaction);
}

/// <summary>
/// Unit of Work pattern for coordinating repository operations.
/// </summary>
public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Gets the signing request repository.
    /// </summary>
    ISigningRequestRepository SigningRequests { get; }

    /// <summary>
    /// Gets the signer repository.
    /// </summary>
    ISignerRepository Signers { get; }

    /// <summary>
    /// Gets the signature field repository.
    /// </summary>
    ISignatureFieldRepository SignatureFields { get; }

    /// <summary>
    /// Gets the signature action repository.
    /// </summary>
    ISignatureActionRepository SignatureActions { get; }

    /// <summary>
    /// Gets the provider transaction repository.
    /// </summary>
    ISigningProviderTransactionRepository ProviderTransactions { get; }

    /// <summary>
    /// Saves all changes to the database.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
