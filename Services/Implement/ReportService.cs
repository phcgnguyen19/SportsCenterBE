using Microsoft.EntityFrameworkCore;
using SportsCenterAPI.Data;
using SportsCenterAPI.Services.Interface;
using SportsCenterAPI.DTOs.Reports;

namespace SportsCenterAPI.Services.Implement
{
    public class ReportService(AppDbContext context) : IReportService
    {
        public async Task<DashboardOverviewDTO> GetOverviewAsync()
        {
            var now = DateTime.UtcNow;

            var totalMembers = await context.Members
                .Include(m => m.User)
                .CountAsync(m => m.User.IsActive);

            var totalClasses = await context.SportClasses
                .CountAsync(c => c.IsActive && c.EndDate >= now);

            var allCompletedPayments = await context.Payments
                .Where(p => p.Status == "Completed")
                .ToListAsync();

            // Calculate the total revenue from completed payments
            var totalRevenue = allCompletedPayments.Sum(p => p.AmountReceived > 0 ? p.AmountReceived : p.Amount);

            var currentMonthRevenue = allCompletedPayments
                .Where(p => p.PaymentDate.Month == now.Month && p.PaymentDate.Year == now.Year)
                .Sum(p => p.AmountReceived > 0 ? p.AmountReceived : p.Amount);

            return new DashboardOverviewDTO
            {
                TotalActiveMembers = totalMembers,
                TotalActiveClasses = totalClasses,
                TotalRevenue = totalRevenue,
                CurrentMonthRevenue = currentMonthRevenue
            };
        }

        public async Task<IEnumerable<RevenueByMonthDTO>> GetRevenueOverTimeAsync(int year)
        {
            var payments = await context.Payments
                .Where(p => p.Status == "Completed" && p.PaymentDate.Year == year)
                .ToListAsync();

            var monthlyRevenue = payments
                .GroupBy(p => p.PaymentDate.Month)
                .Select(g => new RevenueByMonthDTO
                {
                    Month = g.Key,
                    Year = year,
                    Revenue = g.Sum(p => p.AmountReceived > 0 ? p.AmountReceived : p.Amount)
                })
                .OrderBy(r => r.Month)
                .ToList();

            //Bổ sung cá tháng không có doanh thu cho đủ 12 tháng
            var fullYearReport = new List<RevenueByMonthDTO>();
            for (int i = 1; i <= 12; i++)
            {
                var monthData = monthlyRevenue.FirstOrDefault(r => r.Month == i);
                fullYearReport.Add(monthData ?? new RevenueByMonthDTO { Month = i, Year = year, Revenue = 0 });
            }

            return fullYearReport;
        }

        public async Task<IEnumerable<ClassStatusReportDTO>> GetClassRegistrationStatusAsync()
        {
            var classes = await context.SportClasses
                .Include(c => c.Coach).ThenInclude(co => co.User)
                .Include(c => c.Registrations)
                .ToListAsync();
            return classes.Select(c => new ClassStatusReportDTO
            {
                ClassId = c.Id,
                ClassName = c.ClassName,
                CoachName = c.Coach != null ? c.Coach.User.FullName : "N/A",
                MaxCapacity = c.MaxCapacity,
                TotalRegistration = c.Registrations.Count(r => r.Status != "Cancelled"),
                FillRatePercentage = c.MaxCapacity > 0
                ? Math.Round((decimal)c.Registrations.Count(r => r.Status != "Cancelled") / c.MaxCapacity * 100, 2) : 0
            }).OrderByDescending(c => c.FillRatePercentage).ToList();
        }
    } 
}
