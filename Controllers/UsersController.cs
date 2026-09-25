using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportsCenterAPI.Helpers;
using SportsCenterAPI.Models.DTOs.Accounts;
using SportsCenterAPI.Models.DTOs.Response;
using SportsCenterAPI.Services.Interface;

namespace SportsCenterAPI.Controllers;

[ApiController]
[Route("api/users/staff")]
[Authorize(Roles = UserRoles.Manager)]
public class UsersController(IAccountService accountService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<StaffResponseDTO>>>> GetAll()
    {
        var staff = await accountService.GetStaffAsync();
        return Ok(ApiResponse<IReadOnlyList<StaffResponseDTO>>.Ok(staff, "Staff accounts retrieved"));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<StaffResponseDTO>>> GetById(int id)
    {
        var staff = await accountService.GetStaffAsync(id);
        return Ok(ApiResponse<StaffResponseDTO>.Ok(staff, "Staff account retrieved"));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<StaffResponseDTO>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<StaffResponseDTO>>> Create(CreateStaffRequestDTO request)
    {
        var staff = await accountService.CreateStaffAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = staff.UserId },
            ApiResponse<StaffResponseDTO>.CreatedAt(staff, "Staff account created"));
    }

    [HttpPut("{id:int}/role")]
    public async Task<ActionResult<ApiResponse<StaffResponseDTO>>> UpdateRole(int id, UpdateStaffRoleRequestDTO request)
    {
        var staff = await accountService.UpdateStaffRoleAsync(id, request, GetCurrentUserId());
        return Ok(ApiResponse<StaffResponseDTO>.Ok(staff, "Staff role updated"));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Deactivate(int id)
    {
        await accountService.DeactivateStaffAsync(id, GetCurrentUserId());
        return NoContent();
    }

    private int GetCurrentUserId() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id : throw new UnauthorizedAccessException("Invalid user identity.");
}
