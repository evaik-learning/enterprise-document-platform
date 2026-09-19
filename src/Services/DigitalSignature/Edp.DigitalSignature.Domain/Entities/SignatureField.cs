namespace Edp.DigitalSignature.Domain.Entities;

using Edp.SharedKernel.Entities;
using Edp.DigitalSignature.Domain.Enums;

/// <summary>
/// Represents a signature field on a document.
/// </summary>
public class SignatureField : BaseEntity<Guid>
{
    /// <summary>
    /// Unique identifier for this signature field.
    /// </summary>
    public Guid SignatureFieldId { get; protected set; }

    /// <summary>
    /// Reference to the signing request.
    /// </summary>
    public Guid SigningRequestId { get; protected set; }

    /// <summary>
    /// Reference to the signer who must sign this field.
    /// </summary>
    public Guid SignerId { get; protected set; }

    /// <summary>
    /// Page number where this field is located (0-based).
    /// </summary>
    public int DocumentPage { get; protected set; }

    /// <summary>
    /// Type of field (signature, initials, date, text, checkbox).
    /// </summary>
    public FieldType FieldType { get; protected set; }

    /// <summary>
    /// X coordinate of the field on the page (pixels or percentage).
    /// </summary>
    public decimal X { get; protected set; }

    /// <summary>
    /// Y coordinate of the field on the page (pixels or percentage).
    /// </summary>
    public decimal Y { get; protected set; }

    /// <summary>
    /// Width of the field (optional).
    /// </summary>
    public decimal? Width { get; protected set; }

    /// <summary>
    /// Height of the field (optional).
    /// </summary>
    public decimal? Height { get; protected set; }

    /// <summary>
    /// Whether this field must be completed.
    /// </summary>
    public bool Required { get; protected set; }

    /// <summary>
    /// Label displayed for this field.
    /// </summary>
    public string? Label { get; protected set; }

    /// <summary>
    /// Value entered or signed by the signer.
    /// </summary>
    public string? Value { get; protected set; }

    /// <summary>
    /// Date and time when this field was completed.
    /// </summary>
    public DateTime? CompletedAt { get; protected set; }

    protected SignatureField()
    {
        Id = Guid.NewGuid();
        SignatureFieldId = Id;
    }

    /// <summary>
    /// Creates a new signature field.
    /// </summary>
    public static SignatureField Create(
        Guid signingRequestId,
        Guid signerId,
        int documentPage,
        FieldType fieldType,
        decimal x,
        decimal y,
        decimal? width,
        decimal? height,
        bool required,
        string? label)
    {
        return new SignatureField
        {
            Id = Guid.NewGuid(),
            SignatureFieldId = Guid.NewGuid(),
            SigningRequestId = signingRequestId,
            SignerId = signerId,
            DocumentPage = documentPage,
            FieldType = fieldType,
            X = x,
            Y = y,
            Width = width,
            Height = height,
            Required = required,
            Label = label
        };
    }

    /// <summary>
    /// Marks this field as completed with the given value.
    /// </summary>
    public void Complete(string value)
    {
        Value = value;
        CompletedAt = DateTime.UtcNow;
    }
}
