using Microsoft.EntityFrameworkCore;
using OneeProject.Database.Common;
using OneeProject.Database.Context;
using OneeProject.Database.Model.API_Model;
using OneeProject.Services.Services.Realtime;
using System.Net.Http.Json;
using System.Text.Json;

namespace OneeProject.Services.Services
{
    /// <summary>
    /// Job lifecycle shared by Admin (poll) and FEAPI (SignalR/FCM via IJobRealtimeNotifier).
    /// </summary>
    public class JobService(
        AppDbContext context,
        IHttpClientFactory httpClientFactory,
        IJobRealtimeNotifier notifier)
    {
        private readonly AppDbContext _context = context;
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
        private readonly IJobRealtimeNotifier _notifier = notifier;
        private static readonly TimeSpan OfferTimeout = TimeSpan.FromMinutes(1);
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public async Task<Message<JobModelForDetailView>> CreateJobAsync(
            JobCreateModel model,
            string createdBy)
        {
            if (string.IsNullOrWhiteSpace(model.Text) || string.IsNullOrWhiteSpace(model.UserId))
                return ErrorDetail("Text and UserId are required.", "400");

            var workerIds = (model.WorkerIds ?? new List<string>())
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct()
                .ToList();

            if (workerIds.Count == 0)
                return ErrorDetail("At least one worker id is required.", "400");

            var customer = await _context.Users.FirstOrDefaultAsync(u => u.Id == model.UserId);
            if (customer == null)
                return ErrorDetail("Customer not found.", "404");

            double latitude;
            double longitude;

            if (model.AddressId.HasValue)
            {
                var address = await _context.SavedAddresses
                    .FirstOrDefaultAsync(a => a.Id == model.AddressId.Value && a.FK_user_ID == customer.Id);
                if (address == null)
                    return ErrorDetail("Saved address not found for this customer.", "404");
                latitude = address.Latitude;
                longitude = address.Longitude;
            }
            else
            {
                var defaultAddress = await _context.SavedAddresses
                    .FirstOrDefaultAsync(a => a.FK_user_ID == customer.Id && a.Is_Default);
                if (defaultAddress == null)
                    return ErrorDetail("Customer has no saved address. Add a default address first.", "400");

                latitude = defaultAddress.Latitude;
                longitude = defaultAddress.Longitude;
            }

            var validWorkers = await _context.Users
                .Where(u => workerIds.Contains(u.Id)
                    && u.UserType == "Worker"
                    && u.IsActive
                    && u.IsOnline)
                .Select(u => u.Id)
                .ToListAsync();

            // Preserve admin order
            var orderedValid = workerIds.Where(id => validWorkers.Contains(id)).ToList();
            if (orderedValid.Count == 0)
                return ErrorDetail("No valid active and online workers found in the provided list.", "400");

            var aiResult = await PredictCategoryAsync(model.Text.Trim());
            if (aiResult.Status != "S" || aiResult.Result == null)
                return ErrorDetail(aiResult.Text, aiResult.Code);

            var predictedCategory = aiResult.Result.Category?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(predictedCategory))
                return ErrorDetail("Could not predict a category for this problem.", "400");

            var category = await _context.Categories
                .Where(c => !c.Isdelete && c.Category_Name.ToLower() == predictedCategory.ToLower())
                .FirstOrDefaultAsync();

            if (category == null)
                return ErrorDetail("Predicted category not found in the system.", "404");

            var firstWorkerId = orderedValid[0];
            var now = CommonResources.LocalDatetime();

            var job = new Job
            {
                Problem_Text = model.Text.Trim(),
                Category_id = category.Id,
                FK_customer_ID = customer.Id,
                FK_worker_ID = firstWorkerId,
                Status = JobStatuses.Offering,
                Offer_Expires_At = now.Add(OfferTimeout),
                Queued_Worker_Ids = JsonSerializer.Serialize(orderedValid),
                Tried_Worker_Ids = JsonSerializer.Serialize(new List<string> { firstWorkerId }),
                Customer_Latitude = latitude,
                Customer_Longitude = longitude,
                CreatedBy = createdBy,
                CreatedOn = now,
                LastUpdatedBy = createdBy,
                LastUpdatedOn = now
            };

            _context.Jobs.Add(job);
            await _context.SaveChangesAsync();

            var created = await GetJobDetailAsync(job.Id);
            if (created != null)
                await _notifier.NotifyJobOfferAsync(created);

            return new Message<JobModelForDetailView>
            {
                Status = "S",
                Text = "Job created. Offer sent to first worker.",
                Code = "200",
                Result = created
            };
        }

        /// <summary>
        /// Admin marks current offer as accepted (on behalf of current worker).
        /// </summary>
        public async Task<Message<JobModelForDetailView>> AcceptOfferAsync(int jobId, string updatedBy)
        {
            var job = await _context.Jobs.FirstOrDefaultAsync(j => j.Id == jobId);
            if (job == null)
                return ErrorDetail("Job not found.", "404");

            if (job.Status != JobStatuses.Offering)
                return ErrorDetail("Job is not awaiting an offer response.", "400");

            if (string.IsNullOrEmpty(job.FK_worker_ID))
                return ErrorDetail("No worker is currently offered this job.", "400");

            var now = CommonResources.LocalDatetime();
            job.Status = JobStatuses.Accepted;
            job.Offer_Expires_At = null;
            job.LastUpdatedBy = updatedBy;
            job.LastUpdatedOn = now;
            await _context.SaveChangesAsync();

            return await OkDetailNotifyUpdated(job.Id, "Job accepted successfully. Chat is now open.");
        }

        /// <summary>
        /// Admin cancels current offer → immediately advance to next queued worker.
        /// </summary>
        public async Task<Message<JobModelForDetailView>> CancelOfferAsync(int jobId, string updatedBy)
        {
            var job = await _context.Jobs.FirstOrDefaultAsync(j => j.Id == jobId);
            if (job == null)
                return ErrorDetail("Job not found.", "404");

            if (job.Status != JobStatuses.Offering)
                return ErrorDetail("Job is not in offering status.", "400");

            await AdvanceOfferAsync(job, updatedBy);
            return await OkDetailNotifyOfferOrUpdated(job.Id, "Offer cancelled. Moved to next worker if available.");
        }

        public async Task AdvanceOfferAsync(Job job, string updatedBy)
        {
            var queue = DeserializeIds(job.Queued_Worker_Ids);
            var tried = DeserializeIds(job.Tried_Worker_Ids);

            if (!string.IsNullOrEmpty(job.FK_worker_ID) && !tried.Contains(job.FK_worker_ID))
                tried.Add(job.FK_worker_ID);

            var eligibleIds = await _context.Users
                .Where(u => queue.Contains(u.Id)
                    && u.UserType == "Worker"
                    && u.IsActive
                    && u.IsOnline)
                .Select(u => u.Id)
                .ToListAsync();

            var nextWorkerId = queue.FirstOrDefault(id => !tried.Contains(id) && eligibleIds.Contains(id));

            var now = CommonResources.LocalDatetime();
            job.Tried_Worker_Ids = JsonSerializer.Serialize(tried);
            job.LastUpdatedBy = updatedBy;
            job.LastUpdatedOn = now;

            if (nextWorkerId == null)
            {
                job.Status = JobStatuses.Failed;
                job.FK_worker_ID = null;
                job.Offer_Expires_At = null;
                await _context.SaveChangesAsync();

                var failed = await GetJobDetailAsync(job.Id);
                if (failed != null)
                    await _notifier.NotifyJobUpdatedAsync(failed);
                return;
            }

            tried.Add(nextWorkerId);
            job.Tried_Worker_Ids = JsonSerializer.Serialize(tried);
            job.FK_worker_ID = nextWorkerId;
            job.Status = JobStatuses.Offering;
            job.Offer_Expires_At = now.Add(OfferTimeout);
            await _context.SaveChangesAsync();

            var offered = await GetJobDetailAsync(job.Id);
            if (offered != null)
                await _notifier.NotifyJobOfferAsync(offered);
        }

        public async Task ProcessExpiredOffersAsync()
        {
            var now = CommonResources.LocalDatetime();
            var expired = await _context.Jobs
                .Where(j => j.Status == JobStatuses.Offering
                            && j.Offer_Expires_At != null
                            && j.Offer_Expires_At <= now)
                .ToListAsync();

            foreach (var job in expired)
                await AdvanceOfferAsync(job, "system");
        }

        public async Task<Message<JobModelForDetailView>> ConfirmJobAsync(
            int jobId,
            string updatedBy,
            decimal amount)
        {
            if (amount <= 0)
                return ErrorDetail("Amount must be greater than zero.", "400");

            var job = await _context.Jobs.FirstOrDefaultAsync(j => j.Id == jobId);
            if (job == null)
                return ErrorDetail("Job not found.", "404");

            if (job.Status != JobStatuses.Accepted)
                return ErrorDetail("Job can only be confirmed from Accepted status.", "400");

            var now = CommonResources.LocalDatetime();
            job.Amount = amount;
            job.Status = JobStatuses.Ongoing;
            job.LastUpdatedBy = updatedBy;
            job.LastUpdatedOn = now;
            await _context.SaveChangesAsync();

            return await OkDetailNotifyUpdated(job.Id, "Job confirmed successfully.");
        }

        /// <summary>
        /// Customer/admin cancel. Allowed while Offering (before worker accepts) or Accepted.
        /// Ending Offering stops the offer cascade / timeout advancement.
        /// </summary>
        public async Task<Message<JobModelForDetailView>> CancelAcceptedJobAsync(
            int jobId,
            string updatedBy,
            string reason)
        {
            var job = await _context.Jobs.FirstOrDefaultAsync(j => j.Id == jobId);
            if (job == null)
                return ErrorDetail("Job not found.", "404");

            if (job.Status != JobStatuses.Accepted && job.Status != JobStatuses.Offering)
                return ErrorDetail("Job can only be cancelled while Offering or Accepted.", "400");

            var wasOffering = job.Status == JobStatuses.Offering;

            // Reason is required after accept; optional while still offering.
            if (!wasOffering && string.IsNullOrWhiteSpace(reason))
                return ErrorDetail("Cancel reason is required.", "400");

            var now = CommonResources.LocalDatetime();
            job.Status = JobStatuses.Cancelled;
            job.Cancel_Reason = string.IsNullOrWhiteSpace(reason)
                ? "Cancelled by customer before a worker accepted."
                : reason.Trim();
            job.Offer_Expires_At = null;
            job.LastUpdatedBy = updatedBy;
            job.LastUpdatedOn = now;
            await _context.SaveChangesAsync();

            var message = wasOffering
                ? "Job cancelled successfully. Offer to workers has been stopped."
                : "Job cancelled successfully.";

            return await OkDetailNotifyUpdated(job.Id, message);
        }

        public async Task<Message<JobModelForDetailView>> CompleteJobAsync(int jobId, string updatedBy)
        {
            var job = await _context.Jobs.FirstOrDefaultAsync(j => j.Id == jobId);
            if (job == null)
                return ErrorDetail("Job not found.", "404");

            if (job.Status != JobStatuses.Ongoing)
                return ErrorDetail("Job can only be completed from Ongoing status.", "400");

            var now = CommonResources.LocalDatetime();
            job.Status = JobStatuses.Completed;
            job.LastUpdatedBy = updatedBy;
            job.LastUpdatedOn = now;
            await _context.SaveChangesAsync();

            return await OkDetailNotifyUpdated(job.Id, "Job completed successfully.");
        }

        public async Task<Message<JobChatMessageModel>> SendChatMessageAsync(
            int jobId,
            string senderId,
            string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return new Message<JobChatMessageModel>
                {
                    Status = "E",
                    Text = "Message is required.",
                    Code = "400"
                };
            }

            if (string.IsNullOrWhiteSpace(senderId))
            {
                return new Message<JobChatMessageModel>
                {
                    Status = "E",
                    Text = "SenderId is required.",
                    Code = "400"
                };
            }

            var job = await _context.Jobs.FirstOrDefaultAsync(j => j.Id == jobId);
            if (job == null)
            {
                return new Message<JobChatMessageModel>
                {
                    Status = "E",
                    Text = "Job not found.",
                    Code = "404"
                };
            }

            if (job.Status != JobStatuses.Accepted && job.Status != JobStatuses.Ongoing)
            {
                return new Message<JobChatMessageModel>
                {
                    Status = "E",
                    Text = "Chat is only available when job is Accepted or Ongoing.",
                    Code = "400"
                };
            }

            var isParticipant = senderId == job.FK_customer_ID || senderId == job.FK_worker_ID;
            if (!isParticipant)
            {
                return new Message<JobChatMessageModel>
                {
                    Status = "E",
                    Text = "SenderId must be the customer or the assigned worker.",
                    Code = "400"
                };
            }

            var entity = new JobChatMessage
            {
                FK_job_ID = jobId,
                FK_sender_ID = senderId,
                Message = message.Trim(),
                CreatedOn = CommonResources.LocalDatetime()
            };
            _context.JobChatMessages.Add(entity);
            await _context.SaveChangesAsync();

            var senderName = await _context.Users
                .Where(u => u.Id == senderId)
                .Select(u => u.Name)
                .FirstOrDefaultAsync() ?? "";

            var chatModel = new JobChatMessageModel
            {
                Id = entity.Id,
                FK_job_ID = entity.FK_job_ID,
                FK_sender_ID = entity.FK_sender_ID,
                Sender_Name = senderName,
                Message = entity.Message,
                CreatedOn = entity.CreatedOn
            };

            await _notifier.NotifyChatMessageAsync(
                jobId, job.FK_customer_ID, job.FK_worker_ID, chatModel);

            return new Message<JobChatMessageModel>
            {
                Status = "S",
                Text = "Message sent.",
                Code = "200",
                Result = chatModel
            };
        }

        public async Task<List<JobChatMessageModel>> GetChatMessagesAsync(int jobId)
        {
            return await _context.JobChatMessages
                .Where(m => m.FK_job_ID == jobId)
                .OrderBy(m => m.CreatedOn)
                .Select(m => new JobChatMessageModel
                {
                    Id = m.Id,
                    FK_job_ID = m.FK_job_ID,
                    FK_sender_ID = m.FK_sender_ID,
                    Sender_Name = _context.Users
                        .Where(u => u.Id == m.FK_sender_ID)
                        .Select(u => u.Name)
                        .FirstOrDefault() ?? "",
                    Message = m.Message,
                    CreatedOn = m.CreatedOn
                })
                .ToListAsync();
        }

        public async Task<JobModelForDetailView?> GetJobDetailAsync(int jobId)
        {
            var job = await _context.Jobs.FirstOrDefaultAsync(j => j.Id == jobId);
            if (job == null) return null;

            var rating = await GetJobRatingAsync(jobId);

            return new JobModelForDetailView
            {
                Id = job.Id,
                Problem_Text = job.Problem_Text,
                Category_id = job.Category_id,
                Category_Name = await _context.Categories
                    .Where(c => c.Id == job.Category_id)
                    .Select(c => c.Category_Name)
                    .FirstOrDefaultAsync() ?? "",
                FK_customer_ID = job.FK_customer_ID,
                Customer_Name = await _context.Users
                    .Where(u => u.Id == job.FK_customer_ID)
                    .Select(u => u.Name)
                    .FirstOrDefaultAsync() ?? "",
                FK_worker_ID = job.FK_worker_ID,
                Worker_Name = job.FK_worker_ID == null
                    ? null
                    : await _context.Users
                        .Where(u => u.Id == job.FK_worker_ID)
                        .Select(u => u.Name)
                        .FirstOrDefaultAsync(),
                Status = job.Status,
                Amount = job.Amount,
                Cancel_Reason = job.Cancel_Reason,
                Offer_Expires_At = job.Offer_Expires_At,
                Queued_Worker_Ids = DeserializeIds(job.Queued_Worker_Ids),
                Tried_Worker_Ids = DeserializeIds(job.Tried_Worker_Ids),
                Customer_Latitude = job.Customer_Latitude,
                Customer_Longitude = job.Customer_Longitude,
                CreatedOn = job.CreatedOn,
                Messages = await GetChatMessagesAsync(jobId),
                HasRating = rating != null,
                Rating = rating
            };
        }

        private async Task<JobRatingModelForView?> GetJobRatingAsync(int jobId)
        {
            return await _context.JobRatings
                .Where(r => r.FK_job_ID == jobId)
                .Select(r => new JobRatingModelForView
                {
                    Id = r.Id,
                    FK_job_ID = r.FK_job_ID,
                    Problem_Text = _context.Jobs
                        .Where(j => j.Id == r.FK_job_ID)
                        .Select(j => j.Problem_Text)
                        .FirstOrDefault() ?? "",
                    FK_worker_ID = r.FK_worker_ID,
                    Worker_Name = _context.Users
                        .Where(u => u.Id == r.FK_worker_ID)
                        .Select(u => u.Name)
                        .FirstOrDefault() ?? "",
                    FK_customer_ID = r.FK_customer_ID,
                    Customer_Name = _context.Users
                        .Where(u => u.Id == r.FK_customer_ID)
                        .Select(u => u.Name)
                        .FirstOrDefault() ?? "",
                    Rating = r.Rating,
                    Feedback = r.Feedback,
                    CreatedBy = r.CreatedBy,
                    CreatedOn = r.CreatedOn
                })
                .FirstOrDefaultAsync();
        }

        public async Task<JobModelWithPagination> GetAllWithPagination(
            int page,
            int itemsPerPage,
            string? search,
            string? status)
        {
            var query = _context.Jobs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(j => j.Status == status);

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(j =>
                    j.Problem_Text.Contains(search) ||
                    _context.Users.Any(u => u.Id == j.FK_customer_ID && u.Name.Contains(search)) ||
                    _context.Users.Any(u => u.Id == j.FK_worker_ID && u.Name.Contains(search)));
            }

            var count = await query.CountAsync();
            var jobs = await query
                .OrderByDescending(j => j.Id)
                .Skip((page - 1) * itemsPerPage)
                .Take(itemsPerPage)
                .Select(j => new JobModelForTable
                {
                    Id = j.Id,
                    Problem_Text = j.Problem_Text,
                    Category_Name = _context.Categories
                        .Where(c => c.Id == j.Category_id)
                        .Select(c => c.Category_Name)
                        .FirstOrDefault() ?? "",
                    Customer_Name = _context.Users
                        .Where(u => u.Id == j.FK_customer_ID)
                        .Select(u => u.Name)
                        .FirstOrDefault() ?? "",
                    Worker_Name = j.FK_worker_ID == null
                        ? null
                        : _context.Users
                            .Where(u => u.Id == j.FK_worker_ID)
                            .Select(u => u.Name)
                            .FirstOrDefault(),
                    Status = j.Status,
                    Amount = j.Amount,
                    Offer_Expires_At = j.Offer_Expires_At,
                    CreatedOn = j.CreatedOn
                })
                .ToListAsync();

            return new JobModelWithPagination { Count = count, Jobs = jobs };
        }

        private async Task<Message<OneeAiPredictData>> PredictCategoryAsync(string text)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("OneeeAi");
                using var response = await client.PostAsJsonAsync(
                    "/api/v1/predict",
                    new OneeAiPredictRequest { Text = text });
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return new Message<OneeAiPredictData>
                    {
                        Status = "E",
                        Text = "AI prediction service failed.",
                        Code = ((int)response.StatusCode).ToString()
                    };
                }

                var parsed = JsonSerializer.Deserialize<OneeAiPredictResponse>(body, JsonOptions);
                if (parsed == null || !parsed.Success || parsed.Data == null)
                {
                    return new Message<OneeAiPredictData>
                    {
                        Status = "E",
                        Text = "AI prediction returned an invalid response.",
                        Code = "502"
                    };
                }

                return new Message<OneeAiPredictData>
                {
                    Status = "S",
                    Text = "Prediction successful.",
                    Code = "200",
                    Result = parsed.Data
                };
            }
            catch (Exception ex)
            {
                return new Message<OneeAiPredictData>
                {
                    Status = "E",
                    Text = $"Unable to reach AI prediction service. {ex.Message}",
                    Code = "503"
                };
            }
        }

        private static List<string> DeserializeIds(string json)
        {
            try
            {
                return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        public async Task<List<JobModelForTable>> GetJobsByCustomerAsync(string customerId) =>
            await MapJobTableQuery(_context.Jobs.Where(j => j.FK_customer_ID == customerId));

        public async Task<List<JobModelForTable>> GetJobsByWorkerAsync(string workerId) =>
            await MapJobTableQuery(_context.Jobs.Where(j => j.FK_worker_ID == workerId));

        public async Task<List<JobModelForDetailView>> GetOfferingJobsForWorkerAsync(string workerId)
        {
            var ids = await _context.Jobs
                .Where(j => j.Status == JobStatuses.Offering && j.FK_worker_ID == workerId)
                .OrderByDescending(j => j.Id)
                .Select(j => j.Id)
                .ToListAsync();

            var list = new List<JobModelForDetailView>();
            foreach (var id in ids)
            {
                var d = await GetJobDetailAsync(id);
                if (d != null) list.Add(d);
            }
            return list;
        }

        private async Task<List<JobModelForTable>> MapJobTableQuery(IQueryable<Job> query)
        {
            return await query
                .OrderByDescending(j => j.Id)
                .Select(j => new JobModelForTable
                {
                    Id = j.Id,
                    Problem_Text = j.Problem_Text,
                    Category_Name = _context.Categories
                        .Where(c => c.Id == j.Category_id)
                        .Select(c => c.Category_Name)
                        .FirstOrDefault() ?? "",
                    Customer_Name = _context.Users
                        .Where(u => u.Id == j.FK_customer_ID)
                        .Select(u => u.Name)
                        .FirstOrDefault() ?? "",
                    Worker_Name = j.FK_worker_ID == null
                        ? null
                        : _context.Users
                            .Where(u => u.Id == j.FK_worker_ID)
                            .Select(u => u.Name)
                            .FirstOrDefault(),
                    Status = j.Status,
                    Amount = j.Amount,
                    Offer_Expires_At = j.Offer_Expires_At,
                    CreatedOn = j.CreatedOn
                })
                .ToListAsync();
        }

        private async Task<Message<JobModelForDetailView>> OkDetail(int jobId, string text) => new()
        {
            Status = "S",
            Text = text,
            Code = "200",
            Result = await GetJobDetailAsync(jobId)
        };

        private async Task<Message<JobModelForDetailView>> OkDetailNotifyUpdated(int jobId, string text)
        {
            var detail = await GetJobDetailAsync(jobId);
            if (detail != null)
                await _notifier.NotifyJobUpdatedAsync(detail);
            return new Message<JobModelForDetailView>
            {
                Status = "S",
                Text = text,
                Code = "200",
                Result = detail
            };
        }

        private async Task<Message<JobModelForDetailView>> OkDetailNotifyOfferOrUpdated(int jobId, string text)
        {
            var detail = await GetJobDetailAsync(jobId);
            if (detail != null)
            {
                if (detail.Status == JobStatuses.Offering)
                    await _notifier.NotifyJobOfferAsync(detail);
                else
                    await _notifier.NotifyJobUpdatedAsync(detail);
            }
            return new Message<JobModelForDetailView>
            {
                Status = "S",
                Text = text,
                Code = "200",
                Result = detail
            };
        }

        private static Message<JobModelForDetailView> ErrorDetail(string text, string code) => new()
        {
            Status = "E",
            Text = text,
            Code = code,
            Result = null
        };
    }
}
