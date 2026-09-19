namespace Edp.DigitalSignature.Infrastructure.Repositories;

using Edp.DigitalSignature.Application.Interfaces;
using Edp.DigitalSignature.Application.Contracts;
using Edp.DigitalSignature.Domain.Entities;
using Edp.DigitalSignature.Domain.Enums;
using Edp.DigitalSignature.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Repository for SigningRequest entities.
/// </summary>
public class SigningRequestRepository : ISigningRequestRepository
{
    private readonly SigningDbContext _context;

    public SigningRequestRepository(SigningDbContext context)
    {
        _context = context;
    }

    public async Task<SigningRequest?> GetByIdAsync(Guid signingRequestId, CancellationToken cancellationToken = default)
    {
        return await _context.SigningRequests
            .FirstOrDefaultAsync(sr => sr.SigningRequestId == signingRequestId, cancellationToken);
    }

    public async Task<SigningRequest?> GetDetailedAsync(Guid signingRequestId, CancellationToken cancellationToken = default)
    {
        return await _context.SigningRequests
            .Include(sr => sr.Signers)
            .FirstOrDefaultAsync(sr => sr.SigningRequestId == signingRequestId, cancellationToken);
    }

    public async Task<List<SigningRequest>> ListAsync(ListSigningRequestsFilter filter, CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(filter);
        return await query
            .OrderByDescending(sr => sr.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync(ListSigningRequestsFilter filter, CancellationToken cancellationToken = default)
    {
        return BuildQuery(filter).CountAsync(cancellationToken);
    }

    private IQueryable<SigningRequest> BuildQuery(ListSigningRequestsFilter filter)
    {
        var query = _context.SigningRequests.AsQueryable();

        if (filter.Status.HasValue)
        {
            query = query.Where(sr => sr.Status == filter.Status.Value);
        }

        if (filter.DocumentId.HasValue)
        {
            query = query.Where(sr => sr.DocumentId == filter.DocumentId.Value);
        }

        if (filter.WorkflowInstanceId.HasValue)
        {
            query = query.Where(sr => sr.WorkflowInstanceId == filter.WorkflowInstanceId.Value);
        }

        if (filter.CreatedFrom.HasValue)
        {
            query = query.Where(sr => sr.CreatedAt >= filter.CreatedFrom.Value);
        }

        if (filter.CreatedTo.HasValue)
        {
            query = query.Where(sr => sr.CreatedAt <= filter.CreatedTo.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.SignerEmail))
        {
            query = query.Where(sr => sr.Signers.Any(s => s.Email == filter.SignerEmail));
        }

        return query;
    }

    public void Add(SigningRequest signingRequest)
    {
        _context.SigningRequests.Add(signingRequest);
    }

    public void Update(SigningRequest signingRequest)
    {
        _context.SigningRequests.Update(signingRequest);
    }

    public async Task<List<SigningRequest>> FindByStatusAsync(int status, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _context.SigningRequests
            .Where(sr => (int)sr.Status == status)
            .OrderByDescending(sr => sr.CreatedAt)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<SigningRequest>> FindExpiredAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SigningRequests
            .Where(sr => sr.Status == SigningRequestStatus.Pending || sr.Status == SigningRequestStatus.InProgress)
            .Where(sr => sr.ExpiresAt != null && sr.ExpiresAt < DateTime.UtcNow)
            .ToListAsync(cancellationToken);
    }
}

/// <summary>
/// Repository for Signer entities.
/// </summary>
public class SignerRepository : ISignerRepository
{
    private readonly SigningDbContext _context;

    public SignerRepository(SigningDbContext context)
    {
        _context = context;
    }

    public async Task<Signer?> GetByIdAsync(Guid signerId, CancellationToken cancellationToken = default)
    {
        return await _context.Signers
            .FirstOrDefaultAsync(s => s.SignerId == signerId, cancellationToken);
    }

    public async Task<List<Signer>> GetBySigningRequestIdAsync(Guid signingRequestId, CancellationToken cancellationToken = default)
    {
        return await _context.Signers
            .Where(s => s.SigningRequestId == signingRequestId)
            .OrderBy(s => s.SigningOrder)
            .ToListAsync(cancellationToken);
    }

    public void Add(Signer signer)
    {
        _context.Signers.Add(signer);
    }

    public void Update(Signer signer)
    {
        _context.Signers.Update(signer);
    }

    public void AddRange(IEnumerable<Signer> signers)
    {
        _context.Signers.AddRange(signers);
    }
}

/// <summary>
/// Repository for SignatureField entities.
/// </summary>
public class SignatureFieldRepository : ISignatureFieldRepository
{
    private readonly SigningDbContext _context;

    public SignatureFieldRepository(SigningDbContext context)
    {
        _context = context;
    }

    public async Task<SignatureField?> GetByIdAsync(Guid signatureFieldId, CancellationToken cancellationToken = default)
    {
        return await _context.SignatureFields
            .FirstOrDefaultAsync(sf => sf.SignatureFieldId == signatureFieldId, cancellationToken);
    }

    public async Task<List<SignatureField>> GetBySigningRequestIdAsync(Guid signingRequestId, CancellationToken cancellationToken = default)
    {
        return await _context.SignatureFields
            .Where(sf => sf.SigningRequestId == signingRequestId)
            .ToListAsync(cancellationToken);
    }

    public void Add(SignatureField signatureField)
    {
        _context.SignatureFields.Add(signatureField);
    }

    public void AddRange(IEnumerable<SignatureField> fields)
    {
        _context.SignatureFields.AddRange(fields);
    }

    public void Update(SignatureField signatureField)
    {
        _context.SignatureFields.Update(signatureField);
    }
}

/// <summary>
/// Repository for SignatureAction (audit trail) entities.
/// </summary>
public class SignatureActionRepository : ISignatureActionRepository
{
    private readonly SigningDbContext _context;

    public SignatureActionRepository(SigningDbContext context)
    {
        _context = context;
    }

    public async Task<SignatureAction?> GetByIdAsync(Guid signatureActionId, CancellationToken cancellationToken = default)
    {
        return await _context.SignatureActions
            .FirstOrDefaultAsync(sa => sa.SignatureActionId == signatureActionId, cancellationToken);
    }

    public async Task<List<SignatureAction>> GetBySigningRequestIdAsync(Guid signingRequestId, CancellationToken cancellationToken = default)
    {
        return await _context.SignatureActions
            .Where(sa => sa.SigningRequestId == signingRequestId)
            .OrderBy(sa => sa.OccurredAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<SignatureAction>> GetBySignerIdAsync(Guid signerId, CancellationToken cancellationToken = default)
    {
        return await _context.SignatureActions
            .Where(sa => sa.SignerId == signerId)
            .OrderBy(sa => sa.OccurredAt)
            .ToListAsync(cancellationToken);
    }

    public void Add(SignatureAction signatureAction)
    {
        _context.SignatureActions.Add(signatureAction);
    }
}

/// <summary>
/// Repository for SigningProviderTransaction entities.
/// </summary>
public class SigningProviderTransactionRepository : ISigningProviderTransactionRepository
{
    private readonly SigningDbContext _context;

    public SigningProviderTransactionRepository(SigningDbContext context)
    {
        _context = context;
    }

    public async Task<SigningProviderTransaction?> GetByIdAsync(Guid transactionId, CancellationToken cancellationToken = default)
    {
        return await _context.ProviderTransactions
            .FirstOrDefaultAsync(spt => spt.ProviderTransactionId == transactionId, cancellationToken);
    }

    public async Task<SigningProviderTransaction?> GetByProviderRequestIdAsync(string providerRequestId, CancellationToken cancellationToken = default)
    {
        return await _context.ProviderTransactions
            .FirstOrDefaultAsync(spt => spt.ProviderRequestId == providerRequestId, cancellationToken);
    }

    public async Task<List<SigningProviderTransaction>> GetBySigningRequestIdAsync(Guid signingRequestId, CancellationToken cancellationToken = default)
    {
        return await _context.ProviderTransactions
            .Where(spt => spt.SigningRequestId == signingRequestId)
            .OrderByDescending(spt => spt.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public void Add(SigningProviderTransaction transaction)
    {
        _context.ProviderTransactions.Add(transaction);
    }

    public void Update(SigningProviderTransaction transaction)
    {
        _context.ProviderTransactions.Update(transaction);
    }
}
