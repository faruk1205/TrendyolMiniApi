using System.ComponentModel.DataAnnotations;

namespace TrendyolMiniApi.DTOs
{
    public class OrderCreateDto
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        [Range(1, 100, ErrorMessage = "En az 1, en fazla 100 adet sipariş verebilirsiniz.")]
        public int Quantity { get; set; }
    }
}