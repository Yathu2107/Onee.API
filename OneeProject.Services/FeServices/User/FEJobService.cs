using OneeProject.Database.Common;
using OneeProject.Database.Context;
using OneeProject.Database.Model.API_Model;
using OneeProject.Database.Model.FEAPI_Model.User;
using OneeProject.Services.Services;

namespace OneeProject.Services.FeServices.User
{
    public class FEJobService(
        JobService jobService,
        JobMatchService jobMatchService,
        JobRatingService jobRatingService)
    {
        private readonly JobService _jobService = jobService;
        private readonly JobMatchService _jobMatchService = jobMatchService;
        private readonly JobRatingService _jobRatingService = jobRatingService;

        public Task<Message<JobMatchResultModel>> FindWorkersAsync(string text, string customerId)
            => _jobMatchService.FindWorkersByTextAsync(text, customerId);

        public Task<Message<JobMatchResultModel>> FindWorkersByCategoryAsync(int categoryId, string customerId)
            => _jobMatchService.FindWorkersByCategoryIdAsync(categoryId, customerId);

        public Task<Message<JobModelForDetailView>> CreateAsync(FEJobCreateRequest model, string customerId)
            => _jobService.CreateJobAsync(new JobCreateModel
            {
                Text = model.Text,
                UserId = customerId,
                WorkerIds = model.WorkerIds ?? new List<string>(),
                AddressId = model.AddressId
            }, customerId);

        public Task<List<JobModelForTable>> GetMineAsync(string customerId)
            => _jobService.GetJobsByCustomerAsync(customerId);

        public async Task<Message<JobModelForDetailView>> GetAsync(int jobId, string customerId)
        {
            var detail = await _jobService.GetJobDetailAsync(jobId);
            if (detail == null)
                return Err("Job not found.", "404");
            if (detail.FK_customer_ID != customerId)
                return Err("You are not allowed to view this job.", "403");
            return Ok(detail, "Job loaded.");
        }

        public async Task<Message<JobModelForDetailView>> CancelAsync(
            int jobId,
            string customerId,
            string reason)
        {
            var detail = await _jobService.GetJobDetailAsync(jobId);
            if (detail == null)
                return Err("Job not found.", "404");
            if (detail.FK_customer_ID != customerId)
                return Err("Only the customer can cancel this job.", "403");

            // Customer may cancel while Offering (before worker accept) or after Accepted.
            if (detail.Status != JobStatuses.Offering && detail.Status != JobStatuses.Accepted)
            {
                return Err(
                    $"Job cannot be cancelled in '{detail.Status}' status. Cancel is allowed while Offering or Accepted.",
                    "400");
            }

            return await _jobService.CancelAcceptedJobAsync(jobId, customerId, reason ?? string.Empty);
        }

        public async Task<Message<JobChatMessageModel>> SendChatAsync(
            int jobId,
            string customerId,
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

            if (detail.FK_customer_ID != customerId)
            {
                return new Message<JobChatMessageModel>
                {
                    Status = "E",
                    Text = "You are not a participant of this job.",
                    Code = "403"
                };
            }

            return await _jobService.SendChatMessageAsync(jobId, customerId, message);
        }

        public async Task<Message<List<JobChatMessageModel>>> GetChatAsync(int jobId, string customerId)
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

            if (detail.FK_customer_ID != customerId)
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

        public async Task<Message<JobRatingModelForView>> AddRatingAsync(
            int jobId,
            string customerId,
            FEJobRatingRequest model)
        {
            var detail = await _jobService.GetJobDetailAsync(jobId);
            if (detail == null)
            {
                return new Message<JobRatingModelForView>
                {
                    Status = "E",
                    Text = "Job not found.",
                    Code = "404"
                };
            }

            if (detail.FK_customer_ID != customerId)
            {
                return new Message<JobRatingModelForView>
                {
                    Status = "E",
                    Text = "Only the customer can rate this job.",
                    Code = "403"
                };
            }

            return await _jobRatingService.AddRatingAsync(
                jobId,
                new JobRatingModelForInsert
                {
                    Rating = model.Rating,
                    Feedback = model.Feedback
                },
                customerId);
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
