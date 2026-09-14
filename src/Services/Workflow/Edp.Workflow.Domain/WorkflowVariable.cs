using Edp.SharedKernel.Entities;

namespace Edp.Workflow.Domain;

public sealed class WorkflowVariable : AuditableEntity<Guid>
{
    public Guid WorkflowInstanceId { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = null!;
    public string Value { get; set; } = null!;
    public string DataType { get; set; } = "string";
    public bool IsSensitive { get; set; }

    public WorkflowVariable() { }

    public WorkflowVariable(string name, string value, string dataType = "string")
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Variable name cannot be empty", nameof(name));

        Id = Guid.NewGuid();
        Name = name;
        Value = value;
        DataType = dataType;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public WorkflowVariable(Guid workflowInstanceId, Guid organizationId, string name, string value, string dataType = "string")
        : this(name, value, dataType)
    {
        WorkflowInstanceId = workflowInstanceId;
        OrganizationId = organizationId;
    }

    public void Set(string value, string? dataType = null)
    {
        Value = value;
        if (!string.IsNullOrWhiteSpace(dataType))
            DataType = dataType;
        ModifiedAt = DateTimeOffset.UtcNow;
    }
}