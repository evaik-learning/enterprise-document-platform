using Edp.SharedKernel.Entities;

namespace Edp.Identity.Domain.Entities;

public sealed class User : AuditableEntity<Guid>
{
    public string Email { get; private set; } = string.Empty;
    public string NormalizedEmail { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    public static User Create(Guid id, string email, string firstName, string lastName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        var trimmedEmail = email.Trim();
        var user = new User
        {
            Id = id,
            Email = trimmedEmail,
            NormalizedEmail = trimmedEmail,
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            DisplayName = string.IsNullOrWhiteSpace(firstName + lastName) ? trimmedEmail : $"{firstName.Trim()} {lastName.Trim()}".Trim()
        };

        return user;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
    public void UpdateProfile(string firstName, string lastName)
    {
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        DisplayName = string.IsNullOrWhiteSpace(firstName + lastName) ? Email : $"{firstName.Trim()} {lastName.Trim()}".Trim();
    }
}
