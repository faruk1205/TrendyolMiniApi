namespace TrendyolMiniApi.Entities
{
    public interface ISoftDeletable
    {
        bool IsDeleted { get; set; }
    }
}