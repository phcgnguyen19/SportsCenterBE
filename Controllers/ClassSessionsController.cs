using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportsCenterAPI.Models.DTOs.Classes;
using SportsCenterAPI.Models.DTOs.Response;
using SportsCenterAPI.Services.Interface;

namespace SportsCenterAPI.Controllers;

[ApiController, Route("api/class-sessions"), Authorize]
public class ClassSessionsController(IClassService service) : Flow2ControllerBase
{
    [HttpGet, AllowAnonymous]
    public async Task<IActionResult> Search([FromQuery] ClassSearch search, CancellationToken ct) => Success(await service.SearchSessionsAsync(search, ct: ct));
    [HttpGet("mine"), Authorize(Roles = "Coach")]
    public async Task<IActionResult> Mine([FromQuery] ClassSearch search, CancellationToken ct) => Success(await service.SearchSessionsAsync(search, actorCoachId: ActorId(), ct: ct));
    [HttpPost("{id:int}/registrations"), Authorize(Roles = "Member")]
    public async Task<IActionResult> Book(int id, CancellationToken ct) => StatusCode(201,
        ApiResponse<RegistrationDTO>.CreatedAt(await service.BookAsync(ActorId(), id, ct: ct), "Đặt lịch thành công."));
    [HttpPost("{id:int}/registrations/members/{memberId:int}"), Authorize(Roles = "Receptionist,Manager")]
    public async Task<IActionResult> BookForMember(int id, int memberId, CancellationToken ct) => StatusCode(201,
        ApiResponse<RegistrationDTO>.CreatedAt(await service.BookAsync(ActorId(), id, memberId, ct), "Đã đặt lịch cho hội viên."));
    [HttpGet("{id:int}/roster"), Authorize(Roles = "Coach,Receptionist,Manager")]
    public async Task<IActionResult> Roster(int id, CancellationToken ct) => Success(await service.GetRosterAsync(ActorId(), id, ct));
    [HttpPost("{id:int}/cancel"), Authorize(Roles = "Manager")]
    public async Task<IActionResult> Cancel(int id, CancelRequest request, CancellationToken ct)
    {
        await service.CancelSessionAsync(ActorId(), id, request, ct); return NoContent();
    }
    [HttpPost("{id:int}/complete"), Authorize(Roles = "Coach,Receptionist,Manager")]
    public async Task<IActionResult> Complete(int id, CancellationToken ct)
    {
        await service.CompleteSessionAsync(ActorId(), id, ct); return NoContent();
    }
    [HttpGet("{id:int}/reviews"), AllowAnonymous]
    public async Task<IActionResult> Reviews(int id, CancellationToken ct, int page = 1, int pageSize = 20) =>
        Success(await service.GetReviewsAsync(id, page, pageSize, ct));
}
