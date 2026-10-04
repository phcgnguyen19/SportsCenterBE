using System.ComponentModel.DataAnnotations;

namespace SportsCenterAPI.Models.DTOs.Payments;

public class PaymentDTO
{
    public int Id { get; set; }

    public int MemberId { get; set; }

    public int? SubscriptionId { get; set; }

    public decimal Amount { get; set; }

    public decimal AmountReceived { get; set; }

    public string PaymentMethod { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    // Chỉ trả ngày thu tiền sau khi thanh toán thành công.
    public DateTime? PaidAt { get; set; }

    public string? TransactionReference { get; set; }

    public long? PayOSOrderCode { get; set; }

    public string? PayOSPaymentLinkId { get; set; }

    public string? GatewayStatus { get; set; }

    public string? CheckoutUrl { get; set; }

    public string? QrCode { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public string? ReviewReason { get; set; }
}

public class CreatePaymentDTO
{
    [Range(1, int.MaxValue)]
    public int SubscriptionId { get; set; }
}

public class CancelPaymentDTO
{
    [Required]
    [StringLength(200)]
    public string Reason { get; set; } = string.Empty;
}