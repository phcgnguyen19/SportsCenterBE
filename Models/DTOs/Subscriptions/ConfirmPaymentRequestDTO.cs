using System.ComponentModel.DataAnnotations;

namespace SportsCenterAPI.Models.DTOs.Subscriptions;

/// <summary>A staff receipt for a payment already received and verified at the counter.</summary>
public class ConfirmPaymentRequestDTO
{
    [Range(typeof(decimal), "0.01", "9999999999999999.99")]
    public decimal Amount { get; set; }

    [Required]
    [RegularExpression("^(Cash|BankTransfer|CreditCard)$")]
    public string PaymentMethod { get; set; } = string.Empty;

    /// <summary>Staff must explicitly confirm receipt; this API does not charge a card or bank.</summary>
    public bool PaymentReceived { get; set; }
}
