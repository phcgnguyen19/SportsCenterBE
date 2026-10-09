using System.ComponentModel.DataAnnotations;

namespace SportsCenterAPI.Models;

public class Payment
{
    // Thông tin thanh toán
    public int Id { get; set; }

    public decimal Amount { get; set; }

    public decimal AmountReceived { get; set; }

    // Cash, CreditCard, BankTransfer, PayOS
    public string PaymentMethod { get; set; } = "Cash";

    // Pending, Completed, Cancelled, Expired, Failed, NeedsReview, Refunded
    public string Status { get; set; } = "Pending";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

    // Hội viên và gói đăng ký
    public int MemberId { get; set; }

    public Member Member { get; set; } = null!;

    public int? SubscriptionId { get; set; }

    public MemberSubscription? Subscription { get; set; }

    // Người tạo yêu cầu thanh toán
    public int? CreatedByUserId { get; set; }

    public User? CreatedByUser { get; set; }

    // Mã biên lai của trung tâm, ví dụ INV-...
    public string? TransactionReference { get; set; }

    // Thông tin kết nối payOS
    public long? PayOSOrderCode { get; set; }

    public string? PayOSPaymentLinkId { get; set; }

    public string? GatewayStatus { get; set; }

    public string? GatewayTransactionReference { get; set; }

    public string? CheckoutUrl { get; set; }

    public string? QrCode { get; set; }

    public string? ReturnUrl { get; set; }

    public string? CancelUrl { get; set; }

    public DateTime? ExpiresAt { get; set; }

    // Lưu tên tại thời điểm lập giao dịch,
    // tránh biên lai cũ đổi theo thông tin hồ sơ hiện tại.
    public string? MemberNameSnapshot { get; set; }

    public string? PackageNameSnapshot { get; set; }

    // Lý do giao dịch cần kiểm tra thủ công
    public string? ReviewReason { get; set; }
}