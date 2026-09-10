namespace SportsCenterAPI.Models
{
    /// <summary>
    /// Payment transaction entity for subscriptions and classes.
    /// Thực thể giao dịch thanh toán cho các gói tập hoặc lớp học.
    /// </summary>
    public class Payment
    {
        public int Id { get; set; }
        public int MemberId { get; set; }
        public Member Member { get; set; } = null!;
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
        public string PaymentMethod { get; set; } = "Cash"; // Cash, CreditCard, BankTransfer, VNPay, Momo
        public string Status { get; set; } = "Completed"; // Pending, Completed, Failed, Refunded
        public int? SubscriptionId { get; set; }
        public MemberSubscription? Subscription { get; set; }
        public string? TransactionReference { get; set; }
    }
}
