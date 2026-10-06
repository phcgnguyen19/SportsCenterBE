namespace SportsCenterAPI.DTOs.Reports
{
    public class DashboardOverviewDTO
    {
        public int TotalActiveMembers { get; set; }
        public int TotalActiveClasses { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal CurrentMonthRevenue { get; set; }
    }

    public class RevenueByMonthDTO
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal Revenue { get; set; }
    }

    public class ClassStatusReportDTO
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public string CoachName { get; set; } = string.Empty;
        public int MaxCapacity { get; set; }
        public int TotalRegistration { get; set; }
        public decimal FillRatePercentage { get; set; }
    }
}
