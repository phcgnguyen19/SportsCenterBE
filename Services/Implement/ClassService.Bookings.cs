using Microsoft.EntityFrameworkCore;
using SportsCenterAPI.Helpers;
using SportsCenterAPI.Models;
using SportsCenterAPI.Models.DTOs.Classes;

namespace SportsCenterAPI.Services.Implement;

public partial class ClassService
{
    public async Task<RegistrationDTO> BookAsync(int actorId, int sessionId, int? memberId = null, CancellationToken ct = default)
    {
        var id = await Transaction(async () =>
        {
            var member = await ResolveMember(actorId, memberId, ct);
            if (!member.User.IsActive || member.User.Role != "Member") throw new BusinessException(409, "Hội viên không hoạt động.");
            var session = await Session(sessionId, ct);
            if (session.Status != "Scheduled" || session.StartsAt <= Now || !session.Class.IsActive || !session.Class.Sport.IsActive ||
                !session.Coach.User.IsActive || session.Coach.User.Role != "Coach" || !session.CancellationPolicy.IsActive)
                throw new BusinessException(409, "Buổi học không còn nhận đăng ký.");
            if (await db.ClassRegistrations.AnyAsync(r => r.SessionId == sessionId && r.MemberId == member.Id && r.Status != "Cancelled", ct))
                throw new BusinessException(409, "Hội viên đã đăng ký buổi này.");
            if (await db.ClassRegistrations.CountAsync(r => r.SessionId == sessionId && r.Status != "Cancelled", ct) >= session.Capacity)
                throw new BusinessException(409, "Buổi học đã hết chỗ.");
            // The user explicitly deferred membership eligibility and member schedule-conflict rules.
            // Serializable isolation + unique index protect capacity and duplicate bookings.
            var registration = new ClassRegistration
            {
                MemberId = member.Id, ClassId = session.ClassId, SessionId = sessionId, RegistrationDate = Now,
                Status = "Registered", CreatedByUserId = actorId,
                CancellationDeadline = session.StartsAt.AddHours(-session.CancellationPolicy.MinimumHoursBeforeStart)
            };
            db.ClassRegistrations.Add(registration);
            await db.SaveChangesAsync(ct);
            await Notify(session, member.UserId, "Đặt lịch thành công",
                $"Hội viên {member.Id} đã đặt buổi {sessionId} - {session.Class.ClassName}, bắt đầu {session.StartsAt:O} (UTC).", false, ct);
            Audit(actorId, nameof(ClassRegistration), registration.Id, $"Booked session {sessionId} for member {member.Id}");
            return registration.Id;
        }, ct);
        return await RegistrationResult(id, ct);
    }

    public async Task<PageResult<RegistrationDTO>> GetRegistrationsAsync(int actorId, int? memberId, int page, int pageSize, CancellationToken ct = default)
    {
        Pagination(page, pageSize);
        var member = await ResolveMember(actorId, memberId, ct);
        var query = db.ClassRegistrations.AsNoTracking().Where(r => r.MemberId == member.Id);
        return new(await query.OrderByDescending(r => r.RegistrationDate).ThenByDescending(r => r.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).Select(RegistrationProjection).ToListAsync(ct), await query.CountAsync(ct), page, pageSize);
    }

    public async Task<RegistrationDTO> CancelRegistrationAsync(int actorId, int id, CancelRequest request, CancellationToken ct = default)
    {
        Validate(request);
        await Transaction(async () =>
        {
            var actor = await Actor(actorId, ct);
            var query = db.ClassRegistrations.Include(r => r.Member).Where(r => r.Id == id);
            if (!IsDesk(actor))
            {
                if (actor.Role != "Member") throw new BusinessException(403, "Chỉ hội viên hoặc lễ tân/Manager được hủy đăng ký.");
                query = query.Where(r => r.Member.UserId == actorId);
            }
            var registration = await query.SingleOrDefaultAsync(ct) ?? throw new BusinessException(404, "Không tìm thấy đăng ký.");
            if (registration.Status == "Cancelled") return true;
            if (!registration.SessionId.HasValue) throw new BusinessException(409, "Đăng ký cũ chưa được gắn với buổi học cụ thể.");
            var session = await Session(registration.SessionId.Value, ct);
            if (registration.Status != "Registered" || session.Status != "Scheduled" || registration.CancellationDeadline == null || Now > registration.CancellationDeadline)
                throw new BusinessException(409, "Đã quá hạn hủy hoặc đăng ký không còn có thể hủy.");
            if (await db.Attendances.AnyAsync(a => a.SessionId == session.Id && a.MemberId == registration.MemberId, ct))
                throw new BusinessException(409, "Không hủy đăng ký đã điểm danh.");
            registration.Status = "Cancelled"; registration.CancelledAt = Now; registration.CancelledByUserId = actorId;
            registration.CancellationReason = request.Reason.Trim();
            await Notify(session, registration.Member.UserId, "Đã hủy đăng ký lớp",
                $"Hội viên {registration.MemberId} đã hủy buổi {session.Id} - {session.Class.ClassName}. Chỗ đã được giải phóng.", true, ct);
            Audit(actorId, nameof(ClassRegistration), id, $"Cancelled registration: {request.Reason.Trim()}");
            return true;
        }, ct);
        return await RegistrationResult(id, ct);
    }

    public async Task<List<RosterDTO>> GetRosterAsync(int actorId, int sessionId, CancellationToken ct = default)
    {
        var session = await Session(sessionId, ct);
        await EnsureSessionStaff(actorId, session, ct);
        return await db.ClassRegistrations.AsNoTracking().Where(r => r.SessionId == sessionId && r.Status != "Cancelled")
            .OrderBy(r => r.Member.User.FullName).ThenBy(r => r.Id)
            .Select(r => new RosterDTO(r.Id, r.MemberId, r.Member.User.FullName, r.Status,
                db.Attendances.Where(a => a.SessionId == sessionId && a.MemberId == r.MemberId).Select(a => a.Status).FirstOrDefault(),
                db.Attendances.Where(a => a.SessionId == sessionId && a.MemberId == r.MemberId).Select(a => (DateTime?)a.CheckInTime).FirstOrDefault()))
            .ToListAsync(ct);
    }

    public async Task<AttendanceDTO> CheckInAsync(int actorId, int registrationId, CancellationToken ct = default)
    {
        return await Transaction(async () =>
        {
            var actor = await Actor(actorId, ct);
            var query = db.ClassRegistrations.Include(r => r.Member).Where(r => r.Id == registrationId);
            if (actor.Role == "Member") query = query.Where(r => r.Member.UserId == actorId);
            var registration = await query.SingleOrDefaultAsync(ct) ?? throw new BusinessException(404, "Không tìm thấy đăng ký.");
            if (!registration.SessionId.HasValue) throw new BusinessException(409, "Đăng ký cũ chưa được gắn buổi học.");
            var session = await Session(registration.SessionId.Value, ct);
            if (actor.Role != "Member") await EnsureSessionStaff(actorId, session, ct);
            var existing = await db.Attendances.SingleOrDefaultAsync(a => a.SessionId == session.Id && a.MemberId == registration.MemberId, ct);
            if (existing != null) return new AttendanceDTO(existing.Id, existing.MemberId, existing.SessionId, existing.CheckInTime, existing.Status);
            if (registration.Status != "Registered" || session.Status != "Scheduled" || Now < session.StartsAt.AddMinutes(-30) || Now >= session.EndsAt)
                throw new BusinessException(409, "Chỉ điểm danh từ 30 phút trước giờ học đến trước khi buổi học kết thúc.");
            var attendance = new Attendance { MemberId = registration.MemberId, ClassId = session.ClassId, SessionId = session.Id, Status = "Present", CheckInTime = Now };
            db.Attendances.Add(attendance);
            await db.SaveChangesAsync(ct);
            Audit(actorId, nameof(Attendance), attendance.Id, $"Checked in registration {registrationId}");
            return new AttendanceDTO(attendance.Id, attendance.MemberId, attendance.SessionId, attendance.CheckInTime, attendance.Status);
        }, ct);
    }

    public async Task<ReviewDTO> ReviewAsync(int actorId, int registrationId, ReviewRequest request, CancellationToken ct = default)
    {
        Validate(request);
        return await Transaction(async () =>
        {
            var actor = await Actor(actorId, ct);
            if (actor.Role != "Member") throw new BusinessException(403, "Chỉ hội viên được đánh giá.");
            var registration = await db.ClassRegistrations.Include(r => r.Session)
                .SingleOrDefaultAsync(r => r.Id == registrationId && r.Member.UserId == actorId, ct)
                ?? throw new BusinessException(404, "Không tìm thấy đăng ký của bạn.");
            if (registration.Status != "Completed" || registration.Session?.Status != "Completed" || registration.Session.EndsAt > Now ||
                !await db.Attendances.AnyAsync(a => a.SessionId == registration.SessionId && a.MemberId == registration.MemberId && a.Status == "Present", ct))
                throw new BusinessException(409, "Chỉ đánh giá buổi đã hoàn thành và có điểm danh tham gia.");
            if (await db.ClassReviews.AnyAsync(r => r.RegistrationId == registrationId, ct))
                throw new BusinessException(409, "Bạn đã đánh giá buổi học này.");
            var review = new ClassReview { RegistrationId = registrationId, Rating = request.Rating, Comment = request.Comment?.Trim(), CreatedAt = Now };
            db.ClassReviews.Add(review);
            await db.SaveChangesAsync(ct);
            Audit(actorId, nameof(ClassReview), review.Id, "Reviewed completed session");
            return new ReviewDTO(review.Id, registrationId, review.Rating, review.Comment, review.CreatedAt);
        }, ct);
    }

    public async Task<PageResult<ReviewDTO>> GetReviewsAsync(int sessionId, int page, int pageSize, CancellationToken ct = default)
    {
        Pagination(page, pageSize);
        if (!await db.ClassSessions.AnyAsync(s => s.Id == sessionId, ct)) throw new BusinessException(404, "Không tìm thấy buổi học.");
        var query = db.ClassReviews.AsNoTracking().Where(r => r.Registration.SessionId == sessionId);
        return new(await query.OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(r => new ReviewDTO(r.Id, r.RegistrationId, r.Rating, r.Comment, r.CreatedAt)).ToListAsync(ct), await query.CountAsync(ct), page, pageSize);
    }
}
