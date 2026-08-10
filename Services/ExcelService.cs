using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using TrendyolMiniApi.DTOs;
using TrendyolMiniApi.Models;

namespace TrendyolMiniApi.Services
{
    public class ExcelService : IExcelService
    {
        private static readonly string[] AllowedExtensions = { ".xlsx" };
        private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

        public async Task<byte[]> ExportAsync<T>(
            IEnumerable<T> data,
            Dictionary<string, Func<T, object?>> columnMappings,
            string sheetName = "Sayfa1",
            CancellationToken cancellationToken = default)
        {
            if (data == null) throw new ArgumentNullException(nameof(data)); 
            if (columnMappings == null || columnMappings.Count == 0) //null mu veya boş mu ?
                throw new ArgumentException("Kolon eşlemeleri boş olamaz.", nameof(columnMappings));

            return await Task.Run(() =>  //Excel oluşturma işlemini ayrı bir iş parçacığında (thread) çalıştırır ve tamamlanmasını bekler. yani bu işi  işlemi çağıran thread değil pooldan başka bir threade'e yaptırılır. Amaç ana threadi meşgul ettirmemek. Senkron işlemlerde kullanılır asenkronlarda kullanılmaz.
            {
                cancellationToken.ThrowIfCancellationRequested(); //iptal istenmişse OperationCanceledException fırlatır.

                using var workbook = new XLWorkbook();  //yeni excel dosyası oluşturur.
                var worksheet = workbook.Worksheets.Add(SanitizeSheetName(sheetName)); //yeni sayfa ekler, önce sheetName temizleni çünkü excel bazı karakterleri kabul etmez.

                // 1. Başlıklar
                int colIndex = 1;
                foreach (var columnName in columnMappings.Keys)
                {
                    var cell = worksheet.Cell(1, colIndex); // 1. satır colIndex'ci sutun
                    cell.Value = columnName;
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                    colIndex++;
                }
                worksheet.SheetView.FreezeRows(1); //ilk satırı sabitler yani Excelde aşağı kaydırsan bile başlık görünür.

                // 2. Veriler
                int rowIndex = 2;
                foreach (var item in data)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    colIndex = 1;
                    foreach (var mappingFunc in columnMappings.Values) //valuelarda gezebilirim neden gezemiyim :D
                    
                    {
                        var value = mappingFunc(item);
                        SetCellValue(worksheet.Cell(rowIndex, colIndex), value); //hücreye veri tipine uygun şekilde yazar.
                        colIndex++;
                    }
                    rowIndex++;
                }

                if (rowIndex <= 5000)
                    worksheet.Columns().AdjustToContents(); //5000 satırdan azsa kolon genişlikleri otomatik ayarlar . fazlaysa performans için yapılmaz.

                using var stream = new MemoryStream(); //dosya ramde oluşturulur. Diske yazılmaz.
                workbook.SaveAs(stream); //Excel dosyasını stream'e kaydeder.
                return stream.ToArray(); //Excel dosyasını stream'e kaydeder.
            }, cancellationToken);
        }

        public async Task<ImportResultDto<T>> ImportAsync<T>(
            IFormFile file,
            Func<List<string>, T> mapFunc,
            int startRow = 2,
            CancellationToken cancellationToken = default)
        {
            ValidateFile(file); //bu methodu altta biz yazdık validasyon kontrolünü yapar.

            var result = new ImportResultDto<T>();
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream, cancellationToken);  //Yüklenen Excel dosyası MemoryStream'e kopyalanır.
            stream.Position = 0; //okuma imleci en başa alınır.

            using var workbook = new XLWorkbook(stream); // memoryStream'den Excel açılır. Artık çalışma sayfalarına erişebiliriz.
            var worksheet = workbook.Worksheet(1); //ilk sayfayı alır.
            var usedRange = worksheet.RangeUsed(); // dolu hücrelerin bulunduğu alanı alır.

            if (usedRange == null) return result;  //hiç veri yoksa boş sonuç döndürür.

            var rows = usedRange.RowsUsed().Skip(startRow - 1).ToList(); //sadece dolu olan satırlar başlık satırı atlanarak alınır. listeye çevrilir.
            result.TotalRowCount = rows.Count;

            int rowIndex = startRow;
            foreach (var row in rows)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var cellValues = new List<string>(); // o satırdaki hücreleri tutacak liste
                    int lastColumn = row.LastCellUsed()?.Address.ColumnNumber ?? 1; //son dolu sütun. eğer null gelirse 1 kullan.

                    for (int i = 1; i <= lastColumn; i++)
                        cellValues.Add(row.Cell(i).GetString()); // cell-> string -> listeye ekle

                    var mappedItem = mapFunc(cellValues); // //liste -> mapFunc -> Nesne.  yani ["Ali","25"] bundan nenw person { name="ali" ,Age=25 } şeklinde nesne oluşur. burda kullanılan metod bu servisin metodunu nerde kullanacaksan orda parametre olarak veriyorsun.
                    result.Items.Add(mappedItem); //oluşan mesle person items listesine eklenir.
                }
                catch (Exception ex)
                {
                    result.Errors.Add(new ImportRowErrorDto() { RowNumber = rowIndex, Message = ex.Message }); 
                }
                rowIndex++;
            }
            return result;
        }

        private static void SetCellValue(IXLCell cell, object? value) //Hücreye gelen verinin türünü kontrol edip Excel'e doğru formatta yazmaktır.
        {
            switch (value)
            {
                case null: cell.Value = Blank.Value; break;
                case DateTime dt: cell.Value = dt; cell.Style.DateFormat.Format = "dd.MM.yyyy"; break;
                case bool b: cell.Value = b; break;
                case byte or sbyte or short or ushort or int or uint or long or ulong: cell.Value = Convert.ToInt64(value); break;
                case float or double or decimal: cell.Value = Convert.ToDouble(value); break;
                default: cell.Value = value.ToString(); break;
            }
        }

        private static string SanitizeSheetName(string sheetName) //Excel sayfa isimlerinde şu karakterler kullanılamaz:

        {
            var invalidChars = new[] { '\\', '/', '?', '*', '[', ']', ':' };
            var sanitized = new string(sheetName.Select(c => invalidChars.Contains(c) ? '_' : c).ToArray());
            return sanitized.Length > 31 ? sanitized[..31] : sanitized;
        }

        private static void ValidateFile(IFormFile? file) //Bu metod yüklenen dosyayı güvenlik açısından doğrular.
        {
            if (file == null || file.Length == 0) throw new ArgumentException("Dosya boş.", nameof(file));
            if (file.Length > MaxFileSizeBytes) throw new ArgumentException("Dosya boyutu aşıldı.", nameof(file));
            if (!AllowedExtensions.Contains(Path.GetExtension(file.FileName), StringComparer.OrdinalIgnoreCase)) throw new ArgumentException("Sadece .xlsx", nameof(file));

            using var checkStream = file.OpenReadStream();
            var header = new byte[2];
            checkStream.Read(header, 0, 2);
            if (header[0] != 0x50 || header[1] != 0x4B) throw new ArgumentException("Geçersiz Excel formatı.", nameof(file));
            //Bu kod, yüklenen dosyanın gerçekten bir Excel (.xlsx) dosyası olup olmadığını kontrol ediyor. Sadece dosya uzantısına
            //(.xlsx) güvenmiyor, dosyanın ilk iki baytını (dosya imzasını) da kontrol ediyor.
        }
    }
}