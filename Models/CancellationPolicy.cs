using System.ComponentModel.DataAnnotations;

namespace SportsCenterAPI.Models;

public class CancellationPolicy
{
    public int Id { get; set; }
    [MaxLength(120)] public string Name { get; set; } = string.Empty;
    public int MinimumHoursBeforeStart { get; set; } = 2;
    public bool IsActive { get; set; } = true;
}
