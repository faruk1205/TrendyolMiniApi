using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi; // En güncel sürümde tipler doğrudan bunun altında!
using System.Text;
using TrendyolMiniApi.Data;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. SERVİSLER BÖLÜMÜ (DEPENDENCY INJECTION)
// ==========================================

builder.Services.AddControllers();

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

app.UseStaticFiles();

app.UseHttpsRedirection();

app.UseAuthentication(); 
app.UseAuthorization();

app.MapControllers();

app.Run();