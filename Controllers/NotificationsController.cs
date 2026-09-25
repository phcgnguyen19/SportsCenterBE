using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportsCenterAPI.Data;
using SportsCenterAPI.Helpers;

namespace SportsCenterAPI.Controllers;

[ApiController, Route("api/notifications"), Authorize]
public class NotificationsController(AppDbContext db) : Flow2ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct, bool unreadOnly = false, int page = 1, int pageSize = 20)
    {
        if (page is < 1 or > 100000 || pageSize is < 1 or > 100) throw new BusinessException(400, "Phân trang không hợp lệ.");
        var actorId = ActorId();
        var query = db.Notifications.AsNoTracking().Where(n => n.UserId == actorId && (!unreadOnly || !n.IsRead));
        return Success(new { items = await query.OrderByDescending(n => n.SentAt).ThenByDescending(n => n.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).Select(n => new { n.Id, n.Title, n.Content, n.IsRead, n.SentAt }).ToListAsync(ct),
            total = await query.CountAsync(ct), page, pageSize });
    }
    [HttpPatch("{id:int}/read")]
    public async Task<IActionResult> MarkRead(int id, CancellationToken ct)
    {
        var actorId = ActorId();
        var notification = await db.Notifications.SingleOrDefaultAsync(n => n.Id == id && n.UserId == actorId, ct)
            ?? throw new BusinessException(404, "Không tìm thấy thông báo của bạn.");
        notification.IsRead = true;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
