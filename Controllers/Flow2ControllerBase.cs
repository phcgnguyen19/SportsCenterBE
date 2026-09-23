using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SportsCenterAPI.Helpers;
using SportsCenterAPI.Models.DTOs.Response;

namespace SportsCenterAPI.Controllers;

public abstract class Flow2ControllerBase : ControllerBase
{
    protected int ActorId() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
        ? id : throw new BusinessException(401, "Vui lòng đăng nhập.");
    protected IActionResult Success<T>(T data, string message = "Thành công.") => Ok(ApiResponse<T>.Ok(data, message));
}
