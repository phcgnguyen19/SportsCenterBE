using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportsCenterAPI.Helpers;
using SportsCenterAPI.Models.DTOs.Accounts;
using SportsCenterAPI.Models.DTOs.Register;
using SportsCenterAPI.Models.DTOs.Response;
using SportsCenterAPI.Services.Interface;

namespace SportsCenterAPI.Controllers;

[ApiController]
[Route("api/members")]
[Authorize]
public class MembersController(IAccountService accountService, IAuthService authService) : ControllerBase
{
    /// <summary>Register a walk-in member. The member supplies their initial password; no JWT is returned to staff.</summary>
    [HttpPost]
    [Authorize(Roles = UserRoles.FrontDesk)]
    [ProducesResponseType(typeof(ApiResponse<MemberResponseDTO>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<MemberResponseDTO>>> RegisterAtDesk(RegisterRequestDTO request)
    {
        var member = await authService.RegisterAtDeskAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = member.MemberId },
            ApiResponse<MemberResponseDTO>.CreatedAt(member, "Member registered successfully"));
    }

    [HttpGet]
    [Authorize(Roles = UserRoles.FrontDesk)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<MemberResponseDTO>>>> GetAll()
    {
        var members = await accountService.GetMembersAsync();
        return Ok(ApiResponse<IReadOnlyList<MemberResponseDTO>>.Ok(members, "Members retrieved"));
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = UserRoles.FrontDesk)]
    public async Task<ActionResult<ApiResponse<MemberResponseDTO>>> GetById(int id)
    {
        var member = await accountService.GetMemberAsync(id);
        return Ok(ApiResponse<MemberResponseDTO>.Ok(member, "Member retrieved"));
    }

    [HttpGet("me")]
    [Authorize(Roles = UserRoles.Member)]
    public async Task<ActionResult<ApiResponse<MemberResponseDTO>>> GetMe()
    {
        var member = await accountService.GetMyMemberAsync(GetCurrentUserId());
        return Ok(ApiResponse<MemberResponseDTO>.Ok(member, "Member profile retrieved"));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = UserRoles.Manager)]
    public async Task<IActionResult> Deactivate(int id)
    {
        await accountService.DeactivateMemberAsync(id);
        return NoContent();
    }

    private int GetCurrentUserId() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id : throw new UnauthorizedAccessException("Invalid user identity.");
}
