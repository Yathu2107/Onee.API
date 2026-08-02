using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace OneeProjectFEAPI.Hubs
{
    [Authorize]
    public class JobHub : Hub
    {
        public static string UserGroup(string userId) => $"user:{userId}";
        public static string JobGroup(int jobId) => $"job:{jobId}";

        public override async Task OnConnectedAsync()
        {
            var uid = Context.User?.FindFirst("uid")?.Value
                ?? Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrWhiteSpace(uid))
                await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(uid));

            await base.OnConnectedAsync();
        }

        public async Task JoinJob(int jobId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, JobGroup(jobId));
        }

        public async Task LeaveJob(int jobId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, JobGroup(jobId));
        }
    }
}
