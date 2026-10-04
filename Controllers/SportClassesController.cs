using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportsCenterAPI.DTOs.Classes;
using SportsCenterAPI.DTOs.Response;
using SportsCenterAPI.Services.Interface;

namespace SportsCenterAPI.Controllers;

[ApiController]
[Route("api/classes")]
[Authorize(Roles = "Manager")]
public class SportClassesController : BaseApiController
{
    private readonly IClassService _classService;

    public SportClassesController(IClassService classService)
    {
        _classService = classService;
    }

    // 1. Tìm kiếm lớp học.
    // Không cần đăng nhập.
    // GET /api/classes
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Search(
        [FromQuery] ClassSearch search,
        CancellationToken cancellationToken)
    {
        var classes = await _classService.SearchClassesAsync(
            search,
            cancellationToken);

        return Success(classes);
    }

    // 2. Quản lý xem danh sách lớp, gồm cả lớp ngừng hoạt động.
    // GET /api/classes/manage
    [HttpGet("manage")]
    public async Task<IActionResult> Manage(
        [FromQuery] ClassSearch search,
        CancellationToken cancellationToken)
    {
        var managerUserId = ActorId();

        var classes = await _classService.GetManagedClassesAsync(
            managerUserId,
            search,
            cancellationToken);

        return Success(classes);
    }

    // 3. Xem thông tin một lớp học.
    // Không cần đăng nhập.
    // GET /api/classes/5
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> Get(
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        var sportClass = await _classService.GetClassAsync(
            id,
            cancellationToken);

        return Success(sportClass);
    }

    // 4. Quản lý tạo lớp học.
    // POST /api/classes
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] ClassRequest request,
        CancellationToken cancellationToken)
    {
        var managerUserId = ActorId();

        // Truyền null cho ID vì đang tạo lớp mới.
        var sportClass = await _classService.SaveClassAsync(
            managerUserId,
            null,
            request,
            cancellationToken);

        var response = ApiResponse<ClassDTO>.CreatedAt(
            sportClass,
            "Đã tạo lớp học.");

        // Trả HTTP 201 và đường dẫn xem lớp vừa tạo.
        return CreatedAtAction(
            nameof(Get),
            new { id = sportClass.Id },
            response);
    }

    // 5. Quản lý cập nhật lớp học.
    // PUT /api/classes/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        [FromRoute] int id,
        [FromBody] ClassRequest request,
        CancellationToken cancellationToken)
    {
        var managerUserId = ActorId();

        // Truyền ID của lớp cần cập nhật.
        var sportClass = await _classService.SaveClassAsync(
            managerUserId,
            id,
            request,
            cancellationToken);

        return Success(sportClass);
    }

    // 6. Quản lý ngừng hoạt động lớp học.
    // Service thực hiện xóa mềm bằng IsActive = false.
    // DELETE /api/classes/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Deactivate(
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        var managerUserId = ActorId();

        await _classService.DeactivateClassAsync(
            managerUserId,
            id,
            cancellationToken);

        return NoContent();
    }

    // 7. Xem danh sách buổi học thuộc một lớp.
    // Không cần đăng nhập.
    // GET /api/classes/5/sessions
    [HttpGet("{id:int}/sessions")]
    [AllowAnonymous]
    public async Task<IActionResult> Sessions(
        [FromRoute] int id,
        [FromQuery] ClassSearch search,
        CancellationToken cancellationToken)
    {
        var sessions = await _classService.SearchSessionsAsync(
            search,
            classId: id,
            ct: cancellationToken);

        return Success(sessions);
    }

    // 8. Quản lý thêm buổi học vào lớp.
    // POST /api/classes/5/sessions
    [HttpPost("{id:int}/sessions")]
    public async Task<IActionResult> AddSession(
        [FromRoute] int id,
        [FromBody] SessionRequest request,
        CancellationToken cancellationToken)
    {
        var managerUserId = ActorId();

        // null là ID buổi học: tạo mới.
        var session = await _classService.SaveSessionAsync(
            managerUserId,
            id,
            null,
            request,
            cancellationToken);

        var response = ApiResponse<SessionDTO>.CreatedAt(
            session,
            "Đã tạo buổi học.");

        return StatusCode(
            StatusCodes.Status201Created,
            response);
    }

    // 9. Quản lý cập nhật một buổi học trong lớp.
    // PUT /api/classes/5/sessions/10
    [HttpPut("{id:int}/sessions/{sessionId:int}")]
    public async Task<IActionResult> UpdateSession(
        [FromRoute] int id,
        [FromRoute] int sessionId,
        [FromBody] SessionRequest request,
        CancellationToken cancellationToken)
    {
        var managerUserId = ActorId();

        var session = await _classService.SaveSessionAsync(
            managerUserId,
            id,
            sessionId,
            request,
            cancellationToken);

        return Success(session);
    }
}