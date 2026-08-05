using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using TrendyolMiniApi.Data;
using TrendyolMiniApi.Models;

namespace TrendyolMiniApi.Services
{
    public class ExcelService : IExcelService
    {
        private readonly ApplicationDbContext _context;

        public ExcelService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int> ImportProductsAsync(IFormFile file, int sellerId)
        {
            // 1. Güvenlik ve Format Kontrolü
            if (file == null || file.Length == 0)
                throw new ArgumentException("Lütfen geçerli bir dosya yükleyin.");

            if (!file.FileName.EndsWith(".xlsx"))
                throw new ArgumentException("Sadece .xlsx uzantılı Excel dosyaları desteklenmektedir.");

            var productsToAdd = new List<Product>();

            // 2. Dosyayı RAM'e (Stream) Alıp Okuma İşlemi
            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);

                using (var workbook = new XLWorkbook(stream))
                {
                    // İlk sayfayı al (Worksheet)
                    var worksheet = workbook.Worksheet(1);
                    
                    // Boş satırları atlayıp dolu olanları al. Skip(1) ile başlık satırını atlıyoruz.
                    var rows = worksheet.RangeUsed().RowsUsed().Skip(1);

                    int rowIndex = 2; // Başlık 1 olduğu için 2'den başlıyoruz (Hata mesajları için)
                    var errorList = new List<string>();

                    foreach (var row in rows)
                    {
                        try
                        {
                            // 3. Hücreleri Okuma ve DTO'ya Çevirme (Manuel Validasyon Örneği)
                            var name = row.Cell(1).GetValue<string>();
                            var price = row.Cell(2).GetValue<decimal>();
                            var stock = row.Cell(3).GetValue<int>();
                            var categoryId = row.Cell(4).GetValue<int>();

                            // İş Kuralları (Business Rules)
                            if (string.IsNullOrWhiteSpace(name)) errorList.Add($"Satır {rowIndex}: Ürün adı boş olamaz.");
                            if (price <= 0) errorList.Add($"Satır {rowIndex}: Fiyat sıfırdan büyük olmalıdır.");
                            if (stock < 0) errorList.Add($"Satır {rowIndex}: Stok eksi olamaz.");

                            // Sorun yoksa listeye ekle
                            productsToAdd.Add(new Product
                            {
                                Name = name,
                                Price = price,
                                Stock = stock,
                                CategoryId = categoryId,
                                SellerId = sellerId // Ürünleri yükleyen satıcının ID'si
                            });
                        }
                        catch (Exception)
                        {
                            errorList.Add($"Satır {rowIndex}: Veri formatı hatalı. Lütfen sayısal alanları kontrol edin.");
                        }

                        rowIndex++;
                    }

                    // 4. Hata Varsa İşlemi Durdur ve Bildir
                    if (errorList.Any())
                    {
                        // Hataları alt alta birleştirip fırlatıyoruz
                        throw new InvalidOperationException("Excel dosyasında hatalar bulundu:\n" + string.Join("\n", errorList));
                    }
                }
            }

            // 5. Her şey kolaysa Toplu Kayıt (Bulk Insert)
            if (productsToAdd.Any())
            {
                await _context.Products.AddRangeAsync(productsToAdd);
                await _context.SaveChangesAsync();
            }

            return productsToAdd.Count;
        }
        
        
        public async Task<byte[]> ExportProductsAsync(int sellerId)
        {
            // 1. İlgili satıcının ürünlerini veritabanından çek (Performans için AsNoTracking)
            var products = await _context.Products
                .AsNoTracking()
                .Where(p => p.SellerId == sellerId)
                .Include(p => p.Category)
                .ToListAsync();

            // 2. ClosedXML ile sanal bir çalışma kitabı oluştur
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Ürünlerim");

                // 3. Başlık Satırını (Header) Yaz ve Şekillendir
                worksheet.Cell(1, 1).Value = "Ürün ID";
                worksheet.Cell(1, 2).Value = "Ürün Adı";
                worksheet.Cell(1, 3).Value = "Fiyat";
                worksheet.Cell(1, 4).Value = "Stok";
                worksheet.Cell(1, 5).Value = "Kategori";

                var headerRow = worksheet.Range("A1:E1");
                headerRow.Style.Font.Bold = true;
                headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;

                // 4. Verileri Döngüyle Satırlara Bas
                int rowIndex = 2;
                foreach (var product in products)
                {
                    worksheet.Cell(rowIndex, 1).Value = product.Id;
                    worksheet.Cell(rowIndex, 2).Value = product.Name;
                    worksheet.Cell(rowIndex, 3).Value = product.Price;
                    worksheet.Cell(rowIndex, 4).Value = product.Stock;
                    worksheet.Cell(rowIndex, 5).Value = product.Category?.Name ?? "Kategorisiz";
            
                    rowIndex++;
                }

                // Sütun genişliklerini içeriğe göre otomatik ayarla
                worksheet.Columns().AdjustToContents();

                // 5. Dosyayı MemoryStream'e kaydet ve byte dizisine çevir (Diske dokunmuyoruz!)
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }
    }
}