using Edp.Identity.Application.Commands;
using Edp.Identity.Application.Interfaces;
using Edp.Identity.Application.Repositories;
using Edp.Identity.Domain.Entities;

namespace Edp.Identity.Application.Services;

public sealed class IdentityService : IIdentityService
{
    private readonly IUserRepository _userRepository;

    public IdentityService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<User> RegisterAsync(RegisterUserCommand command, CancellationToken cancellationToken = default)
    {
        var email = command.Email.Trim();
        var existing = await _userRepository.GetByEmailAsync(email, cancellationToken);
        if (existing is not null)
        {
            throw new InvalidOperationException($"User with email '{email}' already exists.");
        }

        var user = User.Create(
            Guid.NewGuid(),
            email,
            command.FirstName,
            command.LastName);

        await _userRepository.AddAsync(user, cancellationToken);
        return user;
    }

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return Task.FromResult<User?>(null);
        }

        return _userRepository.GetByEmailAsync(email.Trim(), cancellationToken);
    }

    public Task<bool> ValidatePasswordAsync(User user, string password, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(!string.IsNullOrWhiteSpace(password) && password.Length >= 8);
    }
}
