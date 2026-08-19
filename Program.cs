using Hangfire;
using Prometheus;
using Serilog;
using TrendyolMiniApi.Extensions;
using TrendyolMiniApi.Hubs;
using TrendyolMiniApi.Jobs;
using TrendyolMiniApi.Markers;
using TrendyolMiniApi.Hubs;
using TrendyolMiniApi.Workers;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. TEMEL ALTYAPI (LOGLAMA)
// ==========================================
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console() 
    .WriteTo.File("Logs/trendyol-log-.txt", rollingInterval: RollingInterval.Day) 
    .WriteTo.Seq("http://localhost:5341") // <-- BİR TEK BU SATIRI EKLEDİK
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
    .AsSelf() // İnterface'siz servisler için  Kendi adıyla da çağırılabilmesini sağlar.
    .WithScopedLifetime()
    
    // 2. ITransientService etiketi olan TÜM sınıfları bul
    .AddClasses(classes => classes.AssignableTo<ITransientService>())
    .AsImplementedInterfaces()
    .AsSelf() 
    .WithTransientLifetime()
    
    // 3. ISingletonService etiketi olan TÜM sınıfları bul
    .AddClasses(classes => classes.AssignableTo<ISingletonService>())
    .AsImplementedInterfaces()
    .AsSelf() 
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


builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

// B. Hazır Altyapı Servisleri (Extension metotlarımızdan geliyor)
builder.Services.AddSwaggerInfrastructure();
builder.Services.AddJwtInfrastructure(builder.Configuration);
builder.Services.AddDatabaseInfrastructure(builder.Configuration);
builder.Services.AddCachingInfrastructure(builder.Configuration);
builder.Services.AddExceptionHandlingInfrastructure();
builder.Services.AddHttpClientsInfrastructure();
builder.Services.AddHangfireInfrastructure(builder.Configuration);
builder.Services.AddMappingInfrastructure();
builder.Services.AddSoapClientInfrastructure();


builder.Services.AddHostedService<GroupMessageWorker>();

// ==========================================
// 3. UYGULAMANIN İNŞASI VE ARA YAZILIMLAR (MIDDLEWARE)
// ==========================================
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
// 1. HTTP İsteklerinin ne kadar sürdüğünü ve kaç tane geldiğini sayar
app.UseHttpMetrics();

app.UseExceptionHandler(); // Akıllı kalkanımız devrede
app.UseStaticFiles();
app.UseHttpsRedirection();

app.UseAuthentication(); 
app.UseAuthorization();

// Bizim yazdığımız, Claim'leri DTO'ya çeviren middleware
app.UseMiddleware<UserContextMiddleware>();

app.MapControllers();
app.MapHub<ChatHub>("/chathub"); // Canlı sohbet telsizi

/*
// 1. Hangfire Kontrol Panelini aktif et (Tarayıcıdan /hangfire adresine girerek izleyebilirsin)
app.UseHangfireDashboard("/hangfire");
*/

#region MyRegion
// 2. İşçiyi (Job) programla! 
//RecurringJob, Hangfire'ın tekrarlayan (Recurring) işleri yönetmek için kullandığı statik sınıftır.
//AddOrUpdate(...), Add → Eğer görev yoksa oluştur. Update → Aynı isimde görev varsa ayarlarını güncelle.
//<ICurrencySyncService> ,"Bu işi çalıştırırken ICurrencySyncService servisini Dependency Injection container'dan al." arka planda "builder.Services.AddScoped<ICurrencySyncService, CurrencySyncService>();" gibi bir işlem yapar.
//"kur-guncelleme-gorevi" ,Bu görevin benzersiz kimliği (Job Id)'dir. Dashboard'da bu isim görünür.
//service => service.SyncUsdRateAsync(), Bu bir Lambda Expression'dır. Parametre "service" aslında "ICurrencySyncService service" nesnesidir.
//Cron.MinuteInterval(5), Bu zamanlama bilgisidir. her 5 dakikada bir çalıştır. demektir.

#endregion
/*
// Her 5 dakikada bir Manager sınıfındaki metodu tetikle
RecurringJob.AddOrUpdate<CurrencyJobManager>(
    "kur-guncelleme-ve-redis-yayinlama", 
    manager => manager.TriggerSyncAndPublish(), 
    Cron.MinuteInterval(1) 
);*/

// 2. Metriklerin dışarıdan okunabilmesi için /metrics adında bir uç nokta açar
app.MapMetrics();

app.Run();