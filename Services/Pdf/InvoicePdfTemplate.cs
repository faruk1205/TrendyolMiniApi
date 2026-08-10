using TrendyolMiniApi.DTOs;
using TrendyolMiniApi.Markers;

// DİKKAT: .Templates kısmı silindi, klasör yapısıyla aynı hale getirildi.
namespace TrendyolMiniApi.Services.Pdf 
{
    // ISingletonService etiketi eklendi!
    public class InvoicePdfTemplate : IPdfTemplate<InvoiceDto>, ISingletonService
    {
        public string ViewName => "~/Services/Pdf/Templates/InvoiceTemplate.cshtml";
    }
}