namespace TrendyolMiniApi.Services.Pdf
{
    public interface IPdfTemplate<T> where T : class
    {
        string ViewName { get; }
    }
}