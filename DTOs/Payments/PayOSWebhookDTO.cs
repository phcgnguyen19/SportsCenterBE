using SportsCenterAPI.Models.DTOs.Payments;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SportsCenterAPI.DTOs.Payments;

public class PayOSWebhookDTO
{
    [Required]
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("desc")]
    public string Desc { get; set; } = string.Empty;

    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [Required]
    [JsonPropertyName("data")]
    public PayOSWebhookData Data { get; set; } = null!;

    [Required]
    [JsonPropertyName("signature")]
    public string Signature { get; set; } = string.Empty;
}