namespace Edp.DigitalSignature.Infrastructure.Repositories;

using Edp.DigitalSignature.Application.Interfaces;
using Edp.DigitalSignature.Infrastructure.Persistence;
using System.Text.Json;
using Edp.Shared.Contracts;
using Edp.SharedKernel.Domain;
using Edp.DigitalSignature.Infrastructure.Outbox;
using Edp.DigitalSignature.Infrastructure.Messaging;

/// <summary>
/// Unit of Work pattern implementation coordinating all repositories.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly SigningDbContext _dbContext;
    private ISigningRequestRepository? _signingRequestRepository;
    private ISignerRepository? _signerRepository;
    private ISignatureFieldRepository? _signatureFieldRepository;
    private ISignatureActionRepository? _signatureActionRepository;
    private ISigningProviderTransactionRepository? _providerTransactionRepository;

    public UnitOfWork(SigningDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public ISigningRequestRepository SigningRequests
    {
        get { return _signingRequestRepository ??= new SigningRequestRepository(_dbContext); }
    }

    public ISignerRepository Signers
    {
        get { return _signerRepository ??= new SignerRepository(_dbContext); }
    }

    public ISignatureFieldRepository SignatureFields
    {
        get { return _signatureFieldRepository ??= new SignatureFieldRepository(_dbContext); }
    }

    public ISignatureActionRepository SignatureActions
    {
        get { return _signatureActionRepository ??= new SignatureActionRepository(_dbContext); }
    }

    public ISigningProviderTransactionRepository ProviderTransactions
    {
        get { return _providerTransactionRepository ??= new SigningProviderTransactionRepository(_dbContext); }
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var aggregates = _dbContext.ChangeTracker
            .Entries<Edp.SharedKernel.Entities.BaseEntity<Guid>>()
            .Select(entry => entry.Entity)
            .Where(entity => entity.DomainEvents.Count > 0)
            .ToList();

        var outboxMessages = new List<OutboxMessage>();
        foreach (var aggregate in aggregates)
        {
            foreach (var domainEvent in aggregate.DomainEvents)
            {
                var eventId = Guid.NewGuid();
                var mappedEvent = SigningIntegrationEventMapper.Map(eventId, domainEvent);
                var eventType = mappedEvent?.EventType ?? domainEvent.GetType().Name;
                var data = mappedEvent?.Data ?? domainEvent;
                var envelope = new EventEnvelope
                {
                    EventId = eventId,
                    EventType = eventType,
                    OccurredAt = DateTimeOffset.UtcNow,
                    OrganizationId = ReadGuid(domainEvent, "OrganizationId"),
                    CorrelationId = ReadGuid(domainEvent, "CorrelationId"),
                    Data = data
                };
                var outboxMessage = OutboxMessage.Create(
                    envelope.EventType,
                    aggregate.GetType().Name,
                    aggregate.Id,
                    JsonSerializer.Serialize(envelope));
                _dbContext.OutboxMessages.Add(outboxMessage);
                outboxMessages.Add(outboxMessage);
            }
        }

        try
        {
            var result = await _dbContext.SaveChangesAsync(cancellationToken);
            foreach (var aggregate in aggregates)
            {
                aggregate.ClearDomainEvents();
            }

            return result;
        }
        catch
        {
            foreach (var message in outboxMessages)
            {
                _dbContext.Entry(message).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
            }

            throw;
        }
    }

    private static Guid? ReadGuid(DomainEvent value, string propertyName)
        => value.GetType().GetProperty(propertyName)?.GetValue(value) is Guid id && id != Guid.Empty ? id : null;

    public void Dispose()
    {
        _dbContext?.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (_dbContext is not null)
        {
            await _dbContext.DisposeAsync();
        }
    }
}
