namespace Edp.DigitalSignature.Domain.Enums;

/// <summary>
/// Types of signature fields that can be placed on a document.
/// </summary>
public enum FieldType
{
    /// <summary>A full signature field for electronic signatures.</summary>
    Signature = 0,

    /// <summary>An initials field for quick approval.</summary>
    Initials = 1,

    /// <summary>A date field, typically auto-populated with current date.</summary>
    Date = 2,

    /// <summary>A text input field for comments or additional information.</summary>
    Text = 3,

    /// <summary>A checkbox field for yes/no confirmation.</summary>
    Checkbox = 4
}
