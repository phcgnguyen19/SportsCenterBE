using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportsCenterAPI.Models.DTOs.Classes;
using SportsCenterAPI.Models.DTOs.Response;
using SportsCenterAPI.Services.Interface;

namespace SportsCenterAPI.Controllers;

[ApiController, Route("api/classes"), Authorize(Roles = "Manager")]
public class SportClassesController(IClassService service) : Flow2ControllerBase
{
    [HttpGet, AllowAnonymous]
    public async Task<IActionResult> Search([FromQuery] ClassSearch search, CancellationToken ct) => Success(await service.SearchClassesAsync(search, ct));
    [HttpGet("manage")]
    public async Task<IActionResult> Manage([FromQuery] ClassSearch search, CancellationToken ct) => Success(await service.GetManagedClassesAsync(ActorId(), search, ct));
    [HttpGet("{id:int}"), AllowAnonymous]
    public async Task<IActionResult> Get(int id, CancellationToken ct) => Success(await service.GetClassAsync(id, ct));
    [HttpPost]
    public async Task<IActionResult> Create(ClassRequest request, CancellationToken ct)
    {
        var item = await service.SaveClassAsync(ActorId(), null, request, ct);
        return CreatedAtAction(nameof(Get), new { id = item.Id }, ApiResponse<ClassDTO>.CreatedAt(item, "Đã tạo lớp học."));
    }
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, ClassRequest request, CancellationToken ct) => Success(await service.SaveClassAsync(ActorId(), id, request, ct));
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
    {
        await service.DeactivateClassAsync(ActorId(), id, ct); return NoContent();
    }
    [HttpGet("{id:int}/sessions"), AllowAnonymous]
    public async Task<IActionResult> Sessions(int id, [FromQuery] ClassSearch search, CancellationToken ct) => Success(await service.SearchSessionsAsync(search, id, ct: ct));
    [HttpPost("{id:int}/sessions")]
    public async Task<IActionResult> AddSession(int id, SessionRequest request, CancellationToken ct)
    {
        var result = await service.SaveSessionAsync(ActorId(), id, null, request, ct);
        return StatusCode(201, ApiResponse<SessionDTO>.CreatedAt(result, "Đã tạo buổi học."));
    }
    [HttpPut("{id:int}/sessions/{sessionId:int}")]
    public async Task<IActionResult> UpdateSession(int id, int sessionId, SessionRequest request, CancellationToken ct) =>
        Success(await service.SaveSessionAsync(ActorId(), id, sessionId, request, ct));
}
