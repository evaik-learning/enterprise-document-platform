using Edp.Identity.Application.Commands;
using Edp.Identity.Domain.Entities;

namespace Edp.Identity.Application.Interfaces;

public interface IIdentityService
{
    Task<User> RegisterAsync(RegisterUserCommand command, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> ValidatePasswordAsync(User user, string password, CancellationToken cancellationToken = default);
}
