namespace OneeProject.Database.Model.API_Model
{
    public class DashboardOverviewModel
    {
        public DashboardSummaryCards Summary { get; set; } = new();
        public List<DashboardChartPoint> JobsByStatus { get; set; } = new();
        public List<DashboardChartPoint> UsersByType { get; set; } = new();
        public List<DashboardChartPoint> JobsByCategory { get; set; } = new();
        public List<DashboardChartPoint> ComplaintsByStatus { get; set; } = new();
        public List<DashboardChartPoint> RatingsDistribution { get; set; } = new();
        public List<DashboardTrendPoint> JobsTrend { get; set; } = new();
    }

    public class DashboardSummaryCards
    {
        public int TotalUsers { get; set; }
        public int TotalWorkers { get; set; }
        public int WorkersOnline { get; set; }
        public int TotalJobs { get; set; }
        public int JobsOffering { get; set; }
        public int JobsAccepted { get; set; }
        public int JobsOngoing { get; set; }
        public int JobsCompleted { get; set; }
        public int JobsCancelled { get; set; }
        public int JobsFailed { get; set; }
        public int OpenComplaints { get; set; }
        public double AvgRating { get; set; }
        public int TotalRatings { get; set; }
        public int NotificationsSent { get; set; }
    }

    public class DashboardChartPoint
    {
        public string Label { get; set; } = string.Empty;
        public int Value { get; set; }
        public int? CategoryId { get; set; }
    }

    public class DashboardTrendPoint
    {
        public string Date { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public int Total { get; set; }
        public int Completed { get; set; }
        public int Cancelled { get; set; }
    }

    public class DashboardJobsTrendModel
    {
        public int Days { get; set; }
        public List<DashboardTrendPoint> Points { get; set; } = new();
    }
}
