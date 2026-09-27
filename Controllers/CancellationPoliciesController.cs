using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportsCenterAPI.Models;
using SportsCenterAPI.Models.DTOs.Classes;
using SportsCenterAPI.Models.DTOs.Response;
using SportsCenterAPI.Services.Interface;

namespace SportsCenterAPI.Controllers;

[ApiController]
[Route("api/cancellation-policies")]
[Authorize(Roles = "Manager")]
public class CancellationPoliciesController(IClassService classService) : Flow2ControllerBase
{
    [HttpGet]
    [ProducesResponseType(
        typeof(ApiResponse<List<CancellationPolicy>>),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>),StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>),StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<List<CancellationPolicy>>>> Get(
        CancellationToken ct)
    {
        var policies = await classService.GetPoliciesAsync(ActorId(), ct);

        return Ok(
            ApiResponse<List<CancellationPolicy>>.Ok(
                policies,
                "Cancellation policies retrieved successfully"));
    }

    [HttpPost]
    [ProducesResponseType( typeof(ApiResponse<CancellationPolicy>),StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>),StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>),StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>),StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<CancellationPolicy>>> Create(
        PolicyRequest request,
        CancellationToken ct)
    {
        var policy = await classService.SavePolicyAsync(
            ActorId(),
            null,
            request,
            ct);

        return StatusCode(
            StatusCodes.Status201Created,
            ApiResponse<CancellationPolicy>.CreatedAt(
                policy,
                "Cancellation policy created successfully"));
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<CancellationPolicy>),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>),StatusCodes.Status400BadRequest)]
    [ProducesResponseType( typeof(ApiResponse<object>),StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>),StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CancellationPolicy>>> Update(
        int id,
        PolicyRequest request,
        CancellationToken ct)
    {
        var policy = await classService.SavePolicyAsync(
            ActorId(),
            id,
            request,
            ct);

        return Ok(
            ApiResponse<CancellationPolicy>.Ok(
                policy,
                "Cancellation policy updated successfully"));
    }
}