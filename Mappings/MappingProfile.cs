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
            
            // CartItem'dan CartItemResponseDto'ya dönüşüm kuralları
            CreateMap<CartItem, CartItemResponseDto>()
                // Veritabanındaki 'Id' sütunu, DTO'daki 'CartItemId'ye gitsin
                .ForMember(dest => dest.CartItemId, opt => opt.MapFrom(src => src.Id))
    
                // Veritabanındaki 'Product.Price' sütunu, DTO'daki 'UnitPrice' alanına gitsin
                .ForMember(dest => dest.UnitPrice, opt => opt.MapFrom(src => src.Product!.Price));

                // Not: ProductId, ProductName ve Quantity alanları isim benzerliğinden OTOMATİK eşleşir!
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



/*MADEM MANUEL TANIMLAMA YAPICAM OTOMAPPİNG NEDEN KULLANAYIM Kİ ?

-Eğer modelinde 30 tane sütun (CreatedDate, UpdatedDate, Quantity, Description, CategoryId vb.) olsaydı, AutoMapper bunların 28 tanesini isimleri aynı olduğu için hiçbir kural yazmana gerek kalmadan %100 otomatik eşleştirecekti.
Yani sen 30 satırlık amelelik yapmak yerine, sadece ismi farklı olan 2 satır için kural yazdın.
-Eğer DTO'nu tasarlarken isimleri veritabanıyla aynı yapsaydın.Profile o 2 satırı da yazmayacaktın. Sadece CreateMap<CartItem, CartItemResponseDto>() diyecek ve konuyu kapatacaktın. Sıfır ayar, %100 otomatik dönüşüm!
-Senin yazdığın o .Select(c => new ...) manuel eşleştirmesi bir serviste çok şık durabilir.
Ama yarın bir gün CartItemResponseDto nesnesini uygulamanın 5 farklı servisinde daha (Admin paneli, Fatura servisi, E-posta gönderim servisi vb.) kullanman gerekirse ne olacak?
O 10 satırlık .Select() kodunu 5 farklı dosyaya kopyala-yapıştır yapacaksın*/