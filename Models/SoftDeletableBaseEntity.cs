namespace TrendyolMiniApi.Models
{
    public abstract class SoftDeleteBaseEntity
    {
        public int Id { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; } = false;

    }
}