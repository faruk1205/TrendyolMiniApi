using PuppeteerSharp;
using PuppeteerSharp.Media;
using TrendyolMiniApi.DTOs;
using TrendyolMiniApi.Markers;

namespace TrendyolMiniApi.Services.Pdf
{
    public interface IPdfService : IScopedService
    {
        // Burada da 'where T : class' kuralı olmak zorunda!
        Task<byte[]> GenerateAsync<T>(T model, IPdfTemplate<T> template) where T : class;
    }

    public class PdfService : IPdfService
    {
        private readonly IRazorViewRenderer _renderer;
        private readonly PuppeteerBrowserProvider _browserProvider;

        public PdfService(IRazorViewRenderer renderer, PuppeteerBrowserProvider browserProvider)
        {
            _renderer = renderer;
            _browserProvider = browserProvider; // Singleton olan tarayıcıyı buraya aldık
        }

        // Arayüz ile bu imza yüzde yüz örtüşmelidir
        public async Task<byte[]> GenerateAsync<T>(T model, IPdfTemplate<T> template) where T : class        {
            // 1. DTO ve Şablonu birleştirip HTML üret (Scoped)
            
            var html = await _renderer.RenderToStringAsync(template.ViewName, model);

            // 2. Hazırda bekleyen Chrome'u al (Singleton)
            var browser = await _browserProvider.GetBrowserAsync();
            
            // 3. Sadece yeni bir sekme (Page) aç
            await using var page = await browser.NewPageAsync();
            await page.SetContentAsync(html);

            // 4. Sekmeyi PDF'e bas ve sekmeyi kapat (using bloğu sayesinde)
            return await page.PdfDataAsync(new PdfOptions
            {
                Format = PaperFormat.A5,
                PrintBackground = true,
                MarginOptions = new MarginOptions { Top = "1cm", Bottom = "1cm", Left = "1cm", Right = "1cm" }
            });
        }
    }
}