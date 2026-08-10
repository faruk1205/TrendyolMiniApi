namespace TrendyolMiniApi.DTOs
{
    public class ImportRowErrorDto
    {
        public int RowNumber { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}