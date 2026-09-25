using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportsCenterAPI.Models.DTOs.Classes;
using SportsCenterAPI.Services.Interface;

namespace SportsCenterAPI.Controllers;

[ApiController, Route("api/cancellation-policies"), Authorize(Roles = "Manager")]
public class CancellationPoliciesController(IClassService service) : Flow2ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct) => Success(await service.GetPoliciesAsync(ActorId(), ct));
    [HttpPost]
    public async Task<IActionResult> Create(PolicyRequest request, CancellationToken ct) => Success(await service.SavePolicyAsync(ActorId(), null, request, ct));
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, PolicyRequest request, CancellationToken ct) => Success(await service.SavePolicyAsync(ActorId(), id, request, ct));
}
