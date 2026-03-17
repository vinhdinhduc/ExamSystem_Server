using ExamSystem.Services.Interfaces;

namespace ExamSystem.Services;

public class ExpiredSessionAutoSubmitHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ExpiredSessionAutoSubmitHostedService> _logger;

    public ExpiredSessionAutoSubmitHostedService(
        IServiceProvider serviceProvider,
        ILogger<ExpiredSessionAutoSubmitHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var examSessionService = scope.ServiceProvider.GetRequiredService<IExamSessionService>();
                var count = await examSessionService.AutoSubmitExpiredSessionsAsync();
                if (count > 0)
                {
                    _logger.LogInformation("Auto submitted {Count} expired exam sessions", count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to auto submit expired sessions");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
