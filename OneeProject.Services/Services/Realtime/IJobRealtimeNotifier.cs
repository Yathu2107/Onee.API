using OneeProject.Database.Model.API_Model;

namespace OneeProject.Services.Services.Realtime
{
    public interface IJobRealtimeNotifier
    {
        Task NotifyJobOfferAsync(JobModelForDetailView job);
        Task NotifyJobUpdatedAsync(JobModelForDetailView job);
        Task NotifyChatMessageAsync(
            int jobId,
            string customerId,
            string? workerId,
            JobChatMessageModel message);
    }

    public class NullJobRealtimeNotifier : IJobRealtimeNotifier
    {
        public Task NotifyJobOfferAsync(JobModelForDetailView job) => Task.CompletedTask;
        public Task NotifyJobUpdatedAsync(JobModelForDetailView job) => Task.CompletedTask;

        public Task NotifyChatMessageAsync(
            int jobId,
            string customerId,
            string? workerId,
            JobChatMessageModel message) => Task.CompletedTask;
    }
}
