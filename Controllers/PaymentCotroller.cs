using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayOS.Exceptions;
using PayOS.Models.Webhooks;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using SportsCenterAPI.Data;
using SportsCenterAPI.DTOs.Response;
using SportsCenterAPI.Helpers;
using SportsCenterAPI.Models.DTOs.Payments;
using SportsCenterAPI.Services.Interface;
using System.Globalization;
using System.Security.Claims;

namespace SportsCenterAPI.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize(Roles = "Member,Receptionist,Manager")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly AppDbContext _context;

    public PaymentsController(
        IPaymentService paymentService,
        AppDbContext context)
    {
        _paymentService = paymentService;
        _context = context;
    }

    // GET /api/payments
    // Hội viên chỉ xem các thanh toán của mình.
    /// <summary>
    /// Xem lịch sử thanh toán trong phạm vi được phép
    /// </summary>
    [HttpGet]
    [ProducesResponseType(
        typeof(ApiResponse<List<PaymentDTO>>),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var response = await _paymentService.GetAllAsync(ActorId());

        return StatusCode(response.StatusCode, response);
    }

    // GET /api/payments/{id}
    /// <summary>
    /// Xem chi tiết giao dịch thanh toán
    /// </summary>
    [HttpGet("{id:int:min(1)}")]
    [ProducesResponseType(
        typeof(ApiResponse<PaymentDTO>),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(int id)
    {
        var response = await _paymentService.GetByIdAsync(
            id,
            ActorId());

        return StatusCode(response.StatusCode, response);
    }

    // POST /api/payments
    // Tạo hoặc lấy lại yêu cầu thanh toán Pending.
    /// <summary>
    /// Tạo hoặc lấy lại yêu cầu thanh toán đang chờ xử lý
    /// </summary>
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

    // POST /api/payments/{id}/sync
    // Đồng bộ trạng thái thực tế từ payOS.
    /// <summary>
    /// Đồng bộ trạng thái thanh toán từ payOS
    /// </summary>
    [HttpPost("{id:int:min(1)}/sync")]
    [ProducesResponseType(
        typeof(ApiResponse<PaymentDTO>),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> Sync(int id)
    {
        var response = await _paymentService.SyncAsync(
            id,
            ActorId());

        return StatusCode(response.StatusCode, response);
    }

    // POST /api/payments/{id}/cancel
    /// <summary>
    /// Hủy yêu cầu thanh toán payOS
    /// </summary>
    [HttpPost("{id:int:min(1)}/cancel")]
    [ProducesResponseType(
        typeof(ApiResponse<PaymentDTO>),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> Cancel(
        int id,
        [FromBody] CancelPaymentDTO dto)
    {
        var response = await _paymentService.CancelAsync(
            id,
            dto.Reason,
            ActorId());

        return StatusCode(response.StatusCode, response);
    }

    // POST /api/payments/payos/webhook
    // Service xác minh chữ ký trước khi xử lý dữ liệu payOS.
    /// <summary>
    /// Nhận webhook payOS và xác minh chữ ký giao dịch
    /// </summary>
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

    /// <summary>
    /// Xuất biên lai PDF của giao dịch đã hoàn tất
    /// </summary>
    /// <remarks>Hội viên chỉ được tải biên lai của mình. Lễ tân và quản lý được tải biên lai của hội viên.</remarks>
    [HttpGet("{id:int:min(1)}/export-pdf")]
    [ProducesResponseType(
        typeof(FileContentResult),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportInvoicePdf(
        int id,
        CancellationToken ct)
    {
        int actorId = ActorId();

        // 1. Tìm giao dịch và kiểm tra chủ sở hữu.
        var query = _context.Payments
            .AsNoTracking()
            .Where(p => p.Id == id);

        if (User.IsInRole("Member"))
        {
            query = query.Where(p => p.Member.UserId == actorId);
        }

        var payment = await query
            .Include(p => p.Member)
                .ThenInclude(m => m.User)
            .Include(p => p.Subscription)
                .ThenInclude(s => s!.Package)
            .SingleOrDefaultAsync(ct);

        if (payment == null)
        {
            return NotFound(new
            {
                message =
                    "Không tìm thấy giao dịch bạn được phép truy cập."
            });
        }

        // 2. Chỉ xuất khi giao dịch đã hoàn tất.
        if (payment.Status != "Completed")
        {
            return BadRequest(new
            {
                message =
                    "Chỉ xuất biên lai cho giao dịch đã hoàn tất."
            });
        }

        // 3. Ưu tiên thông tin được lưu lúc thanh toán.
        string invoiceCode =
            payment.TransactionReference ?? $"INV-{payment.Id}";

        string memberName =
            payment.MemberNameSnapshot ??
            payment.Member.User.FullName;

        string packageName =
            payment.PackageNameSnapshot ??
            payment.Subscription?.Package?.PackageName ??
            "Gói tập";

        string paymentMethod = payment.PaymentMethod switch
        {
            "Cash" => "Tiền mặt",
            "BankTransfer" => "Chuyển khoản",
            "CreditCard" => "Thẻ tín dụng",
            "PayOS" => "payOS",
            _ => payment.PaymentMethod
        };

        var culture = CultureInfo.GetCultureInfo("vi-VN");

        string totalAmount =
            payment.AmountReceived.ToString("N0", culture) + " VNĐ";

        // 4. Tạo tài liệu PDF (đen trắng, khổ A4).
        const string ink = "#000000";
        const string muted = "#595959";
        const string line = "#D4D4D4";

        static string VnDate(DateTime utc, string format) =>
            new DateTimeOffset(DateTime.SpecifyKind(utc, DateTimeKind.Utc))
                .ToOffset(TimeSpan.FromHours(7))
                .ToString(format, CultureInfo.InvariantCulture);

        var subscription = payment.Subscription;
        string period = subscription?.StartDate != null && subscription.EndDate != null
            ? $"{VnDate(subscription.StartDate.Value, "dd/MM/yyyy")} – {VnDate(subscription.EndDate.Value, "dd/MM/yyyy")}"
            : subscription != null
                ? $"{subscription.DurationInDays} ngày"
                : "—";

        string email = payment.Member.User.Email;
        string phone = string.IsNullOrWhiteSpace(payment.Member.User.PhoneNumber)
            ? "—"
            : payment.Member.User.PhoneNumber!;

        var document = QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(QuestPDF.Helpers.PageSizes.A4);
                page.Margin(40);
                page.PageColor("#FFFFFF");
                page.DefaultTextStyle(style => style.FontSize(10).FontColor(ink));

                page.Header().Column(header =>
                {
                    header.Item().Row(row =>
                    {
                        row.RelativeItem().Column(brand =>
                        {
                            brand.Item().Text("SPORTS CENTER")
                                .FontSize(22).Bold().LetterSpacing(0.08f);
                            brand.Item().PaddingTop(3)
                                .Text("Hệ thống quản lý trung tâm thể thao")
                                .FontSize(9).FontColor(muted);
                        });

                        row.RelativeItem().AlignRight().Column(title =>
                        {
                            title.Item().AlignRight().Text("BIÊN LAI THANH TOÁN")
                                .FontSize(14).Bold();
                            title.Item().PaddingTop(3).AlignRight()
                                .Text($"Số: {invoiceCode}")
                                .FontSize(9).FontColor(muted);
                        });
                    });

                    header.Item().PaddingTop(14).LineHorizontal(2).LineColor(ink);
                });

                page.Content().PaddingTop(24).Column(content =>
                {
                    content.Spacing(22);

                    // Thông tin giao dịch.
                    content.Item().Row(row =>
                    {
                        void Meta(string label, string value) =>
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text(label.ToUpperInvariant())
                                    .FontSize(7.5f).Bold().FontColor(muted).LetterSpacing(0.06f);
                                c.Item().PaddingTop(4).Text(value).SemiBold();
                            });

                        Meta("Ngày thanh toán", $"{VnDate(payment.PaymentDate, "dd/MM/yyyy HH:mm")} (UTC+7)");
                        Meta("Phương thức", paymentMethod);
                        Meta("Mã giao dịch", $"#{payment.Id}");

                        row.ConstantItem(96).AlignRight().AlignMiddle()
                            .Border(1.5f).BorderColor(ink).PaddingVertical(5).PaddingHorizontal(8)
                            .AlignCenter().Text("ĐÃ THANH TOÁN")
                            .FontSize(8).Bold().LetterSpacing(0.06f);
                    });

                    // Thông tin hội viên.
                    content.Item().Column(customer =>
                    {
                        customer.Item().PaddingBottom(6)
                            .Text("THÔNG TIN HỘI VIÊN")
                            .FontSize(8).Bold().FontColor(muted).LetterSpacing(0.06f);

                        customer.Item().BorderTop(0.75f).BorderColor(line).PaddingTop(10).Row(row =>
                        {
                            row.RelativeItem(2).Column(c =>
                            {
                                c.Item().Text("Họ và tên").FontSize(8).FontColor(muted);
                                c.Item().PaddingTop(2).Text(memberName).FontSize(12).Bold();
                            });
                            row.RelativeItem(2).Column(c =>
                            {
                                c.Item().Text("Email").FontSize(8).FontColor(muted);
                                c.Item().PaddingTop(2).Text(email);
                            });
                            row.RelativeItem(1).Column(c =>
                            {
                                c.Item().Text("Điện thoại").FontSize(8).FontColor(muted);
                                c.Item().PaddingTop(2).Text(phone);
                            });
                        });
                    });

                    // Bảng dịch vụ.
                    content.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(34);
                            columns.RelativeColumn(4);
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(2.5f);
                        });

                        table.Header(header =>
                        {
                            IContainer Head(IContainer c) =>
                                c.Background(ink).PaddingVertical(8).PaddingHorizontal(10);

                            header.Cell().Element(Head).Text("STT")
                                .FontSize(8).Bold().FontColor("#FFFFFF");
                            header.Cell().Element(Head).Text("DỊCH VỤ / GÓI TẬP")
                                .FontSize(8).Bold().FontColor("#FFFFFF");
                            header.Cell().Element(Head).Text("THỜI HẠN")
                                .FontSize(8).Bold().FontColor("#FFFFFF");
                            header.Cell().Element(Head).AlignRight().Text("THÀNH TIỀN")
                                .FontSize(8).Bold().FontColor("#FFFFFF");
                        });

                        IContainer Body(IContainer c) =>
                            c.BorderBottom(0.75f).BorderColor(ink).PaddingVertical(12).PaddingHorizontal(10);

                        table.Cell().Element(Body).Text("1");
                        table.Cell().Element(Body).Text(packageName).SemiBold();
                        table.Cell().Element(Body).Text(period);
                        table.Cell().Element(Body).AlignRight().Text(totalAmount).SemiBold();
                    });

                    // Tổng tiền.
                    content.Item().AlignRight().Width(250).Column(total =>
                    {
                        total.Item().PaddingBottom(8).Row(row =>
                        {
                            row.RelativeItem().Text("Tạm tính").FontColor(muted);
                            row.AutoItem().Text(totalAmount);
                        });

                        total.Item().Border(1.5f).BorderColor(ink).Padding(12).Column(box =>
                        {
                            box.Item().Text("TỔNG TIỀN ĐÃ THANH TOÁN")
                                .FontSize(7.5f).Bold().FontColor(muted).LetterSpacing(0.06f);
                            box.Item().PaddingTop(4).AlignRight()
                                .Text(totalAmount).FontSize(20).Bold();
                        });
                    });
                });

                page.Footer().Column(footer =>
                {
                    footer.Item().LineHorizontal(0.75f).LineColor(line);
                    footer.Item().PaddingTop(10).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Cảm ơn bạn đã đồng hành cùng Sports Center.")
                                .FontSize(9).SemiBold();
                            c.Item().PaddingTop(2)
                                .Text("Vui lòng lưu biên lai để đối chiếu khi cần hỗ trợ.")
                                .FontSize(8).FontColor(muted);
                        });

                        row.AutoItem().AlignBottom()
                            .Text($"Xuất lúc {VnDate(DateTime.UtcNow, "dd/MM/yyyy HH:mm")}")
                            .FontSize(8).FontColor(muted);
                    });
                });
            });
        });

        // 5. Trả file để tải xuống hoặc in.
        byte[] pdfBytes = document.GeneratePdf();

        return File(
            pdfBytes,
            "application/pdf",
            $"BienLai_{payment.Id}.pdf");
    }

    // Lấy ID tài khoản từ JWT.
    private int ActorId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(value, out int actorId) || actorId <= 0)
        {
            throw new BusinessException(
                StatusCodes.Status401Unauthorized,
                "Thông tin đăng nhập không hợp lệ.");
        }

        return actorId;
    }
}