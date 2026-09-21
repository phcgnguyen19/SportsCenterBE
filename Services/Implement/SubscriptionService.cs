using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SportsCenterAPI.Data;
using SportsCenterAPI.Helpers;
using SportsCenterAPI.Models;
using SportsCenterAPI.Models.DTOs.Subscriptions;
using SportsCenterAPI.Services.Interface;

namespace SportsCenterAPI.Services.Implement;

public class SubscriptionService(AppDbContext context) : ISubscriptionService
{
    public async Task<SubscriptionDTO> CreateAsync(int actorId, int? memberId,
        CreateSubscriptionRequestDTO request, CancellationToken cancellationToken = default)
    {
        return await InTransactionAsync(async () =>
        {
            var member = await ResolveMemberAsync(actorId, memberId, cancellationToken);
            var package = await context.MembershipPackages.SingleOrDefaultAsync(
                p => p.Id == request.PackageId, cancellationToken)
                ?? throw new BusinessException(404, "Membership package was not found.");
            if (!package.IsActive)
                throw new BusinessException(409, "This membership package is no longer available.");
            if (package.Price <= 0 || decimal.Round(package.Price, 2) != package.Price ||
                package.DurationInDays is <= 0 or > 36500)
                throw new BusinessException(409, "The package has invalid price or duration settings.");

            var now = DateTime.UtcNow;
            var alreadyRegistered = await context.MemberSubscriptions.AnyAsync(s =>
                s.MemberId == member.Id && s.PackageId == package.Id &&
                (s.Status == "Pending" || (s.Status == "Active" && (s.EndDate == null || s.EndDate > now))),
                cancellationToken);
            if (alreadyRegistered)
                throw new BusinessException(409, "The member already has a pending or active subscription to this package.");

            var subscription = new MemberSubscription
            {
                MemberId = member.Id,
                Package = package,
                AgreedPrice = package.Price,
                DurationInDays = package.DurationInDays,
                CreatedAt = now,
                Status = "Pending"
            };
            context.MemberSubscriptions.Add(subscription);
            await context.SaveChangesAsync(cancellationToken);
            context.AuditLogs.Add(new AuditLog
            {
                UserId = actorId,
                Action = "Create",
                EntityName = nameof(MemberSubscription),
                EntityId = subscription.Id,
                Details = $"Created Pending subscription for member {member.Id}, package {package.Id}."
            });
            await context.SaveChangesAsync(cancellationToken);
            return ToDto(subscription, now);
        }, cancellationToken);
    }

    public async Task<List<SubscriptionDTO>> GetForMemberAsync(int actorId, int? memberId,
        CancellationToken cancellationToken = default)
    {
        var member = await ResolveMemberAsync(actorId, memberId, cancellationToken, requireActive: false);
        var subscriptions = await ReadSubscriptions()
            .Where(s => s.MemberId == member.Id)
            .OrderByDescending(s => s.CreatedAt).ThenByDescending(s => s.Id)
            .ToListAsync(cancellationToken);
        var now = DateTime.UtcNow;
        return subscriptions.Select(s => ToDto(s, now)).ToList();
    }

    public async Task<SubscriptionDTO> GetByIdAsync(int actorId, int subscriptionId,
        CancellationToken cancellationToken = default)
    {
        var actor = await GetActorAsync(actorId, cancellationToken);
        var query = ReadSubscriptions().Where(s => s.Id == subscriptionId);
        if (!IsStaff(actor))
        {
            if (actor.Role != "Member")
                throw new BusinessException(403, "Only members and reception staff may access subscriptions.");
            query = query.Where(s => s.Member.UserId == actorId);
        }
        var subscription = await query.SingleOrDefaultAsync(cancellationToken)
            ?? throw new BusinessException(404, "Subscription was not found.");
        return ToDto(subscription, DateTime.UtcNow);
    }

    public async Task<PaymentReceiptDTO> ConfirmPaymentAsync(int actorId, int subscriptionId,
        ConfirmPaymentRequestDTO request, CancellationToken cancellationToken = default)
    {
        return await InTransactionAsync(async () =>
        {
            var actor = await GetActorAsync(actorId, cancellationToken);
            if (!IsStaff(actor))
                throw new BusinessException(403, "Only reception staff or a manager can confirm a payment.");
            if (!request.PaymentReceived)
                throw new BusinessException(400, "Confirm that the payment has been received before issuing a receipt.");
            if (request.PaymentMethod is not ("Cash" or "BankTransfer" or "CreditCard"))
                throw new BusinessException(400, "Payment method must be Cash, BankTransfer or CreditCard.");
            if (request.Amount <= 0 || request.Amount > 9999999999999999.99m ||
                decimal.Round(request.Amount, 2) != request.Amount)
                throw new BusinessException(400, "Payment amount must be positive and have at most two decimal places.");

            var subscription = await context.MemberSubscriptions
                .Include(s => s.Member).ThenInclude(m => m.User)
                .SingleOrDefaultAsync(s => s.Id == subscriptionId, cancellationToken)
                ?? throw new BusinessException(404, "Subscription was not found.");
            if (!subscription.Member.User.IsActive)
                throw new BusinessException(409, "The member account is inactive.");
            if (subscription.Status != "Pending")
                throw new BusinessException(409, "Only a Pending subscription can be paid and activated.");
            if (await context.Payments.AnyAsync(p => p.SubscriptionId == subscriptionId && p.Status == "Completed", cancellationToken))
                throw new BusinessException(409, "This subscription has already been paid.");
            if (subscription.AgreedPrice <= 0 || request.Amount != subscription.AgreedPrice)
                throw new BusinessException(400, "Payment must equal the full agreed subscription price.");
            if (subscription.DurationInDays is <= 0 or > 36500)
                throw new BusinessException(409, "The subscription has invalid duration settings.");

            var now = DateTime.UtcNow;
            var payment = new Payment
            {
                MemberId = subscription.MemberId,
                SubscriptionId = subscription.Id,
                Amount = request.Amount,
                PaymentMethod = request.PaymentMethod,
                Status = "Completed",
                PaymentDate = now,
                TransactionReference = $"INV-{now:yyyyMMdd}-{Guid.NewGuid():N}"
            };
            context.Payments.Add(payment);
            subscription.StartDate = now;
            subscription.EndDate = now.AddDays(subscription.DurationInDays);
            subscription.Status = "Active";
            await context.SaveChangesAsync(cancellationToken);
            context.AuditLogs.AddRange(
                new AuditLog
                {
                    UserId = actorId,
                    Action = "Create",
                    EntityName = nameof(Payment),
                    EntityId = payment.Id,
                    Details = $"Confirmed {payment.PaymentMethod} receipt {payment.TransactionReference} for subscription {subscription.Id}."
                },
                new AuditLog
                {
                    UserId = actorId,
                    Action = "Update",
                    EntityName = nameof(MemberSubscription),
                    EntityId = subscription.Id,
                    Details = "Pending -> Active after full payment confirmation."
                });
            await context.SaveChangesAsync(cancellationToken);
            return ToReceipt(payment);
        }, cancellationToken);
    }

    public async Task<SubscriptionDTO> CancelPendingAsync(int actorId, int subscriptionId,
        CancellationToken cancellationToken = default)
    {
        return await InTransactionAsync(async () =>
        {
            var actor = await GetActorAsync(actorId, cancellationToken);
            var query = context.MemberSubscriptions.Include(s => s.Package).Include(s => s.Payments)
                .Where(s => s.Id == subscriptionId);
            if (!IsStaff(actor))
            {
                if (actor.Role != "Member")
                    throw new BusinessException(403, "Only members and reception staff may cancel pending subscriptions.");
                query = query.Where(s => s.Member.UserId == actorId);
            }
            var subscription = await query.SingleOrDefaultAsync(cancellationToken)
                ?? throw new BusinessException(404, "Subscription was not found.");
            if (subscription.Status != "Pending" || subscription.Payments.Any(p => p.Status == "Completed"))
                throw new BusinessException(409, "Only an unpaid Pending subscription can be cancelled.");
            subscription.Status = "Cancelled";
            context.AuditLogs.Add(new AuditLog
            {
                UserId = actorId,
                Action = "Update",
                EntityName = nameof(MemberSubscription),
                EntityId = subscription.Id,
                Details = "Pending -> Cancelled before payment."
            });
            await context.SaveChangesAsync(cancellationToken);
            return ToDto(subscription, DateTime.UtcNow);
        }, cancellationToken);
    }

    private IQueryable<MemberSubscription> ReadSubscriptions() => context.MemberSubscriptions
        .AsNoTracking().Include(s => s.Package).Include(s => s.Payments);

    private async Task<User> GetActorAsync(int actorId, CancellationToken cancellationToken)
    {
        return await context.Users.SingleOrDefaultAsync(u => u.Id == actorId && u.IsActive, cancellationToken)
            ?? throw new BusinessException(401, "The account is unavailable or inactive. Please sign in again.");
    }

    private static bool IsStaff(User user) => user.Role is "Manager" or "Receptionist";

    private async Task<Member> ResolveMemberAsync(int actorId, int? memberId, CancellationToken cancellationToken,
        bool requireActive = true)
    {
        var actor = await GetActorAsync(actorId, cancellationToken);
        IQueryable<Member> members = context.Members.Include(m => m.User);
        if (memberId.HasValue)
        {
            if (!IsStaff(actor))
                throw new BusinessException(403, "Only reception staff or a manager may select another member.");
            members = members.Where(m => m.Id == memberId.Value);
        }
        else
        {
            if (actor.Role != "Member")
                throw new BusinessException(403, "This endpoint is for members. Staff must select a member explicitly.");
            members = members.Where(m => m.UserId == actorId);
        }
        var member = await members.SingleOrDefaultAsync(cancellationToken)
            ?? throw new BusinessException(404, "Member profile was not found.");
        if (requireActive && !member.User.IsActive)
            throw new BusinessException(409, "The member account is inactive.");
        return member;
    }

    private async Task<T> InTransactionAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken)
    {
        // Range locks prevent two requests from both passing the duplicate-purchase check.
        // RowVersion and the unique completed-payment index provide additional safeguards.
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            var result = await action();
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new BusinessException(409, "The subscription changed in another request. Refresh before trying again.");
        }
        catch (Exception exception) when (IsSqlConcurrencyConflict(exception))
        {
            throw new BusinessException(409, "Another request changed this subscription. Refresh before trying again.");
        }
        // On validation, database or cancellation failure, disposal rolls back the transaction.
    }

    private static bool IsSqlConcurrencyConflict(Exception exception)
    {
        for (Exception? current = exception; current != null; current = current.InnerException)
        {
            if (current is SqlException sql && sql.Number is 1205 or 2601 or 2627)
                return true;
        }
        return false;
    }

    private static SubscriptionDTO ToDto(MemberSubscription subscription, DateTime now) => new()
    {
        Id = subscription.Id,
        MemberId = subscription.MemberId,
        PackageId = subscription.PackageId,
        PackageName = subscription.Package.PackageName,
        AgreedPrice = subscription.AgreedPrice,
        DurationInDays = subscription.DurationInDays,
        Status = subscription.Status == "Active" && subscription.EndDate <= now ? "Expired" : subscription.Status,
        CreatedAt = subscription.CreatedAt,
        StartDate = subscription.StartDate,
        EndDate = subscription.EndDate,
        Payments = subscription.Payments.OrderByDescending(p => p.PaymentDate).Select(ToReceipt).ToList()
    };

    private static PaymentReceiptDTO ToReceipt(Payment payment) => new()
    {
        Id = payment.Id,
        SubscriptionId = payment.SubscriptionId,
        Amount = payment.Amount,
        PaymentMethod = payment.PaymentMethod,
        Status = payment.Status,
        PaymentDate = payment.PaymentDate,
        TransactionReference = payment.TransactionReference
    };
}
