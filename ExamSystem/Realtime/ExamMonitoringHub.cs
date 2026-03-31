using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ExamSystem.Realtime;

[Authorize]
public class ExamMonitoringHub : Hub
{
    public async Task JoinExamRoom(Guid examId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"exam:{examId}");
    }

    public async Task LeaveExamRoom(Guid examId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"exam:{examId}");
    }

    public async Task JoinMonitoringRoom()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "exam-monitoring");
    }

    public async Task LeaveMonitoringRoom()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "exam-monitoring");
    }
}
