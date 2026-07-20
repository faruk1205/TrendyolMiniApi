using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi; // En güncel sürümde tipler doğrudan bunun altında!
using System.Text;
using TrendyolMiniApi.Data;
using Serilog;
using TrendyolMiniApi.Hubs; // Bunu en üste eklemeyi unutma
using TrendyolMiniApi.Middlewares;
using TrendyolMiniApi.Services;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. SERVİSLER BÖLÜMÜ (DEPENDENCY INJECTION)
// =========================================


//  SERILOG AYARLARI
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console() // Şimdilik sadece terminale yazsın (İleride File veya ElasticSearch eklenebilir)
    .WriteTo.File("Logs/trendyol-log-.txt", rollingInterval: RollingInterval.Day) // YENİ: Logları dosyaya kaydet!
    .CreateLogger();

builder.Host.UseSerilog(); // .NET'in yerleşik loglayıcısını çöpe at, Serilog'u kullan diyoruz.


// "Sistemde biri senden IFileService isterse, ona FileService sınıfını ver" diyoruz.
builder.Services.AddScoped<IFileService, FileService>();



builder.Services.AddControllers();

builder.Services.AddScoped<MessageService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IFavoriteService, FavoriteService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAuthService, AuthService>();





// SignalR servislerini sisteme dahil et
builder.Services.AddSignalR();


//  HATA YAKALAYICIYI SİSTEME KAYDET
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails(); // RFC 7807 formatını aktif et

//  REDIS BAĞLANTISINI TANIT (L2 Dağıtık Cache için)
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379"; // Docker'da ayaklandırdığımız adres
    options.InstanceName = "Trendyol_";      // Redis içindeki anahtarların başına gelecek ön ek
});

// 2. HYBRID CACHE (Zaten eklemiştik)
#pragma warning disable EXTEXP0018
builder.Services.AddHybridCache(options =>
{
    options.DefaultEntryOptions = new Microsoft.Extensions.Caching.Hybrid.HybridCacheEntryOptions
    {
        Expiration = TimeSpan.FromMinutes(5),
        LocalCacheExpiration = TimeSpan.FromMinutes(5)
    };
});
#pragma warning restore EXTEXP0018

// PostgreSQL Veritabanı Bağlantısı
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT GÜVENLİK ALTYAPISI
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                builder.Configuration.GetSection("JwtSettings:Secret").Value!)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration.GetSection("JwtSettings:Issuer").Value,
            ValidateAudience = true,
            ValidAudience = builder.Configuration.GetSection("JwtSettings:Audience").Value,
            ValidateLifetime = true
        };
        //MEsajlaşma sistemi yüzünden eklendi
        //JWT doğrulama sürecinde çalışacak olayları (events) tanımlamak için JwtBearerEvents nesnesi oluşturulur. Bu sayede token'ın nasıl okunacağı gibi varsayılan davranışlar özelleştirilebilir.
        options.Events = new JwtBearerEvents
        {
            //OnMessageReceived olayı, sunucuya bir istek geldiğinde ve JWT doğrulaması başlamadan hemen önce çalışır. Bu olayın amacı, JWT token'ının nereden alınacağını belirlemektir.
            //Buradaki context, OnMessageReceived olayına ASP.NET Core tarafından otomatik olarak gönderilen parametredir.context içinde ne var- > MessageReceivedContext, o anda gelen HTTP isteğiyle ilgili birçok bilgi içerir.
            OnMessageReceived = context =>
            {
                //Gelen isteğin URL'sindeki access_token isimli query parametresini okur.
                var accessToken = context.Request.Query["access_token"];

                //İsteğin hangi adrese (endpoint'e) yapıldığını alır.
                var path = context.HttpContext.Request.Path;
                
                //URL'de gerçekten bir token var mı? İstek SignalR Hub adresi olan /chathub'a mı yapılıyor? Her iki koşul da doğruysa, URL'deki token kullanılacaktır.
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/chathub"))
                {
                    // URL'den aldığın token'ı, sanki Header'dan gelmiş gibi sisteme yedir! URL'den alınan JWT token'ı, doğrulama sistemine verilir. Bu satır sayesinde URL'den gelen token, sanki Authorization header'ından gelmiş gibi kabul edilir ve normal JWT doğrulama işlemi devam eder
                    context.Token = accessToken;
                }
                //Bu olay içinde yapılacak işlem tamamlanmıştır. Herhangi bir asenkron işlem olmadığı için tamamlanmış bir Task döndürülür ve JWT doğrulama süreci kaldığı yerden devam eder.
                return Task.CompletedTask;
            }
        };
    });

// SWAGGER'A "AUTHORIZE" (KİLİT) BUTONU EKLEME (OpenAPI 2.x Uyumlu Yeni Versiyon)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Trendyol Mini API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Lütfen kutucuğa sadece Token'ınızı yapıştırın (Başına Bearer yazmanıza gerek yok)."
    });

    // Senin yakaladığın o muazzam OpenAPI 2.x çözümü:
    c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
    });
});

var app = builder.Build();

// ==========================================
// 2. ARA YAZILIM (MIDDLEWARE) BÖLÜMÜ
// ==========================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//  KALKANI DEVREYE SOK
app.UseExceptionHandler();

app.UseStaticFiles();

app.UseHttpsRedirection();

app.UseAuthentication(); 
app.UseAuthorization();

app.MapControllers();

// Bu satır, ChatHub sınıfını /chathub adresine bağlar. Yani istemciler (React, Angular, JavaScript, Flutter vb.) SignalR bağlantısını bu URL üzerinden kurar.
app.MapHub<ChatHub>("/chathub");


app.Run();