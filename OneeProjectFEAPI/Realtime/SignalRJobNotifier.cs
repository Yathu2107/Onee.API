using Microsoft.AspNetCore.SignalR;
using OneeProject.Database.Model.API_Model;
using OneeProject.Services.Services.Realtime;
using OneeProjectFEAPI.Hubs;

namespace OneeProjectFEAPI.Realtime
{
    public class SignalRJobNotifier(IHubContext<JobHub> hub) : IJobRealtimeNotifier
    {
        private readonly IHubContext<JobHub> _hub = hub;

        public async Task NotifyJobOfferAsync(JobModelForDetailView job)
        {
            if (string.IsNullOrEmpty(job.FK_worker_ID))
                return;

            await _hub.Clients.Group(JobHub.UserGroup(job.FK_worker_ID))
                .SendAsync("JobOffer", job);

            await _hub.Clients.Group(JobHub.UserGroup(job.FK_customer_ID))
                .SendAsync("JobUpdated", job);
        }

        public async Task NotifyJobUpdatedAsync(JobModelForDetailView job)
        {
            await _hub.Clients.Group(JobHub.UserGroup(job.FK_customer_ID))
                .SendAsync("JobUpdated", job);

            if (!string.IsNullOrEmpty(job.FK_worker_ID))
            {
                await _hub.Clients.Group(JobHub.UserGroup(job.FK_worker_ID))
                    .SendAsync("JobUpdated", job);
            }

            await _hub.Clients.Group(JobHub.JobGroup(job.Id))
                .SendAsync("JobUpdated", job);
        }

        public async Task NotifyChatMessageAsync(
            int jobId,
            string customerId,
            string? workerId,
            JobChatMessageModel message)
        {
            await _hub.Clients.Group(JobHub.JobGroup(jobId))
                .SendAsync("ChatMessage", message);

            await _hub.Clients.Group(JobHub.UserGroup(customerId))
                .SendAsync("ChatMessage", message);

            if (!string.IsNullOrEmpty(workerId))
            {
                await _hub.Clients.Group(JobHub.UserGroup(workerId))
                    .SendAsync("ChatMessage", message);
            }
        }
    }
}
