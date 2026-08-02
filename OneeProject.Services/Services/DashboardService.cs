using Microsoft.EntityFrameworkCore;
using OneeProject.Database.Common;
using OneeProject.Database.Context;
using OneeProject.Database.Model.API_Model;

namespace OneeProject.Services.Services
{
    public class DashboardService(AppDbContext context)
    {
        private readonly AppDbContext _context = context;

        public async Task<DashboardOverviewModel> GetOverviewAsync()
        {
            var summary = await BuildSummaryAsync();
            var jobsByStatus = await BuildJobsByStatusAsync();
            var usersByType = await BuildUsersByTypeAsync();
            var jobsByCategory = await BuildJobsByCategoryAsync();
            var complaintsByStatus = await BuildComplaintsByStatusAsync();
            var ratingsDistribution = await BuildRatingsDistributionAsync();
            var jobsTrend = await BuildJobsTrendAsync(7);

            return new DashboardOverviewModel
            {
                Summary = summary,
                JobsByStatus = jobsByStatus,
                UsersByType = usersByType,
                JobsByCategory = jobsByCategory,
                ComplaintsByStatus = complaintsByStatus,
                RatingsDistribution = ratingsDistribution,
                JobsTrend = jobsTrend
            };
        }

        public async Task<DashboardJobsTrendModel> GetJobsTrendAsync(int days)
        {
            days = Math.Clamp(days, 7, 90);
            return new DashboardJobsTrendModel
            {
                Days = days,
                Points = await BuildJobsTrendAsync(days)
            };
        }

        private async Task<DashboardSummaryCards> BuildSummaryAsync()
        {
            var totalUsers = await _context.Users
                .CountAsync(u => u.UserType == "User" && u.IsActive);
            var totalWorkers = await _context.Users
                .CountAsync(u => u.UserType == "Worker" && u.IsActive);
            var workersOnline = await _context.Users
                .CountAsync(u => u.UserType == "Worker" && u.IsActive && u.IsOnline);

            var jobStatusCounts = await _context.Jobs
                .GroupBy(j => j.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            int StatusCount(string status) =>
                jobStatusCounts.FirstOrDefault(x => x.Status == status)?.Count ?? 0;

            var totalJobs = jobStatusCounts.Sum(x => x.Count);

            var openComplaints = await _context.Complaints
                .CountAsync(c =>
                    c.Status == ComplaintStatuses.Open ||
                    c.Status == ComplaintStatuses.InReview);

            var totalRatings = await _context.JobRatings.CountAsync();
            var avgRating = totalRatings == 0
                ? 0
                : Math.Round(await _context.JobRatings.AverageAsync(r => (double)r.Rating), 1);

            var notificationsSent = await _context.AdminNotifications
                .CountAsync(n => n.Status == AdminNotificationStatuses.Sent);

            return new DashboardSummaryCards
            {
                TotalUsers = totalUsers,
                TotalWorkers = totalWorkers,
                WorkersOnline = workersOnline,
                TotalJobs = totalJobs,
                JobsOffering = StatusCount(JobStatuses.Offering),
                JobsAccepted = StatusCount(JobStatuses.Accepted),
                JobsOngoing = StatusCount(JobStatuses.Ongoing),
                JobsCompleted = StatusCount(JobStatuses.Completed),
                JobsCancelled = StatusCount(JobStatuses.Cancelled),
                JobsFailed = StatusCount(JobStatuses.Failed),
                OpenComplaints = openComplaints,
                AvgRating = avgRating,
                TotalRatings = totalRatings,
                NotificationsSent = notificationsSent
            };
        }

        private async Task<List<DashboardChartPoint>> BuildJobsByStatusAsync()
        {
            var ordered = new[]
            {
                JobStatuses.Offering,
                JobStatuses.Accepted,
                JobStatuses.Ongoing,
                JobStatuses.Completed,
                JobStatuses.Cancelled,
                JobStatuses.Failed
            };

            var counts = await _context.Jobs
                .GroupBy(j => j.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            return ordered
                .Select(status => new DashboardChartPoint
                {
                    Label = status,
                    Value = counts.FirstOrDefault(c => c.Status == status)?.Count ?? 0
                })
                .ToList();
        }

        private async Task<List<DashboardChartPoint>> BuildUsersByTypeAsync()
        {
            var users = await _context.Users
                .CountAsync(u => u.UserType == "User" && u.IsActive);
            var workers = await _context.Users
                .CountAsync(u => u.UserType == "Worker" && u.IsActive);

            return new List<DashboardChartPoint>
            {
                new() { Label = "User", Value = users },
                new() { Label = "Worker", Value = workers }
            };
        }

        private async Task<List<DashboardChartPoint>> BuildJobsByCategoryAsync()
        {
            var categoryNames = await _context.Categories
                .Select(c => new { c.Id, c.Category_Name })
                .ToDictionaryAsync(c => c.Id, c => c.Category_Name);

            var counts = await _context.Jobs
                .GroupBy(j => j.Category_id)
                .Select(g => new { CategoryId = g.Key, Count = g.Count() })
                .ToListAsync();

            return counts
                .Select(c => new DashboardChartPoint
                {
                    CategoryId = c.CategoryId,
                    Label = categoryNames.TryGetValue(c.CategoryId, out var name) ? name : "Unknown",
                    Value = c.Count
                })
                .OrderByDescending(x => x.Value)
                .ToList();
        }

        private async Task<List<DashboardChartPoint>> BuildComplaintsByStatusAsync()
        {
            var ordered = new[]
            {
                ComplaintStatuses.Open,
                ComplaintStatuses.InReview,
                ComplaintStatuses.Resolved,
                ComplaintStatuses.Rejected
            };

            var counts = await _context.Complaints
                .GroupBy(c => c.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            return ordered
                .Select(status => new DashboardChartPoint
                {
                    Label = status,
                    Value = counts.FirstOrDefault(c => c.Status == status)?.Count ?? 0
                })
                .ToList();
        }

        private async Task<List<DashboardChartPoint>> BuildRatingsDistributionAsync()
        {
            var counts = await _context.JobRatings
                .GroupBy(r => r.Rating)
                .Select(g => new { Rating = g.Key, Count = g.Count() })
                .ToListAsync();

            return Enumerable.Range(1, 5)
                .Select(rating => new DashboardChartPoint
                {
                    Label = rating.ToString(),
                    Value = counts.FirstOrDefault(c => c.Rating == rating)?.Count ?? 0
                })
                .ToList();
        }

        private async Task<List<DashboardTrendPoint>> BuildJobsTrendAsync(int days)
        {
            var today = CommonResources.LocalDatetime().Date;
            var start = today.AddDays(-(days - 1));

            var jobs = await _context.Jobs
                .Where(j => j.CreatedOn >= start)
                .Select(j => new { j.CreatedOn, j.Status })
                .ToListAsync();

            var byDate = jobs
                .GroupBy(j => j.CreatedOn.Date)
                .ToDictionary(
                    g => g.Key,
                    g => new
                    {
                        Total = g.Count(),
                        Completed = g.Count(x => x.Status == JobStatuses.Completed),
                        Cancelled = g.Count(x => x.Status == JobStatuses.Cancelled)
                    });

            var points = new List<DashboardTrendPoint>();
            for (var d = start; d <= today; d = d.AddDays(1))
            {
                byDate.TryGetValue(d, out var stats);
                points.Add(new DashboardTrendPoint
                {
                    Date = d.ToString("yyyy-MM-dd"),
                    Label = d.ToString("MMM d"),
                    Total = stats?.Total ?? 0,
                    Completed = stats?.Completed ?? 0,
                    Cancelled = stats?.Cancelled ?? 0
                });
            }

            return points;
        }
    }
}
