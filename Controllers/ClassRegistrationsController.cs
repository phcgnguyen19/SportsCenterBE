using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportsCenterAPI.Models.DTOs.Classes;
using SportsCenterAPI.Services.Interface;

namespace SportsCenterAPI.Controllers;

[ApiController, Route("api/class-registrations"), Authorize]
public class ClassRegistrationsController(IClassService service) : Flow2ControllerBase
{
    [HttpGet("mine"), Authorize(Roles = "Member")]
    public async Task<IActionResult> Mine(CancellationToken ct, int page = 1, int pageSize = 20) => Success(await service.GetRegistrationsAsync(ActorId(), null, page, pageSize, ct));
    [HttpGet("members/{memberId:int}"), Authorize(Roles = "Receptionist,Manager")]
    public async Task<IActionResult> ForMember(int memberId, CancellationToken ct, int page = 1, int pageSize = 20) => Success(await service.GetRegistrationsAsync(ActorId(), memberId, page, pageSize, ct));
    [HttpPost("{id:int}/cancel"), Authorize(Roles = "Member,Receptionist,Manager")]
    public async Task<IActionResult> Cancel(int id, CancelRequest request, CancellationToken ct) => Success(await service.CancelRegistrationAsync(ActorId(), id, request, ct), "Đã hủy và giải phóng chỗ.");
    [HttpPost("{id:int}/check-in"), Authorize(Roles = "Member,Coach,Receptionist,Manager")]
    public async Task<IActionResult> CheckIn(int id, CancellationToken ct) => Success(await service.CheckInAsync(ActorId(), id, ct), "Đã điểm danh.");
    [HttpPost("{id:int}/review"), Authorize(Roles = "Member")]
    public async Task<IActionResult> Review(int id, ReviewRequest request, CancellationToken ct) => Success(await service.ReviewAsync(ActorId(), id, request, ct));
}
