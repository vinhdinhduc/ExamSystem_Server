using ExamSystem.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ExamSystem.Background;

/// <summary>
/// Định kỳ kiểm tra phiên thi đang làm nhưng không còn heartbeat (sinh viên mất mạng / treo tab)
/// để chuyển sang chờ quản trị viên — vì client offline không gọi được API báo sự cố.
/// </summary>
public class ExamSessionHeartbeatWatchdogHostedService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(60);
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ExamSessionHeartbeatWatchdogHostedService> _logger;

    public ExamSessionHeartbeatWatchdogHostedService(
        IServiceProvider serviceProvider,
        ILogger<ExamSessionHeartbeatWatchdogHostedService> logger)
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
                await Task.Delay(Interval, stoppingToken);
                using var scope = _serviceProvider.CreateScope();
                var examSessionService = scope.ServiceProvider.GetRequiredService<IExamSessionService>();
                var paused = await examSessionService.PauseSessionsWithStaleHeartbeatAsync(stoppingToken);
                if (paused > 0)
                {
                    _logger.LogInformation(
                        "Heartbeat watchdog: đã tạm dừng {Count} phiên chờ xử lý sự cố",
                        paused);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Heartbeat watchdog lỗi không mong đợi");
            }
        }
    }
}
