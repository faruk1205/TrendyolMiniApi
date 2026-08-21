namespace TrendyolMiniApi.Attributes
{
    // Bu attribute'ü taşıyan endpoint'ler, Idempotency-Key header'ı zorunlu kılar.
    [AttributeUsage(AttributeTargets.Method)]
    public class IdempotentAttribute : Attribute
    {
    }
}