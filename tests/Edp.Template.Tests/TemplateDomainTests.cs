using Edp.Template.Domain.Entities;
using Edp.Template.Domain.Enums;
using Edp.Template.Domain.Events;
using Edp.Template.Domain.Exceptions;
using Xunit;

namespace Edp.Template.Tests;

public class TemplateDomainTests
{
    private static readonly Guid OrganizationId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public void CreateNormalizesCodeAndDefaultsToDraft()
    {
        var template = global::Edp.Template.Domain.Entities.Template.Create(Guid.NewGuid(), OrganizationId, "Employee Offer", "  emp-offer  ", "desc", UserId);

        Assert.Equal("EMP-OFFER", template.Code);
        Assert.Equal(TemplateStatus.Draft, template.Status);
        Assert.Equal(OrganizationId, template.OrganizationId);
        Assert.Single(template.DomainEvents);
        Assert.IsType<TemplateCreatedDomainEvent>(template.DomainEvents.Single());
    }

    [Fact]
    public void CreateWithoutOrganizationThrows()
    {
        Assert.Throws<TemplateDomainException>(() =>
            global::Edp.Template.Domain.Entities.Template.Create(Guid.NewGuid(), Guid.Empty, "Name", "CODE", null, UserId));
    }

    [Fact]
    public void ActivateVersionSetsCurrentVersionAndStatus()
    {
        var template = CreateTemplate();
        var versionId = Guid.NewGuid();

        template.ActivateVersion(versionId, UserId);

        Assert.Equal(versionId, template.CurrentVersionId);
        Assert.Equal(TemplateStatus.Active, template.Status);
    }

    [Fact]
    public void ArchiveThenActivateThrows()
    {
        var template = CreateTemplate();
        template.Archive(UserId);

        Assert.Equal(TemplateStatus.Archived, template.Status);
        Assert.Throws<TemplateDomainException>(() => template.ActivateVersion(Guid.NewGuid(), UserId));
    }

    [Fact]
    public void ArchiveThenUpdateDetailsThrows()
    {
        var template = CreateTemplate();
        template.Archive(UserId);

        Assert.Throws<TemplateDomainException>(() => template.UpdateDetails("New name", null, UserId));
    }

    [Fact]
    public void TemplateVersionCreateStartsAsUploadedAndNotValidated()
    {
        var version = CreateVersion(1);

        Assert.Equal(TemplateVersionStatus.Uploaded, version.Status);
        Assert.Equal(ValidationStatus.NotValidated, version.ValidationStatus);
        Assert.Contains(version.DomainEvents, e => e is TemplateVersionCreatedDomainEvent);
    }

    [Fact]
    public void TemplateVersionActivateWithoutValidPassingThrows()
    {
        var version = CreateVersion(1);

        Assert.Throws<TemplateDomainException>(() => version.Activate());
    }

    [Fact]
    public void TemplateVersionActivateAfterValidationSucceeds()
    {
        var version = CreateVersion(1);
        version.MarkValidated(isValid: true);

        version.Activate();

        Assert.Equal(TemplateVersionStatus.Active, version.Status);
    }

    [Fact]
    public void TemplateVersionVersionNumberMustBePositive()
    {
        Assert.Throws<TemplateDomainException>(() => TemplateVersion.Create(
            Guid.NewGuid(), Guid.NewGuid(), OrganizationId, 0, "f.docx", "templates", "path", "hash", 10, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", null, UserId));
    }

    private static global::Edp.Template.Domain.Entities.Template CreateTemplate() =>
        global::Edp.Template.Domain.Entities.Template.Create(Guid.NewGuid(), OrganizationId, "Contract", "CONTRACT", null, UserId);

    private static TemplateVersion CreateVersion(int versionNumber) => TemplateVersion.Create(
        Guid.NewGuid(),
        Guid.NewGuid(),
        OrganizationId,
        versionNumber,
        "contract.docx",
        "templates",
        "organizations/org/templates/tpl/versions/v1/contract.docx",
        "hash",
        1024,
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        null,
        UserId);
}
