using SportsCenterAPI.Models.DTOs.Subscriptions;

namespace SportsCenterAPI.Services.Interface;

public interface ISubscriptionService
{
    Task<SubscriptionDTO> CreateAsync(int actorId, int? memberId, CreateSubscriptionRequestDTO request, CancellationToken cancellationToken = default);
    Task<List<SubscriptionDTO>> GetForMemberAsync(int actorId, int? memberId, CancellationToken cancellationToken = default);
    Task<SubscriptionDTO> GetByIdAsync(int actorId, int subscriptionId, CancellationToken cancellationToken = default);
    Task<SubscriptionDTO> CancelPendingAsync(int actorId, int subscriptionId, CancellationToken cancellationToken = default);
    Task<PaymentReceiptDTO> ConfirmPaymentAsync(int actorId, int subscriptionId, ConfirmPaymentRequestDTO request, CancellationToken cancellationToken = default);
}
