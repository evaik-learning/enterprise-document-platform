using Edp.Workflow.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Edp.Workflow.Infrastructure.BackgroundJobs;

public sealed class ApprovalTimeoutWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ApprovalTimeoutWorker> _logger;
    private readonly TimeSpan _interval;

    public ApprovalTimeoutWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<ApprovalTimeoutWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _interval = TimeSpan.FromSeconds(30);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var timeoutService = scope.ServiceProvider.GetRequiredService<IApprovalTimeoutService>();
                var expiredCount = await timeoutService.ProcessExpiredAsync(100, stoppingToken);
                if (expiredCount > 0)
                    _logger.LogInformation("Expired {ApprovalTaskCount} workflow approval tasks", expiredCount);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Approval timeout processing failed");
            }
        }
    }
}
