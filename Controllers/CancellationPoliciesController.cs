using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportsCenterAPI.DTOs.Classes;
using SportsCenterAPI.DTOs.Response;
using SportsCenterAPI.Helpers;
using SportsCenterAPI.Models;
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
            "L?y danh s�ch ch�nh s�ch h?y th�nh c�ng."));
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
            "T?o ch�nh s�ch h?y th�nh c�ng."));
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
            "C?p nh?t ch�nh s�ch h?y th�nh c�ng."));
    }

    private int GetActorId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(value, out var actorId))
        {
            throw new BusinessException(401, "Vui l�ng ??ng nh?p.");
        }

        return actorId;
    }
}