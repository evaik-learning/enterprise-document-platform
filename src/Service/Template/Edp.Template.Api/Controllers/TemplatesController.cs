using Edp.Template.Application.Dto;
using Edp.Template.Api.Models;
using Edp.Template.Application.Contracts;
using Edp.Template.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace Edp.Template.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TemplatesController : ControllerBase
{
    private readonly ITemplateRepository _repository;
    private readonly ITemplateVersionRepository _versionRepository;
    private readonly ITemplateStorage _storage;
    private readonly IPlaceholderExtractor _extractor;
    private readonly IPlaceholderRepository _placeholderRepository;

    public TemplatesController(
        ITemplateRepository repository,
        ITemplateVersionRepository versionRepository,
        ITemplateStorage storage,
        IPlaceholderExtractor extractor,
        IPlaceholderRepository placeholderRepository)
    {
        _repository = repository;
        _versionRepository = versionRepository;
        _storage = storage;
        _extractor = extractor;
        _placeholderRepository = placeholderRepository;
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TemplateDto>> GetTemplate(Guid id)
    {
        var template = await _repository.GetByIdAsync(id);
        if (template is null) return NotFound();

        var dto = new TemplateDto
        {
            Id = template.Id,
            Name = template.Name,
            Description = template.Description,
            Status = template.Status
        };

        return Ok(dto);
    }

    [HttpGet]
    public async Task<ActionResult> ListTemplates([FromQuery] string? name = null, [FromQuery] string? status = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var (items, total) = await _repository.SearchAsync(name, status, page, pageSize);

        var dtos = items.Select(t => new TemplateDto
        {
            Id = t.Id,
            Name = t.Name,
            Description = t.Description,
            Status = t.Status
        });

        return Ok(new { Items = dtos, TotalCount = total, Page = page, PageSize = pageSize });
    }

    [HttpPost]
    public async Task<ActionResult<TemplateDto>> CreateTemplate(CreateTemplateRequest request)
    {
        var template = new global::Edp.Template.Domain.Entities.Template
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            Status = "Draft"
        };

        await _repository.AddAsync(template);

        var dto = new TemplateDto
        {
            Id = template.Id,
            Name = template.Name,
            Description = template.Description,
            Status = template.Status
        };

        return CreatedAtAction(nameof(GetTemplate), new { id = dto.Id }, dto);
    }

    [HttpPost("{templateId:guid}/versions")]
    public async Task<IActionResult> UploadVersion([FromRoute] Guid templateId, IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null) return BadRequest("File is required.");
        if (file.Length == 0) return BadRequest("File is empty.");
        if (file.Length > 10 * 1024 * 1024) return BadRequest("File too large (max 10 MB).");

        var allowed = new[] { 
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "application/octet-stream" // fallback for some clients
        };

        if (!allowed.Contains(file.ContentType) && Path.GetExtension(file.FileName)?.ToLowerInvariant() != ".docx")
            return BadRequest("Unsupported file type. Only DOCX allowed.");

        // read into memory so we can compute hash, extract placeholders and upload
        await using var ms = new MemoryStream();
        await file.CopyToAsync(ms, cancellationToken);
        ms.Position = 0;

        // compute hash
        ms.Position = 0;
        string fileHash;
        using (var sha = System.Security.Cryptography.SHA256.Create())
        {
            var hash = sha.ComputeHash(ms);
            fileHash = Convert.ToBase64String(hash);
        }

        ms.Position = 0;

        // determine next version
        var nextVersion = await _versionRepository.GetNextVersionNumberAsync(templateId, cancellationToken);
        var storagePath = $"{templateId}/v{nextVersion}/{file.FileName}";

        // upload to blob
        ms.Position = 0;
        await _storage.UploadAsync(ms, storagePath, file.ContentType, cancellationToken);

        // extract placeholders
        ms.Position = 0;
        var placeholders = (await _extractor.ExtractAsync(ms, cancellationToken)).ToList();

        // create version metadata
        var version = new TemplateVersion
        {
            Id = Guid.NewGuid(),
            TemplateId = templateId,
            VersionNumber = nextVersion,
            FileName = file.FileName,
            StoragePath = storagePath,
            FileHash = fileHash,
            FileSize = file.Length,
            ContentType = file.ContentType,
            ValidationStatus = "NotValidated",
            Status = "Uploaded"
        };

        await _versionRepository.AddAsync(version, cancellationToken);

        // persist placeholders via repository
        var placeholderEntities = placeholders.Select(p => new Placeholder
        {
            Id = Guid.NewGuid(),
            TemplateVersionId = version.Id,
            Name = p.Name,
            // default values for now
            DisplayName = p.Name,
            DataType = "String",
            IsRequired = false,
            DefaultValue = null,
            Format = null,
            Description = null
        }).ToList();

        if (placeholderEntities.Count > 0)
        {
            await _placeholderRepository.AddRangeAsync(placeholderEntities, cancellationToken);
        }

        var dto = new TemplateVersionDto
        {
            Id = version.Id,
            VersionNumber = version.VersionNumber,
            FileName = version.FileName,
            StoragePath = version.StoragePath,
            FileHash = version.FileHash
        };

        return CreatedAtAction(nameof(GetTemplate), new { id = templateId }, dto);
    }

    [HttpPost("{templateId:guid}/versions/{versionId:guid}/validate")]
    public async Task<IActionResult> ValidateVersion([FromRoute] Guid templateId, [FromRoute] Guid versionId, [FromServices] ITemplateValidator validator, CancellationToken cancellationToken = default)
    {
        var result = await validator.ValidateAsync(templateId, versionId, cancellationToken);

        // persist validation result
        var entity = new Edp.Template.Domain.Entities.ValidationResultEntity
        {
            Id = Guid.NewGuid(),
            TemplateVersionId = versionId,
            Status = result.IsValid ? "Valid" : "Invalid",
            ErrorCount = result.ErrorCount,
            WarningCount = result.WarningCount,
            ValidatedAt = DateTime.UtcNow
        };

        // use DbContext directly for quick persistence
        // resolve via HttpContext RequestServices
        var db = HttpContext.RequestServices.GetRequiredService<Edp.Template.Infrastructure.Persistence.TemplateDbContext>();
        await db.ValidationResults.AddAsync(entity, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return Ok(result);
    }

    [HttpGet("{templateId:guid}/versions/{versionId:guid}/validation")]
    public async Task<IActionResult> GetValidation([FromRoute] Guid templateId, [FromRoute] Guid versionId, CancellationToken cancellationToken)
    {
        var db = HttpContext.RequestServices.GetRequiredService<Edp.Template.Infrastructure.Persistence.TemplateDbContext>();
        var vr = await db.ValidationResults.Where(v => v.TemplateVersionId == versionId).OrderByDescending(v => v.ValidatedAt).FirstOrDefaultAsync(cancellationToken);
        if (vr is null) return NotFound();

        return Ok(new { Status = vr.Status, ErrorCount = vr.ErrorCount, WarningCount = vr.WarningCount, ValidatedAt = vr.ValidatedAt });
    }

    [HttpPost("{templateId:guid}/versions/{versionId:guid}/activate")]
    public async Task<IActionResult> ActivateVersion([FromRoute] Guid templateId, [FromRoute] Guid versionId, CancellationToken cancellationToken)
    {
        // load template and version
        var template = await _repository.GetByIdAsync(templateId, cancellationToken);
        if (template is null) return NotFound("Template not found");

        var version = await _versionRepository.GetByIdAsync(versionId, cancellationToken);
        if (version is null) return NotFound("Version not found");

        // check validation status
        var db = HttpContext.RequestServices.GetRequiredService<Edp.Template.Infrastructure.Persistence.TemplateDbContext>();
        var vr = await db.ValidationResults.Where(v => v.TemplateVersionId == versionId).OrderByDescending(v => v.ValidatedAt).FirstOrDefaultAsync(cancellationToken);
        if (vr == null || vr.Status != "Valid") return BadRequest("Version is not validated as Valid");

        // perform updates with optimistic concurrency using RowVersion
        try
        {
            var prevActive = await db.TemplateVersions.Where(v => v.TemplateId == templateId && v.Status == "Active").ToListAsync(cancellationToken);
            foreach (var p in prevActive)
            {
                p.Status = "Inactive";
            }

            var vEntity = await db.TemplateVersions.FirstOrDefaultAsync(v => v.Id == versionId, cancellationToken);
            if (vEntity == null) return NotFound();
            vEntity.Status = "Active";

            var tEntity = await db.Templates.FirstOrDefaultAsync(t => t.Id == templateId, cancellationToken);
            tEntity!.CurrentVersionId = versionId;
            tEntity.Status = "Active";

            await db.SaveChangesAsync(cancellationToken);
            return Ok();
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
        {
            return StatusCode(StatusCodes.Status412PreconditionFailed, "Concurrency conflict detected. Retry with latest resource state.");
        }
    }

    [HttpPost("{templateId:guid}/deactivate")]
    public async Task<IActionResult> DeactivateTemplate([FromRoute] Guid templateId, CancellationToken cancellationToken)
    {
        var db = HttpContext.RequestServices.GetRequiredService<Edp.Template.Infrastructure.Persistence.TemplateDbContext>();
        var tEntity = await db.Templates.FirstOrDefaultAsync(t => t.Id == templateId, cancellationToken);
        if (tEntity is null) return NotFound();

        tEntity.Status = "Inactive";
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return Ok();
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
        {
            return StatusCode(StatusCodes.Status412PreconditionFailed, "Concurrency conflict detected.");
        }
    }

    [HttpPost("{templateId:guid}/archive")]
    public async Task<IActionResult> ArchiveTemplate([FromRoute] Guid templateId, CancellationToken cancellationToken)
    {
        var db = HttpContext.RequestServices.GetRequiredService<Edp.Template.Infrastructure.Persistence.TemplateDbContext>();
        var tEntity = await db.Templates.FirstOrDefaultAsync(t => t.Id == templateId, cancellationToken);
        if (tEntity is null) return NotFound();

        tEntity.Status = "Archived";
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return Ok();
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
        {
            return StatusCode(StatusCodes.Status412PreconditionFailed, "Concurrency conflict detected.");
        }
    }
}
