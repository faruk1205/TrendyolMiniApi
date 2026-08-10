namespace TrendyolMiniApi.DTOs
{
    public class ImportResultDto<T>
    {
        public int TotalRowCount { get; set; }
        public List<T> Items { get; set; } = new();
        public List<ImportRowErrorDto> Errors { get; set; } = new();
        
        public bool IsSuccess => !Errors.Any(); 
    }
}