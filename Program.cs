using Serilog;
using TrendyolMiniApi.Extensions;
using TrendyolMiniApi.Hubs;
using TrendyolMiniApi.Markers;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. TEMEL ALTYAPI (LOGLAMA)
// ==========================================
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console() 
    .WriteTo.File("Logs/trendyol-log-.txt", rollingInterval: RollingInterval.Day) 
    .CreateLogger();

builder.Host.UseSerilog();

// ==========================================
// 2. SERVİSLERİN KAYIT EDİLMESİ
// ==========================================



//etiket yöntemi için marker dosyasında boş interface'ler tanımladık (Scrutor)
builder.Services.Scan(scan => scan
    .FromAssemblyOf<Program>()
    
    // 1. IScopedService etiketi olan TÜM sınıfları bul
    .AddClasses(classes => classes.AssignableTo<IScopedService>())
    .AsImplementedInterfaces()
    .WithScopedLifetime()
    
    // 2. ITransientService etiketi olan TÜM sınıfları bul
    .AddClasses(classes => classes.AssignableTo<ITransientService>())
    .AsImplementedInterfaces()
    .WithTransientLifetime()
    
    // 3. ISingletonService etiketi olan TÜM sınıfları bul
    .AddClasses(classes => classes.AssignableTo<ISingletonService>())
    .AsImplementedInterfaces()
    .WithSingletonLifetime()
);
// A. Kendi yazdığımız iş servisleri (Scrutor ile otomatik taranır)
/*builder.Services.Scan(scan => scan
    .FromAssemblyOf<Program>()
    .AddClasses(classes => classes.Where(type => type.Name.EndsWith("Service")))
    .AsImplementedInterfaces()
    .WithScopedLifetime()
);
*/


builder.Services.AddControllers();
builder.Services.AddSignalR();

// B. Hazır Altyapı Servisleri (Extension metotlarımızdan geliyor)
builder.Services.AddSwaggerInfrastructure();
builder.Services.AddJwtInfrastructure(builder.Configuration);
builder.Services.AddDatabaseInfrastructure(builder.Configuration);
builder.Services.AddCachingInfrastructure();
builder.Services.AddExceptionHandlingInfrastructure();
builder.Services.AddHttpClientsInfrastructure();

// ==========================================
// 3. UYGULAMANIN İNŞASI VE ARA YAZILIMLAR (MIDDLEWARE)
// ==========================================
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler(); // Akıllı kalkanımız devrede
app.UseStaticFiles();
app.UseHttpsRedirection();

app.UseAuthentication(); 
app.UseAuthorization();

app.MapControllers();
app.MapHub<ChatHub>("/chathub"); // Canlı sohbet telsizi

app.Run();