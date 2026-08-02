using OneeProject.Database.Common;
using OneeProject.Database.Context;
using OneeProject.Database.Model.API_Model;
using OneeProject.Services.Services;

namespace OneeProject.Services.FeServices.Worker
{
    public class FEWorkerJobService(JobService jobService)
    {
        private readonly JobService _jobService = jobService;

        public Task<List<JobModelForDetailView>> GetOffersAsync(string workerId)
            => _jobService.GetOfferingJobsForWorkerAsync(workerId);

        public Task<List<JobModelForTable>> GetMineAsync(string workerId)
            => _jobService.GetJobsByWorkerAsync(workerId);

        public async Task<Message<JobModelForDetailView>> GetAsync(int jobId, string workerId)
        {
            var detail = await _jobService.GetJobDetailAsync(jobId);
            if (detail == null)
                return Err("Job not found.", "404");
            if (detail.FK_worker_ID != workerId)
                return Err("You are not allowed to view this job.", "403");
            return Ok(detail, "Job loaded.");
        }

        public async Task<Message<JobModelForDetailView>> AcceptAsync(int jobId, string workerId)
        {
            var detail = await _jobService.GetJobDetailAsync(jobId);
            if (detail == null)
                return Err("Job not found.", "404");
            if (detail.Status != JobStatuses.Offering || detail.FK_worker_ID != workerId)
                return Err("This offer is not assigned to you.", "403");

            return await _jobService.AcceptOfferAsync(jobId, workerId);
        }

        public async Task<Message<JobModelForDetailView>> DeclineAsync(int jobId, string workerId)
        {
            var detail = await _jobService.GetJobDetailAsync(jobId);
            if (detail == null)
                return Err("Job not found.", "404");
            if (detail.Status != JobStatuses.Offering || detail.FK_worker_ID != workerId)
                return Err("This offer is not assigned to you.", "403");

            return await _jobService.CancelOfferAsync(jobId, workerId);
        }

        public async Task<Message<JobModelForDetailView>> ConfirmAsync(
            int jobId,
            string workerId,
            decimal amount)
        {
            var detail = await _jobService.GetJobDetailAsync(jobId);
            if (detail == null)
                return Err("Job not found.", "404");
            if (detail.FK_worker_ID != workerId)
                return Err("Only the assigned worker can confirm the amount.", "403");

            return await _jobService.ConfirmJobAsync(jobId, workerId, amount);
        }

        public async Task<Message<JobModelForDetailView>> CompleteAsync(int jobId, string workerId)
        {
            var detail = await _jobService.GetJobDetailAsync(jobId);
            if (detail == null)
                return Err("Job not found.", "404");
            if (detail.FK_worker_ID != workerId)
                return Err("Only the assigned worker can complete this job.", "403");

            return await _jobService.CompleteJobAsync(jobId, workerId);
        }

        public async Task<Message<JobChatMessageModel>> SendChatAsync(
            int jobId,
            string workerId,
            string message)
        {
            var detail = await _jobService.GetJobDetailAsync(jobId);
            if (detail == null)
            {
                return new Message<JobChatMessageModel>
                {
                    Status = "E",
                    Text = "Job not found.",
                    Code = "404"
                };
            }

            if (detail.FK_worker_ID != workerId)
            {
                return new Message<JobChatMessageModel>
                {
                    Status = "E",
                    Text = "You are not a participant of this job.",
                    Code = "403"
                };
            }

            return await _jobService.SendChatMessageAsync(jobId, workerId, message);
        }

        public async Task<Message<List<JobChatMessageModel>>> GetChatAsync(int jobId, string workerId)
        {
            var detail = await _jobService.GetJobDetailAsync(jobId);
            if (detail == null)
            {
                return new Message<List<JobChatMessageModel>>
                {
                    Status = "E",
                    Text = "Job not found.",
                    Code = "404"
                };
            }

            if (detail.FK_worker_ID != workerId)
            {
                return new Message<List<JobChatMessageModel>>
                {
                    Status = "E",
                    Text = "You are not a participant of this job.",
                    Code = "403"
                };
            }

            var messages = await _jobService.GetChatMessagesAsync(jobId);
            return new Message<List<JobChatMessageModel>>
            {
                Status = "S",
                Text = "Messages loaded.",
                Code = "200",
                Result = messages
            };
        }

        private static Message<JobModelForDetailView> Ok(JobModelForDetailView detail, string text) => new()
        {
            Status = "S",
            Text = text,
            Code = "200",
            Result = detail
        };

        private static Message<JobModelForDetailView> Err(string text, string code) => new()
        {
            Status = "E",
            Text = text,
            Code = code,
            Result = null
        };
    }
}
