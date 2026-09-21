using System.ComponentModel.DataAnnotations;

namespace SportsCenterAPI.Models.DTOs.Subscriptions;

public class CreateSubscriptionRequestDTO
{
    [Range(1, int.MaxValue)]
    public int PackageId { get; set; }
}
