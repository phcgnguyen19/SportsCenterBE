using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayOS.Exceptions;
using PayOS.Models.Webhooks;
using SportsCenterAPI.DTOs.Response;
using SportsCenterAPI.Helpers;
using SportsCenterAPI.Models.DTOs.Payments;
using SportsCenterAPI.Services.Interface;

namespace SportsCenterAPI.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize(Roles = "Member,Receptionist,Manager")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    // GET: api/payments
    // Service giới hạn Member chỉ xem thanh toán của mình.
    [HttpGet]
    [ProducesResponseType(
        typeof(ApiResponse<List<PaymentDTO>>),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var response = await _paymentService.GetAllAsync(ActorId());

        return StatusCode(response.StatusCode, response);
    }

    // GET: api/payments/1
    [HttpGet("{id:int:min(1)}")]
    [ProducesResponseType(
        typeof(ApiResponse<PaymentDTO>),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById([FromRoute] int id)
    {
        var response = await _paymentService.GetByIdAsync(
            id,
            ActorId());

        return StatusCode(response.StatusCode, response);
    }

    // POST: api/payments
    // Tạo hoặc lấy lại yêu cầu thanh toán Pending.
    [HttpPost]
    [ProducesResponseType(
        typeof(ApiResponse<PaymentDTO>),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> Create(
        [FromBody] CreatePaymentDTO dto)
    {
        var response = await _paymentService.CreateAsync(
            dto,
            ActorId());

        return StatusCode(response.StatusCode, response);
    }

    // POST: api/payments/1/sync
    // Tra cứu trạng thái thực tế từ payOS.
    [HttpPost("{id:int:min(1)}/sync")]
    [ProducesResponseType(
        typeof(ApiResponse<PaymentDTO>),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> Sync([FromRoute] int id)
    {
        var response = await _paymentService.SyncAsync(
            id,
            ActorId());

        return StatusCode(response.StatusCode, response);
    }

    // POST: api/payments/1/cancel
    [HttpPost("{id:int:min(1)}/cancel")]
    [ProducesResponseType(
        typeof(ApiResponse<PaymentDTO>),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> Cancel(
        [FromRoute] int id,
        [FromBody] CancelPaymentDTO dto)
    {
        var response = await _paymentService.CancelAsync(
            id,
            dto.Reason,
            ActorId());

        return StatusCode(response.StatusCode, response);
    }

    // POST: api/payments/payos/webhook
    // payOS gọi endpoint này; xác minh chữ ký trong service.
    [AllowAnonymous]
    [HttpPost("payos/webhook")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponse<object>),
        StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PayOSWebhook(
        [FromBody] Webhook dto)
    {
        try
        {
            await _paymentService.PayOSWebhookAsync(dto);

            return Ok(new
            {
                success = true
            });
        }
        catch (WebhookException)
        {
            return BadRequest(
                ApiResponse<object>.BadRequest(
                    "Dữ liệu hoặc chữ ký webhook không hợp lệ."));
        }
    }

    private int ActorId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(value, out var actorId) || actorId <= 0)
        {
            throw new BusinessException(
                StatusCodes.Status401Unauthorized,
                "Thông tin đăng nhập không hợp lệ.");
        }

        return actorId;
    }
}