using SportsCenterAPI.DTOs.Reports;

namespace SportsCenterAPI.Services.Interface;

public interface IReportService
{
    Task<DashboardOverviewDTO> GetOverviewAsync();
    Task<IEnumerable<RevenueByMonthDTO>> GetRevenueOverTimeAsync(int year);
    Task<IEnumerable<ClassStatusReportDTO>> GetClassRegistrationStatusAsync();
}

