using Edp.Organization.Application.Commands;
using Edp.Organization.Application.Interfaces;
using Edp.Organization.Application.Repositories;

namespace Edp.Organization.Application.Services;

public sealed class OrganizationService : IOrganizationService
{
    private readonly IOrganizationRepository _repository;

    public OrganizationService(IOrganizationRepository repository)
    {
        _repository = repository;
    }

    public async Task<global::Edp.Organization.Domain.Entities.Organization> CreateAsync(CreateOrganizationCommand command, CancellationToken cancellationToken = default)
    {
        var organization = global::Edp.Organization.Domain.Entities.Organization.Create(Guid.NewGuid(), command.Name, command.Description);
        return await _repository.AddAsync(organization, cancellationToken);
    }

    public Task<global::Edp.Organization.Domain.Entities.Organization?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _repository.GetByIdAsync(id, cancellationToken);
    }

    public Task<IReadOnlyList<global::Edp.Organization.Domain.Entities.Organization>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return _repository.GetAllAsync(cancellationToken);
    }
}
