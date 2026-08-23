namespace Edp.Document.Domain.Enums;

public enum DocumentStatus
{
    Requested,
    Queued,
    Generating,
    Generated,
    Failed,
    Draft,
    InReview,
    PendingApproval,
    Approved,
    Rejected,
    PendingSignature,
    Signed,
    Completed,
    Archived,
    Cancelled,
    Deleted
}

public enum GenerationStatus
{
    Queued,
    Processing,
    Completed,
    Failed,
    Cancelled
}

public enum DocumentFileType
{
    Docx,
    Pdf,
    Preview
}
