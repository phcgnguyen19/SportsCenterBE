using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayOS.Exceptions;
using PayOS.Models.Webhooks;
using QuestPDF.Fluent;
using SportsCenterAPI.DTOs.Response;
using SportsCenterAPI.Helpers;
using SportsCenterAPI.Models.DTOs.Payments;
using SportsCenterAPI.Services.Interface;
using System.Security.Claims;

namespace SportsCenterAPI.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize(Roles = "Member,Receptionist,Manager")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly SportsCenterAPI.Data.AppDbContext _context;

    public PaymentsController(IPaymentService paymentService, SportsCenterAPI.Data.AppDbContext context)
    {
        _paymentService = paymentService;
        _context = context;
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

    [HttpGet("{id}/export-pdf")]
    [Authorize(Roles = "Receptionist,Manager,Member")]
    public async Task<IActionResult> ExportInvoicePdf(int id)
    {
        // 1. Lấy lịch sử giao dịch và thông tin KH từ Database
        var payment = await _context.Payments
            .Include(p => p.Member).ThenInclude(m => m.User)
            .Include(p => p.Subscription).ThenInclude(s => s.Package)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (payment == null) return NotFound("Không tìm thấy giao dịch.");
        if (payment.Status != "Completed") return BadRequest("Chỉ có thể xuất hóa đơn cho giao dịch đã hoàn tất.");

        // 2. Bắt đầu vẽ File PDF
        var document = QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(QuestPDF.Helpers.PageSizes.A5); // Hóa đơn thường dùng giấy A5
                page.Margin(1.5f, QuestPDF.Infrastructure.Unit.Centimetre);
                page.PageColor(QuestPDF.Helpers.Colors.White);
                page.DefaultTextStyle(x => x.FontSize(11));

                // Tiêu đề
                page.Header().AlignCenter().Text("HÓA ĐƠN THANH TOÁN")
                    .SemiBold().FontSize(20).FontColor(QuestPDF.Helpers.Colors.Blue.Darken2);

                // Nội dung thông tin Khách hàng & Giao dịch
                page.Content().PaddingVertical(1, QuestPDF.Infrastructure.Unit.Centimetre).Column(x =>
                {
                    x.Spacing(8);
                    x.Item().Text($"Mã hóa đơn: {payment.TransactionReference ?? "INV-" + payment.Id}");
                    x.Item().Text($"Ngày thanh toán: {payment.PaymentDate:dd/MM/yyyy HH:mm}");
                    x.Item().Text($"Khách hàng: {payment.MemberNameSnapshot ?? payment.Member?.User?.FullName}");
                    x.Item().Text($"Dịch vụ: {payment.PackageNameSnapshot ?? payment.Subscription?.Package?.PackageName}");
                    x.Item().Text($"Phương thức: {payment.PaymentMethod}");

                    x.Item().PaddingTop(15).LineHorizontal(1).LineColor(QuestPDF.Helpers.Colors.Grey.Lighten2);

                    // Tổng tiền
                    x.Item().PaddingTop(10).AlignRight().Text($"TỔNG TIỀN: {payment.AmountReceived:N0} VNĐ")
                        .FontSize(16).SemiBold().FontColor(QuestPDF.Helpers.Colors.Red.Medium);
                });

                // Lời cảm ơn ở cuối
                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Cảm ơn quý khách đã sử dụng dịch vụ tại Sports Center!");
                });
            });
        });

        // 3. Trả file PDF về cho người dùng tải xuống
        byte[] pdfBytes = document.GeneratePdf();
        return File(pdfBytes, "application/pdf", $"HoaDon_{payment.Id}.pdf");
    }
}