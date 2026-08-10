namespace TrendyolMiniApi.Services.Pdf
{
    // Hangi DTO'nun hangi View (Razor) ile çalışacağını C# derleyicisine mühürlüyoruz.
    public interface IPdfTemplate<T>
    {
        string ViewName { get; }
    }
}