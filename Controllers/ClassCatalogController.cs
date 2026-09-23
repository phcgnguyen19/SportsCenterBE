using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportsCenterAPI.Data;
using SportsCenterAPI.Helpers;
using SportsCenterAPI.Models;

namespace SportsCenterAPI.Controllers;

public class SportRequest
{
    [Required, StringLength(120)] public string Name { get; set; } = string.Empty;
    [StringLength(2000)] public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}
public class CoachProfileRequest
{
    [StringLength(200)] public string? Specialization { get; set; }
    [StringLength(2000)] public string? Bio { get; set; }
    [Range(0, 80)] public int YearsOfExperience { get; set; }
}

[ApiController, Route("api"), Authorize(Roles = "Manager")]
public class ClassCatalogController(AppDbContext db) : Flow2ControllerBase
{
    [HttpGet("sports"), AllowAnonymous]
    public async Task<IActionResult> Sports(CancellationToken ct) => Success(await db.Sports.AsNoTracking().Where(s => s.IsActive)
        .OrderBy(s => s.Id).Select(s => new { s.Id, s.Name, s.Description }).ToListAsync(ct));
    [HttpPost("sports")]
    public async Task<IActionResult> CreateSport(SportRequest request, CancellationToken ct)
    {
        var sport = new Sport { Name = request.Name.Trim(), Description = request.Description?.Trim(), IsActive = request.IsActive };
        db.Sports.Add(sport); await db.SaveChangesAsync(ct);
        return Success(new { sport.Id, sport.Name, sport.Description, sport.IsActive });
    }
    [HttpPut("sports/{id:int}")]
    public async Task<IActionResult> UpdateSport(int id, SportRequest request, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var sport = await db.Sports.SingleOrDefaultAsync(s => s.Id == id, ct) ?? throw new BusinessException(404, "Không tìm thấy bộ môn.");
        if (!request.IsActive && await db.ClassSessions.AnyAsync(s => s.Class.SportId == id && s.Status == "Scheduled", ct))
            throw new BusinessException(409, "Bộ môn vẫn còn buổi học chưa hoàn thành/hủy.");
        sport.Name = request.Name.Trim(); sport.Description = request.Description?.Trim(); sport.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        return Success(new { sport.Id, sport.Name, sport.Description, sport.IsActive });
    }
    [HttpGet("coaches"), AllowAnonymous]
    public async Task<IActionResult> Coaches(CancellationToken ct) => Success(await db.Coaches.AsNoTracking()
        .Where(c => c.User.IsActive && c.User.Role == "Coach").OrderBy(c => c.Id)
        .Select(c => new { c.Id, c.User.FullName, c.Specialization, c.Bio, c.YearsOfExperience }).ToListAsync(ct));
    [HttpPut("coaches/{id:int}/profile")]
    public async Task<IActionResult> UpdateCoach(int id, CoachProfileRequest request, CancellationToken ct)
    {
        var coach = await db.Coaches.SingleOrDefaultAsync(c => c.Id == id, ct) ?? throw new BusinessException(404, "Không tìm thấy HLV.");
        coach.Specialization = request.Specialization?.Trim(); coach.Bio = request.Bio?.Trim(); coach.YearsOfExperience = request.YearsOfExperience;
        await db.SaveChangesAsync(ct);
        return Success(new { coach.Id, coach.Specialization, coach.Bio, coach.YearsOfExperience });
    }
}
