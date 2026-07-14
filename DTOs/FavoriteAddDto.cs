using System.ComponentModel.DataAnnotations;

namespace TrendyolMiniApi.DTOs
{
    public class FavoriteAddDto
    {
        [Required(ErrorMessage = "Favoriye eklenecek ürünün ID'si zorunludur.")]
        public int ProductId { get; set; }
    }
}