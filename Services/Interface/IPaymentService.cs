using PayOS.Models.Webhooks;
using SportsCenterAPI.DTOs.Payments;
using SportsCenterAPI.DTOs.Response;
using SportsCenterAPI.Models.DTOs.Payments;

namespace SportsCenterAPI.Services.Interface;

public interface IPaymentService
{
    Task PayOSWebhookAsync(Webhook dto);

    Task<ApiResponse<List<PaymentDTO>>> GetAllAsync(
        int actorId);

    Task<ApiResponse<PaymentDTO>> GetByIdAsync(
        int id,
        int actorId);

    Task<ApiResponse<PaymentDTO>> CreateAsync(
        CreatePaymentDTO dto,
        int actorId);

    Task<ApiResponse<PaymentDTO>> CancelAsync(
        int id,
        string reason,
        int actorId);

    Task<ApiResponse<PaymentDTO>> SyncAsync(
        int id,
        int actorId);
}