using AutoMapper;
using TrendyolMiniApi.Entities;
using TrendyolMiniApi.DTOs;
using TrendyolMiniApi.Models;

namespace TrendyolMiniApi.Mappings
{
    public class MappingProfile : Profile  //Profile ise AutoMapper'ın sağladığı temel (base) sınıftır. "Ben AutoMapper'a dönüşüm kurallarını tanımlayan bir profil oluşturuyorum."
    {
        public MappingProfile()
        {
            //bir method sonucu döndürmek için 
            //Anlamı Product nesnesini ProductResponseDto nesnesine dönüştür.
            CreateMap<Product, ProductResponseDto>();
            
            // sışardan parametre olarak dto alanlar için  (Örn: Ürün eklerken)
            CreateMap<ProductCreateDto, Product>();
        }
    }
}
/*örneğin :

Product product = new Product
{
    Id = 1,
    Name = "Laptop",
    Price = 35000
};            

AutoMapper sayesinde"ProductResponseDto dto = _mapper.Map<ProductResponseDto>(product);" yazdığında otomatik olarak :

dto.Id = product.Id;
dto.Name = product.Name;
dto.Price = product.Price;   işlemi yapılır. */