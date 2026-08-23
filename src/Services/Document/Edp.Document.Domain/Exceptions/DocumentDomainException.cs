namespace Edp.Document.Domain.Exceptions;

public sealed class DocumentDomainException : Exception
{
    public DocumentDomainException(string message) : base(message)
    {
    }
}
