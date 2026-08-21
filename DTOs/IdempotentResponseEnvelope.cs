namespace TrendyolMiniApi.DTOs
{
    // Redis'e serialize edilip saklanan, daha önce üretilmiş cevabın tam kopyası.
    public class IdempotentResponseEnvelope
    {
        public int StatusCode { get; set; }
        public object? Body { get; set; } //object: İçerisine her türlü veri tipini alabilmesi için en temel veri tipi (object) seçilmiş. Çünkü API bazen bir ürün listesi dönerken bazen sadece bir hata mesajı string'i dönebilir.

        public IdempotentResponseEnvelope() { } //boş constructer  Neden gerekli? Bu veri Redis'ten okunurken JSON'dan tekrar bu C# nesnesine dönüştürülürken (Deserialization işlemi sırasında) System.Text.Json veya Newtonsoft.Json gibi kütüphaneler genellikle bu boş yapıcı metoda ihtiyaç duyar.

        public IdempotentResponseEnvelope(int statusCode, object? body)
        {
            StatusCode = statusCode;
            Body = body;
        }
    }
}