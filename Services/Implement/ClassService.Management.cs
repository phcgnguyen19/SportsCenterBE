using Microsoft.EntityFrameworkCore;
using SportsCenterAPI.DTOs.Classes;
using SportsCenterAPI.Helpers;
using SportsCenterAPI.Models;

namespace SportsCenterAPI.Services.Implement;

public partial class ClassService
{
    // id = null: tạo lớp mới; có id: cập nhật lớp đã tồn tại.
    public async Task<ClassDTO> SaveClassAsync(
        int actorId,
        int? id,
        ClassRequest request,
        CancellationToken ct = default)
    {
        Validate(request);
        if (string.IsNullOrWhiteSpace(request.ClassName) ||
            request.StartDate >= request.EndDate ||
            request.EndDate.UtcDateTime <= Now ||
            request.StartDate.Year < 2000 ||
            decimal.Round(request.Price, 2) != request.Price)
        {
            throw new BusinessException(400, "Tên lớp, thời gian hoặc giá lớp không hợp lệ.");
        }

        var classId = await Transaction(async () =>
        {
            await Manager(actorId, ct);
            if (!await db.Sports.AnyAsync(s => s.Id == request.SportId && s.IsActive, ct))
            {
                throw new BusinessException(404, "Không tìm thấy bộ môn đang hoạt động.");
            }

            await ActiveCoach(request.CoachId, ct);

            var sportClass = id.HasValue
                ? await db.SportClasses.SingleOrDefaultAsync(c => c.Id == id, ct)
                    ?? throw new BusinessException(404, "Không tìm thấy lớp học.")
                : new SportClass();

            // Khi sửa lớp, phải bảo vệ lịch và đăng ký đã có.
            if (id.HasValue)
            {
                if (!request.IsActive && await db.ClassSessions.AnyAsync(s => s.ClassId == id && s.Status == "Scheduled", ct))
                {
                    throw new BusinessException(409, "Hãy hủy hoặc hoàn thành các buổi học trước khi đóng lớp.");
                }

                if (await db.ClassSessions.AnyAsync(s =>
                    s.ClassId == id &&
                    (s.StartsAt < request.StartDate.UtcDateTime ||
                     s.EndsAt > request.EndDate.UtcDateTime), ct))
                {
                    throw new BusinessException(409, "Khoảng thời gian lớp phải bao gồm mọi buổi học đã tạo.");
                }

                if (sportClass.SportId != request.SportId &&
                    await db.ClassRegistrations.AnyAsync(r =>
                        r.ClassId == id && r.Status != "Cancelled", ct))
                {
                    throw new BusinessException(409, "Không đổi bộ môn khi đã có học viên đăng ký.");
                }

                if (await db.ClassSessions.AnyAsync(s =>
                    s.ClassId == id &&
                    s.Status == "Scheduled" &&
                    s.EndsAt > Now &&
                    s.Capacity > request.MaxCapacity, ct))
                {
                    throw new BusinessException(409, "Giới hạn của lớp không thể thấp hơn sức chứa các buổi học chưa kết thúc.");
                }
            }

            sportClass.ClassName = request.ClassName.Trim();
            sportClass.SportId = request.SportId;
            sportClass.CoachId = request.CoachId;
            sportClass.Price = request.Price;
            sportClass.MaxCapacity = request.MaxCapacity;
            sportClass.Schedule = request.Schedule?.Trim();
            sportClass.StartDate = request.StartDate.UtcDateTime;
            sportClass.EndDate = request.EndDate.UtcDateTime;
            sportClass.IsActive = request.IsActive;

            if (!id.HasValue)
            {
                db.SportClasses.Add(sportClass);
            }

            await db.SaveChangesAsync(ct);
            Audit(actorId, nameof(SportClass), sportClass.Id, id.HasValue ? "Updated class" : "Created class");
            return sportClass.Id;
        }, ct);

        return await GetClassAsync(classId, ct);
    }

    public async Task DeactivateClassAsync(
        int actorId,
        int id,
        CancellationToken ct = default)
    {
        await Transaction(async () =>
        {
            await Manager(actorId, ct);
            var sportClass = await db.SportClasses.SingleOrDefaultAsync(c => c.Id == id, ct)
                ?? throw new BusinessException(404, "Không tìm thấy lớp học.");

            if (await db.ClassSessions.AnyAsync(s => s.ClassId == id && s.Status == "Scheduled", ct))
            {
                throw new BusinessException(409, "Hãy hủy hoặc hoàn thành các buổi học trước khi đóng lớp.");
            }

            sportClass.IsActive = false;
            Audit(actorId, nameof(SportClass), id, "Deactivated class");
            return true;
        }, ct);
    }

    private async Task ActiveCoach(int coachId, CancellationToken ct)
    {
        if (!await db.Coaches.AnyAsync(c => c.Id == coachId && c.User.IsActive && c.User.Role == "Coach", ct))
        {
            throw new BusinessException(404, "Không tìm thấy HLV đang hoạt động.");
        }
    }

    // Cùng một luồng kiểm tra cho cả tạo buổi học và sửa buổi học.
    public async Task<SessionDTO> SaveSessionAsync(
        int actorId,
        int classId,
        int? id,
        SessionRequest request,
        CancellationToken ct = default)
    {
        // 1. Kiểm tra dữ liệu và thời gian trước khi truy cập database.
        Validate(request);
        if (request.StartsAt >= request.EndsAt || request.StartsAt.UtcDateTime <= Now)
        {
            throw new BusinessException(400, "Buổi học phải bắt đầu trong tương lai và kết thúc sau giờ bắt đầu.");
        }

        var sessionId = await Transaction(async () =>
        {
            // 2. Kiểm tra quyền Manager, lớp, bộ môn, HLV và chính sách hủy.
            await Manager(actorId, ct);

            var sportClass = await db.SportClasses
                .Include(c => c.Sport)
                .SingleOrDefaultAsync(c => c.Id == classId, ct)
                ?? throw new BusinessException(404, "Không tìm thấy lớp học.");

            if (!sportClass.IsActive || !sportClass.Sport.IsActive)
            {
                throw new BusinessException(409, "Lớp hoặc bộ môn đã ngừng hoạt động.");
            }

            if (request.StartsAt.UtcDateTime < sportClass.StartDate || request.EndsAt.UtcDateTime > sportClass.EndDate)
            {
                throw new BusinessException(400, "Buổi học phải nằm trong khoảng thời gian của lớp.");
            }

            await ActiveCoach(request.CoachId, ct);

            var policy = await db.CancellationPolicies.SingleOrDefaultAsync(p =>
                p.Id == request.CancellationPolicyId && p.IsActive, ct)
                ?? throw new BusinessException(404, "Không tìm thấy chính sách hủy đang hoạt động.");

            // 3. Có id thì lấy buổi cũ; không có id thì tạo đối tượng mới.
            var session = id.HasValue
                ? await db.ClassSessions.SingleOrDefaultAsync(s =>
                    s.Id == id && s.ClassId == classId, ct)
                    ?? throw new BusinessException(404, "Không tìm thấy buổi học của lớp.")
                : new ClassSession { ClassId = classId };

            if (session.Status != "Scheduled" || (id.HasValue && session.StartsAt <= Now))
            {
                throw new BusinessException(409, "Không sửa buổi học đã bắt đầu, hủy hoặc hoàn thành.");
            }

            var enrolledCount = id.HasValue
                ? await db.ClassRegistrations.CountAsync(r =>
                    r.SessionId == id && r.Status != "Cancelled", ct)
                : 0;

            // 4. Số chỗ không được vượt giới hạn lớp và phòng.
            if (request.Capacity > sportClass.MaxCapacity)
            {
                throw new BusinessException(400, "Số chỗ của buổi học không được vượt giới hạn của lớp.");
            }

            if (request.RoomId.HasValue)
            {
                var room = await db.Rooms.SingleOrDefaultAsync(r =>
                    r.Id == request.RoomId.Value && r.IsActive, ct)
                    ?? throw new BusinessException(404, "Không tìm thấy phòng tập đang hoạt động.");

                if (request.Capacity > room.Capacity)
                {
                    throw new BusinessException(400, "Số chỗ của buổi học không được vượt sức chứa phòng tập.");
                }
            }

            // 5. Phòng và HLV không được có buổi học khác chồng giờ.
            // Dùng < và > để hai buổi liền nhau vẫn được phép.
            if (request.RoomId.HasValue && await db.ClassSessions.AnyAsync(s =>
                s.Id != (id ?? 0) &&
                s.Status == "Scheduled" &&
                s.RoomId == request.RoomId.Value &&
                s.StartsAt < request.EndsAt.UtcDateTime &&
                s.EndsAt > request.StartsAt.UtcDateTime, ct))
            {
                throw new BusinessException(409, "Phòng tập đã có buổi học khác trong khung giờ này.");
            }

            if (request.Capacity < enrolledCount)
            {
                throw new BusinessException(409, "Số chỗ không thể thấp hơn số học viên đã đăng ký.");
            }

            if (await db.ClassSessions.AnyAsync(s =>
                s.Id != (id ?? 0) &&
                s.Status == "Scheduled" &&
                s.CoachId == request.CoachId &&
                s.StartsAt < request.EndsAt.UtcDateTime &&
                s.EndsAt > request.StartsAt.UtcDateTime, ct))
            {
                throw new BusinessException(409, "HLV đã có buổi học khác trùng giờ.");
            }

            // 6. Đổi giờ phải kiểm tra cả lịch của các học viên đã đăng ký.
            if (id.HasValue &&
                (session.StartsAt != request.StartsAt.UtcDateTime ||
                 session.EndsAt != request.EndsAt.UtcDateTime))
            {
                var registeredMemberIds = db.ClassRegistrations
                    .Where(r => r.SessionId == id && r.Status == "Registered")
                    .Select(r => r.MemberId);

                if (await db.ClassRegistrations.AnyAsync(r =>
                    registeredMemberIds.Contains(r.MemberId) &&
                    r.SessionId != id &&
                    r.Status == "Registered" &&
                    r.Session != null &&
                    r.Session.Status == "Scheduled" &&
                    r.Session.StartsAt < request.EndsAt.UtcDateTime &&
                    r.Session.EndsAt > request.StartsAt.UtcDateTime, ct))
                {
                    throw new BusinessException(409, "Giờ mới bị trùng với buổi học khác của học viên đã đăng ký.");
                }
            }

            // So sánh với dữ liệu cũ trước khi gán các giá trị mới.
            bool shouldNotifyMembers = id.HasValue && enrolledCount > 0 && (
                session.StartsAt != request.StartsAt.UtcDateTime ||
                session.EndsAt != request.EndsAt.UtcDateTime ||
                session.CoachId != request.CoachId ||
                session.CancellationPolicyId != request.CancellationPolicyId ||
                session.RoomId != request.RoomId);

            // 7. Chỉ lưu sau khi tất cả điều kiện đã hợp lệ.
            session.CoachId = request.CoachId;
            session.CancellationPolicyId = request.CancellationPolicyId;
            session.RoomId = request.RoomId;
            session.StartsAt = request.StartsAt.UtcDateTime;
            session.EndsAt = request.EndsAt.UtcDateTime;
            session.Capacity = request.Capacity;

            if (!id.HasValue)
            {
                db.ClassSessions.Add(session);
            }

            await db.SaveChangesAsync(ct);

            // 8. Cập nhật hạn hủy và thông báo cho những học viên chưa hủy đăng ký.
            if (shouldNotifyMembers)
            {
                var registrations = await db.ClassRegistrations
                    .Include(r => r.Member)
                    .Where(r => r.SessionId == session.Id && r.Status == "Registered")
                    .ToListAsync(ct);

                foreach (var registration in registrations)
                {
                    registration.CancellationDeadline = session.StartsAt.AddHours(-policy.MinimumHoursBeforeStart);
                    db.Notifications.Add(new Notification
                    {
                        UserId = registration.Member.UserId,
                        Title = "Cập nhật lịch buổi học",
                        Content = $"Buổi #{session.Id} - {sportClass.ClassName} đã được cập nhật lịch học sang {session.StartsAt:dd/MM/yyyy HH:mm} (UTC). Vui lòng kiểm tra lại lịch của bạn.",
                        SentAt = Now
                    });
                }
            }

            Audit(actorId, nameof(ClassSession), session.Id, id.HasValue ? "Updated session" : "Created session");
            return session.Id;
        }, ct);

        return await SessionProjection(
            db.ClassSessions.AsNoTracking().Where(s => s.Id == sessionId))
            .SingleAsync(ct);
    }

    public async Task CancelSessionAsync(
        int actorId,
        int id,
        CancelRequest request,
        CancellationToken ct = default)
    {
        Validate(request);
        await Transaction(async () =>
        {
            await Manager(actorId, ct);
            var session = await Session(id, ct);
            if (session.Status == "Cancelled")
            {
                return true;
            }

            if (session.Status != "Scheduled" || session.StartsAt <= Now)
            {
                throw new BusinessException(409, "Chỉ hủy buổi học chưa bắt đầu.");
            }

            session.Status = "Cancelled";
            session.CancellationReason = request.Reason.Trim();

            // Trung tâm hủy buổi thì các đăng ký còn hiệu lực cũng phải được hủy.
            var registrations = await db.ClassRegistrations
                .Include(r => r.Member)
                .Where(r => r.SessionId == id && r.Status == "Registered")
                .ToListAsync(ct);

            foreach (var registration in registrations)
            {
                registration.Status = "Cancelled";
                registration.CancelledAt = Now;
                registration.CancelledByUserId = actorId;
                registration.CancellationReason = "Trung tâm hủy buổi: " + request.Reason.Trim();
                if (registration.CancellationReason.Length > 500)
                {
                    registration.CancellationReason = registration.CancellationReason[..500];
                }

                db.Notifications.Add(new Notification
                {
                    UserId = registration.Member.UserId,
                    Title = "Buổi học đã hủy",
                    Content = $"Buổi {id} - {session.Class.ClassName} đã hủy. Lý do: {request.Reason.Trim()}",
                    SentAt = Now
                });
            }

            await Notify(session, actorId, "Trung tâm hủy buổi học", $"Buổi {id} đã hủy; giải phóng {registrations.Count} chỗ.", true, ct);
            Audit(actorId, nameof(ClassSession), id, $"Cancelled session and {registrations.Count} registrations");
            return true;
        }, ct);
    }

    public async Task CompleteSessionAsync(
        int actorId,
        int id,
        CancellationToken ct = default)
    {
        await Transaction(async () =>
        {
            var session = await Session(id, ct);
            await EnsureSessionStaff(actorId, session, ct);
            if (session.Status == "Completed")
            {
                return true;
            }

            if (session.Status != "Scheduled" || session.EndsAt > Now)
            {
                throw new BusinessException(409, "Chỉ hoàn thành buổi học đã kết thúc và chưa bị hủy.");
            }

            session.Status = "Completed";
            var registrations = await db.ClassRegistrations
                .Where(r => r.SessionId == id && r.Status == "Registered")
                .ToListAsync(ct);

            foreach (var registration in registrations)
            {
                registration.Status = "Completed";
            }

            Audit(actorId, nameof(ClassSession), id, "Completed session");
            return true;
        }, ct);
    }

    public async Task<List<CancellationPolicy>> GetPoliciesAsync(
        int actorId,
        CancellationToken ct = default)
    {
        await Manager(actorId, ct);
        return await db.CancellationPolicies
            .AsNoTracking()
            .OrderBy(p => p.Id)
            .ToListAsync(ct);
    }

    public async Task<CancellationPolicy> SavePolicyAsync(
        int actorId,
        int? id,
        PolicyRequest request,
        CancellationToken ct = default)
    {
        Validate(request);
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new BusinessException(400, "Tên chính sách không được để trống.");
        }

        return await Transaction(async () =>
        {
            await Manager(actorId, ct);

            var policy = id.HasValue
                ? await db.CancellationPolicies.SingleOrDefaultAsync(p => p.Id == id, ct)
                    ?? throw new BusinessException(404, "Không tìm thấy chính sách.")
                : new CancellationPolicy();

            // Sửa chính sách không làm thay đổi hạn hủy của các đăng ký đã có.
            policy.Name = request.Name.Trim();
            policy.MinimumHoursBeforeStart = request.MinimumHoursBeforeStart;
            policy.IsActive = request.IsActive;

            if (!id.HasValue)
            {
                db.CancellationPolicies.Add(policy);
            }

            await db.SaveChangesAsync(ct);
            Audit(actorId, nameof(CancellationPolicy), policy.Id, "Saved cancellation policy");
            return policy;
        }, ct);
    }
}
