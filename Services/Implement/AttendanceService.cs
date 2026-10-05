using Microsoft.EntityFrameworkCore;
using SportsCenterAPI.DTOs.Attendances;
using SportsCenterAPI.Models;
using SportsCenterAPI.Services.Interface;
using SportsCenterAPI.Data;

namespace SportsCenterAPI.Services.Implement
{
    public class AttendanceService(AppDbContext context) : IAttendanceService
    {
        public async Task<AttendanceResponseDTO> MarkAttendanceAsync(MarkAttendanceDTO request)
        {
            var session = await context.ClassSessions
                .Include(s => s.Class)
                .FirstOrDefaultAsync(s => s.Id == request.SessionId)
                ?? throw new ArgumentException($"Class session with ID {request.SessionId} not found.");

            var isRegistered = await context.ClassRegistrations
                .AnyAsync(r => r.MemberId == request.MemberId && r.ClassId == session.ClassId && r.Status != "Cancelled");

            if (!isRegistered)
                throw new ArgumentException("Member is not registerd for this class");

            var existingAttendance = await context.Attendances
                .FirstOrDefaultAsync(a => a.MemberId == request.MemberId && a.SessionId == request.SessionId);

            if (existingAttendance != null)
            {
                existingAttendance.Status = request.Status;
                existingAttendance.CheckInTime = DateTime.UtcNow;
                await context.SaveChangesAsync();
                return MapToDTO(existingAttendance);
            }

            var attendance = new Attendance
            {
                MemberId = request.MemberId,
                ClassId = session.ClassId,
                SessionId = request.SessionId,
                Status = request.Status,
                CheckInTime = DateTime.UtcNow
            };

            context.Attendances.Add(attendance);
            await context.SaveChangesAsync();

            return MapToDTO(attendance);
        }

        public async Task<IEnumerable<AttendanceResponseDTO>> GetSessionAttendancesAsync(int sessionId)
        {
            var attendances = await context.Attendances
                .Include(a => a.Member)
                .ThenInclude(m => m.User)
                .Where(a => a.SessionId == sessionId)
                .ToListAsync();
            return attendances.Select(MapToDTO);
        }

        private AttendanceResponseDTO MapToDTO(Attendance attendance)
        {
            return new AttendanceResponseDTO
            {
                Id = attendance.Id,
                MemberId = attendance.MemberId,
                MemberName = attendance.Member.User.FullName,
                CheckInTime = attendance.CheckInTime,
                Status = attendance.Status,
                SessionId = attendance.SessionId
            };
        }
    }
}
