using SportsCenterAPI.Models;
using SportsCenterAPI.Models.DTOs.Payments;
using PayOS.Models.Webhooks;

namespace SportsCenterAPI.Services.Interface;

public class PayOSPaymentLinkResult
{
    public string PaymentUrl { get; set; } = string.Empty;
    public string QrCode { get; set; } = string.Empty;
    public string PaymentLinkId { get; set; } = string.Empty;
}

public interface IPayOSService
{
    Task<string> CreatePaymentLinkAsync(Payment payment);

    Task<PayOSPaymentLinkResult> CreatePaymentLinkDetailsAsync(Payment payment);

    Task<PayOSPaymentLinkResult> GetPaymentLinkDetailsAsync( Payment payment);

    Task<PayOSPaymentLinkResult> CancelPaymentLinkAsync(Payment payment,string reason);
    Task VerifyWebhookAsync(Webhook dto);
}