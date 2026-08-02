using Microsoft.EntityFrameworkCore;
using OneeProject.Database.Common;
using OneeProject.Database.Context;
using OneeProject.Database.Model.API_Model;

namespace OneeProject.Services.Services
{
    public class JobRatingService(AppDbContext context)
    {
        private readonly AppDbContext _context = context;

        public async Task<Message<JobRatingModelForView>> AddRatingAsync(
            int jobId,
            JobRatingModelForInsert model,
            string createdBy)
        {
            if (model.Rating < 1 || model.Rating > 5)
            {
                return Error("Rating must be between 1 and 5.", "400");
            }

            var job = await _context.Jobs.FirstOrDefaultAsync(j => j.Id == jobId);
            if (job == null)
                return Error("Job not found.", "404");

            if (job.Status != JobStatuses.Completed)
                return Error("Rating can only be added for completed jobs.", "400");

            if (string.IsNullOrEmpty(job.FK_worker_ID))
                return Error("Job has no assigned worker to rate.", "400");

            var alreadyExists = await _context.JobRatings.AnyAsync(r => r.FK_job_ID == jobId);
            if (alreadyExists)
                return Error("Rating already submitted for this job.", "400");

            var entity = new JobRating
            {
                FK_job_ID = job.Id,
                FK_worker_ID = job.FK_worker_ID,
                FK_customer_ID = job.FK_customer_ID,
                Rating = model.Rating,
                Feedback = string.IsNullOrWhiteSpace(model.Feedback) ? string.Empty : model.Feedback.Trim(),
                CreatedBy = createdBy,
                CreatedOn = CommonResources.LocalDatetime()
            };

            _context.JobRatings.Add(entity);
            await _context.SaveChangesAsync();

            var view = await GetByJobIdAsync(jobId);
            return new Message<JobRatingModelForView>
            {
                Status = "S",
                Text = "Rating submitted successfully.",
                Code = "200",
                Result = view
            };
        }

        public async Task<JobRatingModelForView?> GetByJobIdAsync(int jobId)
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

        private static Message<JobRatingModelForView> Error(string text, string code) => new()
        {
            Status = "E",
            Text = text,
            Code = code,
            Result = null
        };
    }
}
