namespace SportsCenterAPI.Models
{
    /// <summary>
    /// Subscription record associating a member with a membership package.
    /// Thực thể lưu thông tin đăng ký gói tập của hội viên.
    /// </summary>
    public class MemberSubscription
    {
        public int Id { get; set; }
        public int MemberId { get; set; }
        public Member Member { get; set; } = null!;
        public int PackageId { get; set; }
        public MembershipPackage Package { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; } = "Active"; // Active, Expired, Cancelled

        // Navigation properties
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
