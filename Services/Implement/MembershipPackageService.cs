using System.Linq.Expressions;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SportsCenterAPI.Data;
using SportsCenterAPI.Helpers;
using SportsCenterAPI.Models;
using SportsCenterAPI.Models.DTOs.MembershipPackages;
using SportsCenterAPI.Services.Interface;

namespace SportsCenterAPI.Services.Implement;

public class MembershipPackageService(AppDbContext context) : IMembershipPackageService
{
    private static readonly Expression<Func<MembershipPackage, MembershipPackageDTO>> Projection = package =>
        new MembershipPackageDTO
        {
            Id = package.Id,
            PackageName = package.PackageName,
            Description = package.Description,
            Price = package.Price,
            DurationInDays = package.DurationInDays,
            IsActive = package.IsActive
        };

    public async Task<IReadOnlyList<MembershipPackageDTO>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await context.MembershipPackages.AsNoTracking()
            .Where(package => package.IsActive)
            .OrderBy(package => package.Id)
            .Select(Projection)
            .ToListAsync(cancellationToken);
    }

    public async Task<MembershipPackageDTO> GetActiveByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await context.MembershipPackages.AsNoTracking()
            .Where(package => package.Id == id && package.IsActive)
            .Select(Projection)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new BusinessException(404, "Membership package not found.");
    }

    public async Task<IReadOnlyList<MembershipPackageDTO>> GetForManagementAsync(int actorId, CancellationToken cancellationToken = default)
    {
        await EnsureManagerAsync(actorId, cancellationToken);
        return await context.MembershipPackages.AsNoTracking()
            .OrderBy(package => package.Id)
            .Select(Projection)
            .ToListAsync(cancellationToken);
    }

    public async Task<MembershipPackageDTO> GetForManagementByIdAsync(int id, int actorId, CancellationToken cancellationToken = default)
    {
        await EnsureManagerAsync(actorId, cancellationToken);
        return await context.MembershipPackages.AsNoTracking()
            .Where(package => package.Id == id)
            .Select(Projection)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new BusinessException(404, "Membership package not found.");
    }

    public async Task<MembershipPackageDTO> CreateAsync(MembershipPackageRequestDTO request, int actorId, CancellationToken cancellationToken = default)
    {
        await EnsureManagerAsync(actorId, cancellationToken);
        Validate(request);

        var package = new MembershipPackage
        {
            PackageName = request.PackageName.Trim(),
            Description = NormalizeDescription(request.Description),
            Price = request.Price,
            DurationInDays = request.DurationInDays,
            IsActive = true
        };

        context.MembershipPackages.Add(package);
        // The generated package ID is not available before saving. The audit payload
        // captures its identifying fields so both records can be saved atomically.
        context.AuditLogs.Add(new AuditLog
        {
            UserId = actorId,
            Action = "Create",
            EntityName = nameof(MembershipPackage),
            Details = JsonSerializer.Serialize(new
            {
                package.PackageName,
                package.Description,
                package.Price,
                package.DurationInDays,
                package.IsActive
            })
        });
        await context.SaveChangesAsync(cancellationToken);
        return ToDTO(package);
    }

    public async Task<MembershipPackageDTO> UpdateAsync(int id, UpdateMembershipPackageRequestDTO request, int actorId, CancellationToken cancellationToken = default)
    {
        await EnsureManagerAsync(actorId, cancellationToken);
        Validate(request);
        if (!request.IsActive.HasValue)
            throw new BusinessException(400, "IsActive is required when updating a membership package.");

        var package = await FindAsync(id, cancellationToken);
        var before = ToDTO(package);
        package.PackageName = request.PackageName.Trim();
        package.Description = NormalizeDescription(request.Description);
        package.Price = request.Price;
        package.DurationInDays = request.DurationInDays;
        package.IsActive = request.IsActive.Value;

        context.AuditLogs.Add(new AuditLog
        {
            UserId = actorId,
            Action = "Update",
            EntityName = nameof(MembershipPackage),
            EntityId = package.Id,
            Details = JsonSerializer.Serialize(new { Before = before, After = ToDTO(package) })
        });
        await context.SaveChangesAsync(cancellationToken);
        return ToDTO(package);
    }

    public async Task DeactivateAsync(int id, int actorId, CancellationToken cancellationToken = default)
    {
        await EnsureManagerAsync(actorId, cancellationToken);
        var package = await FindAsync(id, cancellationToken);
        if (!package.IsActive)
            return;

        package.IsActive = false;
        context.AuditLogs.Add(new AuditLog
        {
            UserId = actorId,
            Action = "Delete",
            EntityName = nameof(MembershipPackage),
            EntityId = package.Id,
            Details = JsonSerializer.Serialize(new { package.PackageName, IsActive = false, SoftDelete = true })
        });
        // Preserve the package row and existing subscriptions/payment history.
        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task<MembershipPackage> FindAsync(int id, CancellationToken cancellationToken)
    {
        return await context.MembershipPackages.SingleOrDefaultAsync(package => package.Id == id, cancellationToken)
            ?? throw new BusinessException(404, "Membership package not found.");
    }

    private async Task EnsureManagerAsync(int actorId, CancellationToken cancellationToken)
    {
        if (!await context.Users.AnyAsync(user => user.Id == actorId && user.IsActive && user.Role == "Manager", cancellationToken))
            throw new BusinessException(403, "An active Manager account is required.");
    }

    private static void Validate(MembershipPackageRequestDTO request)
    {
        if (string.IsNullOrWhiteSpace(request.PackageName) || request.PackageName.Trim().Length > 200)
            throw new BusinessException(400, "Package name is required and must not exceed 200 characters.");
        if (request.Description?.Length > 2000)
            throw new BusinessException(400, "Description must not exceed 2000 characters.");
        if (request.Price <= 0 || request.Price > 9999999999999999.99m || decimal.Round(request.Price, 2) != request.Price)
            throw new BusinessException(400, "Price must be positive, fit decimal(18,2), and have at most two decimal places.");
        if (request.DurationInDays is <= 0 or > 36500)
            throw new BusinessException(400, "Duration must be between 1 and 36500 days (100 years).");
    }

    private static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    private static MembershipPackageDTO ToDTO(MembershipPackage package) => new()
    {
        Id = package.Id,
        PackageName = package.PackageName,
        Description = package.Description,
        Price = package.Price,
        DurationInDays = package.DurationInDays,
        IsActive = package.IsActive
    };
}
