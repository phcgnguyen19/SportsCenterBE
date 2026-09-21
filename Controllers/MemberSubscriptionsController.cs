using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportsCenterAPI.Helpers;
using SportsCenterAPI.Models.DTOs.Response;
using SportsCenterAPI.Models.DTOs.Subscriptions;
using SportsCenterAPI.Services.Interface;

namespace SportsCenterAPI.Controllers;

[ApiController]
[Route("api/member-subscriptions")]
[Authorize]
public class MemberSubscriptionsController(ISubscriptionService subscriptions) : ControllerBase
{
    /// <summary>Member chooses a package. The subscription stays Pending until staff confirms payment.</summary>
    [HttpPost]
    [Authorize(Roles = "Member")]
    [ProducesResponseType(typeof(ApiResponse<SubscriptionDTO>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(CreateSubscriptionRequestDTO request, CancellationToken cancellationToken)
    {
        var result = await subscriptions.CreateAsync(ActorId(), null, request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            ApiResponse<SubscriptionDTO>.CreatedAt(result, "Subscription created; awaiting payment."));
    }

    /// <summary>Receptionist or manager registers a package on behalf of a member.</summary>
    [HttpPost("members/{memberId:int}")]
    [Authorize(Roles = "Receptionist,Manager")]
    [ProducesResponseType(typeof(ApiResponse<SubscriptionDTO>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateForMember(int memberId, CreateSubscriptionRequestDTO request, CancellationToken cancellationToken)
    {
        var result = await subscriptions.CreateAsync(ActorId(), memberId, request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            ApiResponse<SubscriptionDTO>.CreatedAt(result, "Subscription created; awaiting payment."));
    }

    [HttpGet("mine")]
    [Authorize(Roles = "Member")]
    [ProducesResponseType(typeof(ApiResponse<List<SubscriptionDTO>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken) =>
        Ok(ApiResponse<List<SubscriptionDTO>>.Ok(
            await subscriptions.GetForMemberAsync(ActorId(), null, cancellationToken), "Subscriptions retrieved."));

    [HttpGet("members/{memberId:int}")]
    [Authorize(Roles = "Receptionist,Manager")]
    [ProducesResponseType(typeof(ApiResponse<List<SubscriptionDTO>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForMember(int memberId, CancellationToken cancellationToken) =>
        Ok(ApiResponse<List<SubscriptionDTO>>.Ok(
            await subscriptions.GetForMemberAsync(ActorId(), memberId, cancellationToken), "Subscriptions retrieved."));

    [HttpGet("{id:int}")]
    [Authorize(Roles = "Member,Receptionist,Manager")]
    [ProducesResponseType(typeof(ApiResponse<SubscriptionDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<SubscriptionDTO>.Ok(
            await subscriptions.GetByIdAsync(ActorId(), id, cancellationToken), "Subscription retrieved."));

    /// <summary>Cancel only an unpaid Pending subscription. Paid packages require a separate refund workflow.</summary>
    [HttpPost("{id:int}/cancel")]
    [Authorize(Roles = "Member,Receptionist,Manager")]
    [ProducesResponseType(typeof(ApiResponse<SubscriptionDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CancelPending(int id, CancellationToken cancellationToken) =>
        Ok(ApiResponse<SubscriptionDTO>.Ok(
            await subscriptions.CancelPendingAsync(ActorId(), id, cancellationToken), "Pending subscription cancelled."));

    /// <summary>Staff records a verified full payment and activates the package atomically. Does not charge a bank/card.</summary>
    [HttpPost("{id:int}/payments")]
    [Authorize(Roles = "Receptionist,Manager")]
    [ProducesResponseType(typeof(ApiResponse<PaymentReceiptDTO>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ConfirmPayment(int id, ConfirmPaymentRequestDTO request, CancellationToken cancellationToken)
    {
        var receipt = await subscriptions.ConfirmPaymentAsync(ActorId(), id, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created,
            ApiResponse<PaymentReceiptDTO>.CreatedAt(receipt, "Payment confirmed and subscription activated."));
    }

    private int ActorId() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
        ? id
        : throw new BusinessException(401, "A valid user identifier is required.");
}
