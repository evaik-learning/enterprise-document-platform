namespace Edp.Template.Domain.Exceptions;

/// <summary>Raised when a requested state transition or operation violates a Template aggregate business rule.</summary>
public sealed class TemplateDomainException : Exception
{
    public TemplateDomainException(string message) : base(message)
    {
    }
}
