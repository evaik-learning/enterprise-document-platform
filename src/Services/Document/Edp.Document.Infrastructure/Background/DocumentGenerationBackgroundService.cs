using Edp.Document.Application.Interfaces;
using Edp.Document.Domain.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Edp.Document.Infrastructure.Background;

public sealed class DocumentGenerationBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DocumentGenerationBackgroundService> _logger;
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(10);

    public DocumentGenerationBackgroundService(IServiceScopeFactory scopeFactory, ILogger<DocumentGenerationBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingJobsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Document generation background worker encountered an error.");
            }

            try
            {
                await Task.Delay(_pollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task ProcessPendingJobsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var jobRepository = scope.ServiceProvider.GetRequiredService<IDocumentGenerationJobRepository>();
        var documentRepository = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();
        var documentGenerationService = scope.ServiceProvider.GetRequiredService<IDocumentGenerationService>();

        var pendingJobs = await jobRepository.GetPendingAsync(cancellationToken);
        foreach (var job in pendingJobs.Where(x => x.Status == "Queued"))
        {
            if (job.Status == "Completed")
            {
                continue;
            }

            var claimSucceeded = await jobRepository.TryStartProcessingAsync(job.Id, cancellationToken);
            if (!claimSucceeded)
            {
                _logger.LogDebug("Another worker already claimed document generation job {JobId}; skipping.", job.Id);
                continue;
            }

            var document = await documentRepository.GetByIdAsync(job.OrganizationId, job.DocumentId, cancellationToken);
            if (document is null)
            {
                _logger.LogWarning("Skipping document generation job {JobId} because document {DocumentId} no longer exists.", job.Id, job.DocumentId);
                continue;
            }

            try
            {
                using (_logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = job.CorrelationId, ["DocumentId"] = job.DocumentId, ["JobId"] = job.Id }))
                {
                    _logger.LogInformation("Processing queued document generation job {JobId} for document {DocumentId}.", job.Id, document.Id);
                    await documentGenerationService.GenerateAsync(job.OrganizationId, job.DocumentId, document.Name, new Dictionary<string, object?>(), new List<string> { "DOCX", "PDF" }, cancellationToken);
                }
            }
            catch (DocumentDomainException ex)
            {
                var failureCategory = ClassifyFailure(ex.Message);
                var latestJob = await jobRepository.GetByIdAsync(job.Id, cancellationToken) ?? job;

                if (failureCategory == "Transient" && latestJob.Attempts < 3)
                {
                    latestJob.MarkRetryScheduled(ex.Message, failureCategory);
                    await jobRepository.UpdateAsync(latestJob, cancellationToken);
                    _logger.LogWarning(ex, "Transient generation failure for job {JobId}; retry scheduled.", job.Id);
                    continue;
                }

                latestJob.MarkFailed(ex.Message, failureCategory);
                await jobRepository.UpdateAsync(latestJob, cancellationToken);
                _logger.LogError(ex, "Document generation failed for job {JobId} with category {FailureCategory}.", job.Id, failureCategory);
            }
            catch (Exception ex)
            {
                var failureCategory = ClassifyFailure(ex.Message);
                var latestJob = await jobRepository.GetByIdAsync(job.Id, cancellationToken) ?? job;

                if (failureCategory == "Transient" && latestJob.Attempts < 3)
                {
                    latestJob.MarkRetryScheduled(ex.Message, failureCategory);
                    await jobRepository.UpdateAsync(latestJob, cancellationToken);
                    _logger.LogWarning(ex, "Transient worker failure for job {JobId}; retry scheduled.", job.Id);
                    continue;
                }

                latestJob.MarkFailed(ex.Message, failureCategory);
                await jobRepository.UpdateAsync(latestJob, cancellationToken);
                _logger.LogError(ex, "Failed to process queued document generation job {JobId}.", job.Id);
            }
        }
    }

    private static string ClassifyFailure(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return "Unknown";
        }

        var normalized = message.Trim();
        var transientSignals = new[] { "transient", "temporary", "timeout", "connection", "network", "service bus", "blob", "storage", "unavailable", "temporarily" };
        var validationSignals = new[] { "validation", "placeholder", "required", "missing", "invalid", "template" };

        if (transientSignals.Any(signal => normalized.Contains(signal, StringComparison.OrdinalIgnoreCase)))
        {
            return "Transient";
        }

        if (validationSignals.Any(signal => normalized.Contains(signal, StringComparison.OrdinalIgnoreCase)))
        {
            return "Validation";
        }

        return "Unknown";
    }
}
