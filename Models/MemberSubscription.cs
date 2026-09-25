using System.ComponentModel.DataAnnotations;

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
        // Purchase terms are fixed when the member chooses a package.
        public decimal AgreedPrice { get; set; }
        public int DurationInDays { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Active, Expired, Cancelled

        [Timestamp]
        public byte[] RowVersion { get; set; } = [];

        // Navigation properties
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
