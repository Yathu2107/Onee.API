using Microsoft.EntityFrameworkCore;
using OneeProject.Database.Common;
using OneeProject.Database.Context;
using OneeProject.Database.Model.API_Model;

namespace OneeProject.Services.Services
{
    public class ComplaintService(
        AppDbContext context,
        NotificationService notificationService)
    {
        private readonly AppDbContext _context = context;
        private readonly NotificationService _notificationService = notificationService;

        private static readonly HashSet<string> AllowedJobStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            JobStatuses.Accepted,
            JobStatuses.Ongoing,
            JobStatuses.Completed,
            JobStatuses.Cancelled
        };

        private static readonly HashSet<string> AllowedComplaintStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            ComplaintStatuses.Open,
            ComplaintStatuses.InReview,
            ComplaintStatuses.Resolved,
            ComplaintStatuses.Rejected
        };

        public async Task<Message<ComplaintModelForView>> CreateAsync(
            ComplaintModelForInsert model,
            string customerId)
        {
            if (string.IsNullOrWhiteSpace(model.Subject) || string.IsNullOrWhiteSpace(model.Description))
                return Err("Subject and Description are required.", "400");

            var job = await _context.Jobs.FirstOrDefaultAsync(j => j.Id == model.JobId);
            if (job == null)
                return Err("Job not found.", "404");

            if (job.FK_customer_ID != customerId)
                return Err("You can only file a complaint for your own job.", "403");

            if (string.IsNullOrEmpty(job.FK_worker_ID))
                return Err("Job has no assigned worker to complain about.", "400");

            if (!AllowedJobStatuses.Contains(job.Status))
                return Err("Complaints are only allowed after a worker has been assigned.", "400");

            var exists = await _context.Complaints.AnyAsync(c => c.FK_job_ID == job.Id);
            if (exists)
                return Err("A complaint already exists for this job.", "400");

            var entity = new Complaint
            {
                FK_job_ID = job.Id,
                FK_customer_ID = job.FK_customer_ID,
                FK_worker_ID = job.FK_worker_ID,
                Subject = model.Subject.Trim(),
                Description = model.Description.Trim(),
                Status = ComplaintStatuses.Open,
                CreatedBy = customerId,
                CreatedOn = CommonResources.LocalDatetime()
            };

            _context.Complaints.Add(entity);
            await _context.SaveChangesAsync();

            var view = await GetByIdAsync(entity.Id);
            return new Message<ComplaintModelForView>
            {
                Status = "S",
                Text = "Complaint submitted successfully.",
                Code = "200",
                Result = view
            };
        }

        public async Task<ComplaintModelForView?> GetByIdAsync(int id)
        {
            return await QueryViews()
                .Where(c => c.Id == id)
                .FirstOrDefaultAsync();
        }

        public async Task<ComplaintModelWithPagination> GetAllWithPagination(
            int page,
            int itemsPerPage,
            string? status,
            string? search)
        {
            var query = _context.Complaints.AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(c => c.Status == status);

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(c =>
                    c.Subject.Contains(search) ||
                    c.Description.Contains(search));
            }

            var count = await query.CountAsync();
            var ids = await query
                .OrderByDescending(c => c.CreatedOn)
                .Skip((page - 1) * itemsPerPage)
                .Take(itemsPerPage)
                .Select(c => c.Id)
                .ToListAsync();

            var items = await QueryViews()
                .Where(c => ids.Contains(c.Id))
                .ToListAsync();

            items = items.OrderByDescending(c => c.CreatedOn).ToList();

            return new ComplaintModelWithPagination
            {
                Count = count,
                Complaints = items
            };
        }

        public async Task<Message<ComplaintModelForView>> UpdateStatusAsync(
            int id,
            ComplaintModelForUpdateStatus model,
            string updatedBy)
        {
            var entity = await _context.Complaints.FirstOrDefaultAsync(c => c.Id == id);
            if (entity == null)
                return Err("Complaint not found.", "404");

            var status = model.Status?.Trim() ?? string.Empty;
            if (!AllowedComplaintStatuses.Contains(status))
                return Err("Invalid status. Use Open, InReview, Resolved, or Rejected.", "400");

            // Normalize casing
            status = AllowedComplaintStatuses.First(s => s.Equals(status, StringComparison.OrdinalIgnoreCase));

            entity.Status = status;
            entity.Admin_Response = string.IsNullOrWhiteSpace(model.Admin_Response)
                ? entity.Admin_Response
                : model.Admin_Response.Trim();
            entity.LastUpdatedBy = updatedBy;
            entity.LastUpdatedOn = CommonResources.LocalDatetime();
            await _context.SaveChangesAsync();

            var body = status switch
            {
                ComplaintStatuses.Resolved => "Your complaint was resolved.",
                ComplaintStatuses.Rejected => "Your complaint was rejected.",
                ComplaintStatuses.InReview => "Your complaint is under review.",
                _ => $"Your complaint status is now {status}."
            };

            await _notificationService.CreateInboxAsync(
                entity.FK_customer_ID,
                "Complaint update",
                string.IsNullOrWhiteSpace(entity.Admin_Response)
                    ? body
                    : $"{body} {entity.Admin_Response}",
                NotificationTypes.ComplaintUpdate,
                entity.FK_job_ID);

            var view = await GetByIdAsync(id);
            return new Message<ComplaintModelForView>
            {
                Status = "S",
                Text = "Complaint updated.",
                Code = "200",
                Result = view
            };
        }

        private IQueryable<ComplaintModelForView> QueryViews()
        {
            return _context.Complaints.Select(c => new ComplaintModelForView
            {
                Id = c.Id,
                FK_job_ID = c.FK_job_ID,
                Problem_Text = _context.Jobs
                    .Where(j => j.Id == c.FK_job_ID)
                    .Select(j => j.Problem_Text)
                    .FirstOrDefault() ?? "",
                FK_customer_ID = c.FK_customer_ID,
                Customer_Name = _context.Users
                    .Where(u => u.Id == c.FK_customer_ID)
                    .Select(u => u.Name)
                    .FirstOrDefault() ?? "",
                FK_worker_ID = c.FK_worker_ID,
                Worker_Name = _context.Users
                    .Where(u => u.Id == c.FK_worker_ID)
                    .Select(u => u.Name)
                    .FirstOrDefault() ?? "",
                Subject = c.Subject,
                Description = c.Description,
                Status = c.Status,
                Admin_Response = c.Admin_Response,
                CreatedBy = c.CreatedBy,
                CreatedOn = c.CreatedOn,
                LastUpdatedBy = c.LastUpdatedBy,
                LastUpdatedOn = c.LastUpdatedOn
            });
        }

        private static Message<ComplaintModelForView> Err(string text, string code) => new()
        {
            Status = "E",
            Text = text,
            Code = code,
            Result = null
        };
    }
}
