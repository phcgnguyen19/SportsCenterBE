using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SportsCenterAPI.Controllers;
using SportsCenterAPI.Data;
using SportsCenterAPI.Models;
using SportsCenterAPI.Services.Interface;

namespace SportsCenterAPI.Tests;

public class PaymentReceiptTests
{
    static PaymentReceiptTests() =>
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

    private static PaymentsController CreateController(AppDbContext db, int userId, string role) =>
        new(Mock.Of<IPaymentService>(), db)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                        new Claim(ClaimTypes.Role, role)
                    ], "test"))
                }
            }
        };

    private static async Task<AppDbContext> SeedAsync()
    {
        var db = TestDbHelper.CreateDbContext();
        db.Users.AddRange(
            new User { Id = 1, Email = "a@t.com", FullName = "Nguyễn Văn Ánh", Role = "Member", PasswordHash = "h" },
            new User { Id = 2, Email = "b@t.com", FullName = "Khách Khác", Role = "Member", PasswordHash = "h" },
            new User { Id = 3, Email = "r@t.com", FullName = "Lễ Tân", Role = "Receptionist", PasswordHash = "h" });
        db.Members.AddRange(new Member { Id = 1, UserId = 1 }, new Member { Id = 2, UserId = 2 });
        db.Payments.AddRange(
            new Payment
            {
                Id = 1, MemberId = 1, Amount = 1_500_000m, AmountReceived = 1_500_000m, PaymentMethod = "PayOS",
                Status = "Completed", TransactionReference = "INV-20261028-00000001",
                PaymentDate = new DateTime(2026, 10, 28, 17, 30, 0, DateTimeKind.Utc),
                MemberNameSnapshot = "Nguyễn Văn Ánh", PackageNameSnapshot = "Gói Gold 3 tháng"
            },
            new Payment { Id = 2, MemberId = 1, Amount = 1m, Status = "Pending" });
        await db.SaveChangesAsync();
        return db;
    }

    [Fact]
    public async Task Export_Completed_OwnerGetsPdf()
    {
        using var db = await SeedAsync();

        var result = await CreateController(db, 1, "Member").ExportInvoicePdf(1, default);

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/pdf", file.ContentType);
        Assert.Equal("BienLai_1.pdf", file.FileDownloadName);
        Assert.StartsWith("%PDF", System.Text.Encoding.ASCII.GetString(file.FileContents, 0, 4));

        // Cho phép xem tận mắt: đặt RECEIPT_OUT=<đường dẫn .pdf> khi chạy test.
        var outPath = Environment.GetEnvironmentVariable("RECEIPT_OUT");
        if (!string.IsNullOrEmpty(outPath)) File.WriteAllBytes(outPath, file.FileContents);
    }

    [Fact]
    public async Task Export_OtherMember_GetsNotFound()
    {
        using var db = await SeedAsync();

        var result = await CreateController(db, 2, "Member").ExportInvoicePdf(1, default);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Export_Receptionist_CanExportMemberReceipt()
    {
        using var db = await SeedAsync();

        var result = await CreateController(db, 3, "Receptionist").ExportInvoicePdf(1, default);

        Assert.IsType<FileContentResult>(result);
    }

    [Fact]
    public async Task Export_PendingPayment_GetsBadRequest()
    {
        using var db = await SeedAsync();

        var result = await CreateController(db, 1, "Member").ExportInvoicePdf(2, default);

        Assert.IsType<BadRequestObjectResult>(result);
    }
}
