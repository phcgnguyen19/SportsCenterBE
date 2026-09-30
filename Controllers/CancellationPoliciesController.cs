using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportsCenterAPI.Helpers;
using SportsCenterAPI.Models;
using SportsCenterAPI.Models.DTOs.Classes;
using SportsCenterAPI.Models.DTOs.Response;
using SportsCenterAPI.Services.Interface;

namespace SportsCenterAPI.Controllers;

[ApiController]
[Route("api/cancellation-policies")]
[Authorize(Roles = "Manager")]
public class CancellationPoliciesController(IClassService service)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(
        typeof(ApiResponse<List<CancellationPolicy>>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<List<CancellationPolicy>>>> Get(
        CancellationToken ct)
    {
        var policies = await service.GetPoliciesAsync(GetActorId(), ct);

        return Ok(ApiResponse<List<CancellationPolicy>>.Ok(
            policies,
            "L?y danh sách chính sách h?y thành công."));
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        PolicyRequest request,
        CancellationToken ct)
    {
        var policy = await service.SavePolicyAsync(
            GetActorId(), null, request, ct);

        return Ok(ApiResponse<CancellationPolicy>.Ok(
            policy,
            "T?o chính sách h?y thành công."));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        PolicyRequest request,
        CancellationToken ct)
    {
        var policy = await service.SavePolicyAsync(
            GetActorId(), id, request, ct);

        return Ok(ApiResponse<CancellationPolicy>.Ok(
            policy,
            "C?p nh?t chính sách h?y thành công."));
    }

    private int GetActorId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(value, out var actorId))
        {
            throw new BusinessException(401, "Vui lòng ??ng nh?p.");
        }

        return actorId;
    }
}