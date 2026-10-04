using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace SportsCenterAPI.Models;

[Index(nameof(TokenHash), IsUnique = true)]
public class RefreshToken
{
    public int Id { get; set; }
    public int UserId { get; set; }

    // Only the SHA-256 hash is persisted; the client receives the random secret.
    [MaxLength(64)]
    public string TokenHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }

    // SQL Server rejects a refresh if another request already rotated/revoked it.
    [Timestamp]
    public byte[] RowVersion { get; set; } = null!;

    public User User { get; set; } = null!;
}
