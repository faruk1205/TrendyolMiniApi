using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;
using TrendyolMiniApi.Data;
using TrendyolMiniApi.Middlewares;
using TrendyolMiniApi.Providers;

namespace TrendyolMiniApi.Extensions
{
    public static class ServiceCollectionExtensions 
    {
        // 1. SWAGGER AYARLARI
        //static demek, bu sınıfın bellekte tek bir sabit yeri vardır ve new ile üretilemez demektir. Bu sayede .NET, uygulama başlarken bu metotları doğrudan hafızaya alır ve her yerden erişilebilir hale getirir.
        public static IServiceCollection AddSwaggerInfrastructure(this IServiceCollection services){
            
            //Program.cs dosyasının içinde sürekli kullandığımız builder.Services var ya; işte o nesnenin veri tipi IServiceCollection'dır.
            // İçinde uygulamanın çalışması için gereken tüm servislerin (Veritabanı, JWT, Swagger, senin yazdığın UserService vb.) listesini tutan resmi bir .NET arayüzüdür (Interface).
        
            
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Trendyol Mini API", Version = "v1" });
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Lütfen kutucuğa sadece Token'ınızı yapıştırın."
                });
                c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
                });
            });
            return services; 
        }

        // 2. JWT VE KİMLİK DOĞRULAMA AYARLARI (Configuration istiyor)
        public static IServiceCollection AddJwtInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration.GetSection("JwtSettings:Secret").Value!)),
                        ValidateIssuer = true,
                        ValidIssuer = configuration.GetSection("JwtSettings:Issuer").Value,
                        ValidateAudience = true,
                        ValidAudience = configuration.GetSection("JwtSettings:Audience").Value,
                        ValidateLifetime = true
                    };
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var accessToken = context.Request.Query["access_token"];
                            var path = context.HttpContext.Request.Path;
                            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/chathub"))
                            {
                                context.Token = accessToken;
                            }
                            return Task.CompletedTask;
                        }
                    };
                });
            return services;
        }

        // 3. VERİTABANI AYARLARI (Configuration istiyor)
        public static IServiceCollection AddDatabaseInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
            return services;
        }

        // 4. ÖNBELLEK (CACHE) AYARLARI
        public static IServiceCollection AddCachingInfrastructure(this IServiceCollection services)
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = "localhost:6379"; 
                options.InstanceName = "Trendyol_";      
            });

            #pragma warning disable EXTEXP0018
            services.AddHybridCache(options =>
            {
                options.DefaultEntryOptions = new Microsoft.Extensions.Caching.Hybrid.HybridCacheEntryOptions
                {
                    Expiration = TimeSpan.FromMinutes(5),
                    LocalCacheExpiration = TimeSpan.FromMinutes(5)
                };
            });
            #pragma warning restore EXTEXP0018

            return services;
        }

        // 5. HATA YÖNETİMİ (EXCEPTION HANDLING) AYARLARI
        public static IServiceCollection AddExceptionHandlingInfrastructure(this IServiceCollection services)
        {
            services.AddExceptionHandler<GlobalExceptionHandler>();
            services.AddProblemDetails(); 
            return services;
        }
        
        // 6. HTTP CLIENT VE DIŞ API AYARLARI
        public static IServiceCollection AddHttpClientsInfrastructure(this IServiceCollection services)
        {
            // 1. REST Provider'ı çantaya ekle
            services.AddHttpClient<IExchangeRateProvider, RestExchangeRateProvider>(client =>
            {
                // Dış API'nin ana adresini burada tanımlıyoruz
                client.BaseAddress = new Uri("https://api.exchangerate-api.com/v4/");
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                //Bu satır, API'ye gönderilen HTTP isteğine bir Accept header'ı ekler. "Bu isteğe vereceğin cevabı mümkünse JSON formatında gönder."
                
                // API 5 saniye içinde cevap vermezse bekleme, bağlantıyı kes!
                client.Timeout = TimeSpan.FromSeconds(5);
            });
            
            // 2. SOAP Provider'ı çantaya ekle
            services.AddHttpClient<IExchangeRateProvider, SoapExchangeRateProvider>(client =>
            {
                // Merkez Bankası günlük kur adresi
                client.BaseAddress = new Uri("https://www.tcmb.gov.tr/"); 
                client.Timeout = TimeSpan.FromSeconds(15);

            });

            return services;
        }
    }
}

//this Kelimesinin Sihri: Bir metodun ilk parametresinin başına this koyduğunda, o metodu IServiceCollection tipindeki nesnelere yama yapmış olursun.
//Böylece Microsoft'un kendi yazdığı AddControllers() veya AddSignalR() metotları nasıl builder.Services. yazınca çıkıyorsa, senin yazdığın metotlar da aynı listede çıkar. Sisteme kendi özel komutlarını öğretmiş oldun.
/*this IServiceCollection services Ne Oluyor?
İşte bütün sihrin gerçekleştiği yer burasıdır. C# dilinde bir metodun ilk parametresinin başına this kelimesini koyarsan, derleyiciye şu emri verirsin:
"Bu metodu al ve IServiceCollection tipindeki nesnelerin orijinal bir özelliğiymiş gibi ona yapıştır."
Eğer başına this KOYMASAYDIK:
Bu sıradan bir metot olurdu. Program.cs içinde bunu kullanmak için o çirkin ve uzun yazımı kullanmak zorunda kalırdık:

// "this" olmasaydı Program.cs'te böyle yazmak zorundaydık:
ServiceCollectionExtensions.AddSwaggerInfrastructure(builder.Services);

Başına this KOYDUĞUMUZ İçin:
Bu metot artık builder.Services nesnesinin kendisine yapıştı (sanki Microsoft'un kendi yazılımcıları kodlamış gibi). Program.cs içinde doğrudan şöyle çağırabiliyoruz:

// "this" sayesinde kodumuz bu kadar şık ve akıcı olur:
builder.Services.AddSwaggerInfrastructure();*/