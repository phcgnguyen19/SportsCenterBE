using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq.Expressions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SportsCenterAPI.Data;
using SportsCenterAPI.Helpers;
using SportsCenterAPI.Models;
using SportsCenterAPI.Models.DTOs.Classes;
using SportsCenterAPI.Services.Interface;

namespace SportsCenterAPI.Services.Implement;

public partial class ClassService(AppDbContext db, TimeProvider clock) : IClassService
{
    private DateTime Now => clock.GetUtcNow().UtcDateTime;
    private static bool IsDesk(User user) => user.Role is "Manager" or "Receptionist";
    private async Task<User> Actor(int id, CancellationToken ct) =>
        await db.Users.SingleOrDefaultAsync(u => u.Id == id && u.IsActive, ct)
        ?? throw new BusinessException(401, "Tài khoản không hoạt động. Vui lòng đăng nhập lại.");
    private async Task<User> Manager(int id, CancellationToken ct)
    {
        var actor = await Actor(id, ct);
        if (actor.Role != "Manager") throw new BusinessException(403, "Chỉ Manager được thực hiện thao tác này.");
        return actor;
    }
    private async Task<Member> ResolveMember(int actorId, int? memberId, CancellationToken ct)
    {
        var actor = await Actor(actorId, ct);
        if (memberId.HasValue && !IsDesk(actor)) throw new BusinessException(403, "Chỉ lễ tân hoặc Manager được chọn hội viên khác.");
        if (!memberId.HasValue && actor.Role != "Member") throw new BusinessException(403, "Nhân viên cần chọn hội viên cụ thể.");
        var query = db.Members.Include(m => m.User).AsQueryable();
        query = memberId.HasValue ? query.Where(m => m.Id == memberId) : query.Where(m => m.UserId == actorId);
        return await query.SingleOrDefaultAsync(ct) ?? throw new BusinessException(404, "Không tìm thấy hội viên.");
    }
    private async Task<ClassSession> Session(int id, CancellationToken ct) =>
        await db.ClassSessions.Include(s => s.Class).ThenInclude(c => c.Sport)
            .Include(s => s.Coach).ThenInclude(c => c.User).Include(s => s.CancellationPolicy)
            .SingleOrDefaultAsync(s => s.Id == id, ct) ?? throw new BusinessException(404, "Không tìm thấy buổi học.");
    private async Task EnsureSessionStaff(int actorId, ClassSession session, CancellationToken ct)
    {
        var actor = await Actor(actorId, ct);
        if (!IsDesk(actor) && !(actor.Role == "Coach" && session.Coach.UserId == actorId))
            throw new BusinessException(403, "Bạn không có quyền quản lý học viên của buổi học này.");
    }
    private static void Validate(object request)
    {
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(request, new ValidationContext(request), errors, true))
            throw new BusinessException(400, string.Join(" ", errors.Select(e => e.ErrorMessage)));
    }
    private static void Pagination(int page, int size)
    {
        if (page is < 1 or > 100000 || size is < 1 or > 100) throw new BusinessException(400, "Page từ 1 đến 100000; PageSize từ 1 đến 100.");
    }
    private void Audit(int actorId, string entity, int id, string details) => db.AuditLogs.Add(new AuditLog
    {
        UserId = actorId, Action = "Update", EntityName = entity, EntityId = id, Details = details, Timestamp = Now
    });
    private async Task Notify(ClassSession session, int memberUserId, string title, string content, bool includeManagers, CancellationToken ct)
    {
        var recipients = new HashSet<int> { memberUserId, session.Coach.UserId };
        if (includeManagers)
            recipients.UnionWith(await db.Users.Where(u => u.Role == "Manager" && u.IsActive).Select(u => u.Id).ToListAsync(ct));
        foreach (var userId in recipients)
            db.Notifications.Add(new Notification { UserId = userId, Title = title, Content = content, SentAt = Now });
    }
    private async Task<T> Transaction<T>(Func<Task<T>> action, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            var result = await action();
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return result;
        }
        catch (Exception ex) when (ex is DbUpdateConcurrencyException || SqlConflict(ex))
        {
            throw new BusinessException(409, "Dữ liệu vừa thay đổi bởi yêu cầu khác. Vui lòng tải lại và thử lại.");
        }
    }
    private static bool SqlConflict(Exception ex)
    {
        for (Exception? current = ex; current != null; current = current.InnerException)
            if (current is SqlException sql && sql.Number is 1205 or 2601 or 2627) return true;
        return false;
    }
    private static readonly Expression<Func<SportClass, ClassDTO>> ClassProjection = c => new(
        c.Id, c.ClassName, c.SportId, c.Sport.Name, c.CoachId, c.Coach == null ? null : c.Coach.User.FullName,
        c.Price, c.MaxCapacity, c.Schedule, c.StartDate, c.EndDate, c.IsActive);
    private static readonly Expression<Func<ClassRegistration, RegistrationDTO>> RegistrationProjection = r => new(
        r.Id, r.MemberId, r.ClassId, r.SessionId, r.Class.ClassName,
        r.Session == null ? null : r.Session.StartsAt, r.Session == null ? null : r.Session.EndsAt,
        r.Status, r.RegistrationDate, r.CreatedByUserId, r.CancellationDeadline, r.CancelledAt, r.CancelledByUserId, r.CancellationReason);
    private Task<RegistrationDTO> RegistrationResult(int id, CancellationToken ct) =>
        db.ClassRegistrations.AsNoTracking().Where(r => r.Id == id).Select(RegistrationProjection).SingleAsync(ct);
    private IQueryable<SessionDTO> SessionProjection(IQueryable<ClassSession> query)
    {
        var now = Now;
        return query.Select(s => new SessionDTO(s.Id, s.ClassId, s.Class.ClassName, s.CoachId, s.Coach.User.FullName,
            s.StartsAt, s.EndsAt, s.Capacity, s.Registrations.Count(r => r.Status != "Cancelled"),
            s.Status == "Scheduled" && s.StartsAt > now && s.Class.IsActive && s.Class.Sport.IsActive && s.Coach.User.IsActive && s.Coach.User.Role == "Coach" && s.CancellationPolicy.IsActive
                ? s.Capacity - s.Registrations.Count(r => r.Status != "Cancelled") : 0,
            s.Status, s.CancellationPolicyId, s.CancellationPolicy.MinimumHoursBeforeStart));
    }
    public Task<PageResult<ClassDTO>> SearchClassesAsync(ClassSearch search, CancellationToken ct = default) => SearchClasses(search, false, ct);
    public async Task<PageResult<ClassDTO>> GetManagedClassesAsync(int actorId, ClassSearch search, CancellationToken ct = default)
    {
        await Manager(actorId, ct);
        return await SearchClasses(search, true, ct);
    }
    private async Task<PageResult<ClassDTO>> SearchClasses(ClassSearch search, bool includeInactive, CancellationToken ct)
    {
        Validate(search);
        if (search.From >= search.To) throw new BusinessException(400, "From phải trước To.");
        var query = db.SportClasses.AsNoTracking().AsQueryable();
        if (!includeInactive) query = query.Where(c => c.IsActive && c.Sport.IsActive);
        if (!string.IsNullOrWhiteSpace(search.Search)) query = query.Where(c => c.ClassName.Contains(search.Search.Trim()));
        if (search.SportId.HasValue) query = query.Where(c => c.SportId == search.SportId);
        if (search.CoachId.HasValue) query = query.Where(c => c.CoachId == search.CoachId);
        if (search.From.HasValue) query = query.Where(c => c.EndDate > search.From.Value.UtcDateTime);
        if (search.To.HasValue) query = query.Where(c => c.StartDate < search.To.Value.UtcDateTime);
        if (search.OnlyAvailable)
        {
            var now = Now;
            query = query.Where(c => c.IsActive && c.Sport.IsActive && db.ClassSessions.Any(s => s.ClassId == c.Id && s.Status == "Scheduled" && s.StartsAt > now &&
                s.Coach.User.IsActive && s.Coach.User.Role == "Coach" && s.CancellationPolicy.IsActive && s.Registrations.Count(r => r.Status != "Cancelled") < s.Capacity));
        }
        return new(await query.OrderBy(c => c.Id).Skip((search.Page - 1) * search.PageSize).Take(search.PageSize).Select(ClassProjection).ToListAsync(ct),
            await query.CountAsync(ct), search.Page, search.PageSize);
    }
    public async Task<ClassDTO> GetClassAsync(int id, CancellationToken ct = default) =>
        await db.SportClasses.AsNoTracking().Where(c => c.Id == id).Select(ClassProjection).SingleOrDefaultAsync(ct)
        ?? throw new BusinessException(404, "Không tìm thấy lớp học.");

    public async Task<PageResult<SessionDTO>> SearchSessionsAsync(ClassSearch search, int? classId = null, int? actorCoachId = null, CancellationToken ct = default)
    {
        Validate(search);
        if (search.From >= search.To) throw new BusinessException(400, "From phải trước To.");
        var query = db.ClassSessions.AsNoTracking().AsQueryable();
        if (actorCoachId.HasValue)
        {
            var actor = await Actor(actorCoachId.Value, ct);
            if (actor.Role != "Coach") throw new BusinessException(403, "Chỉ dành cho HLV.");
            query = query.Where(s => s.Coach.UserId == actor.Id);
        }
        else query = query.Where(s => s.Class.IsActive && s.Class.Sport.IsActive);
        if (classId.HasValue) query = query.Where(s => s.ClassId == classId);
        if (!string.IsNullOrWhiteSpace(search.Search)) query = query.Where(s => s.Class.ClassName.Contains(search.Search.Trim()));
        if (search.SportId.HasValue) query = query.Where(s => s.Class.SportId == search.SportId);
        if (search.CoachId.HasValue) query = query.Where(s => s.CoachId == search.CoachId);
        if (search.From.HasValue) query = query.Where(s => s.StartsAt >= search.From.Value.UtcDateTime);
        if (search.To.HasValue) query = query.Where(s => s.StartsAt < search.To.Value.UtcDateTime);
        if (search.OnlyAvailable)
        {
            var now = Now;
            query = query.Where(s => s.Status == "Scheduled" && s.StartsAt > now && s.Coach.User.IsActive && s.Coach.User.Role == "Coach" &&
                s.CancellationPolicy.IsActive && s.Registrations.Count(r => r.Status != "Cancelled") < s.Capacity);
        }
        return new(await SessionProjection(query.OrderBy(s => s.StartsAt).ThenBy(s => s.Id).Skip((search.Page - 1) * search.PageSize).Take(search.PageSize)).ToListAsync(ct),
            await query.CountAsync(ct), search.Page, search.PageSize);
    }
}
