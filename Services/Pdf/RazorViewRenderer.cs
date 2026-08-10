using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using TrendyolMiniApi.Markers;

public interface IRazorViewRenderer : IScopedService
{
    Task<string> RenderToStringAsync<T>(string viewName, T model);
}

public class RazorViewRenderer : IRazorViewRenderer
{
    private readonly IRazorViewEngine _viewEngine;  //IRazorViewEngine (Razor Motoru): C# kodları (@Model.UrunAdi) ile HTML etiketlerini (<h1>) harmanlayıp, ortaya saf bir HTML çıkaran fabrikadır.
    private readonly ITempDataProvider _tempDataProvider;
    private readonly IServiceProvider _serviceProvider;

    public RazorViewRenderer(
        IRazorViewEngine viewEngine,
        ITempDataProvider tempDataProvider,
        IServiceProvider serviceProvider)
    {
        _viewEngine = viewEngine;
        _tempDataProvider = tempDataProvider;
        _serviceProvider = serviceProvider;
    }

    public async Task<string> RenderToStringAsync<T>(string viewName, T model)
    {
        var httpContext = new DefaultHttpContext { RequestServices = _serviceProvider };
        var actionContext = new ActionContext(httpContext, new Microsoft.AspNetCore.Routing.RouteData(), new ActionDescriptor()); //ActionContext (Eylem Bağlamı): .NET motoruna "Şu an hangi adresteyiz, hangi Controller çalışıyor?" bilgisini veren haritadır.
        //Razor motoru çalışmak için her zaman bir "Web Tarayıcısı" tarafından çağrıldığını sanmak ister. Biz burada DefaultHttpContext diyerek ona sahte bir ziyaretçi yaratıyoruz. Sanki biri siteye girmiş gibi motoru kandırıyoruz.
        
        
        // Motora diyoruz ki: "Sana viewName adında bir adres verdim (örneğin: ~/Services/Pdf/InvoiceTemplate.cshtml). Git o dosyayı bul."
        var viewResult = _viewEngine.GetView(executingFilePath: null, viewPath: viewName, isMainPage: true);

        // 2. Eğer GetView bulamazsa, güvenlik ağı olarak "FindView" ile klasik klasörleri tarar
        if (!viewResult.Success)
        {
            viewResult = _viewEngine.FindView(actionContext, viewName, isMainPage: true);
        }

        // 3. Hala bulamadıysa, nerelere baktığını söyleyerek detaylı hata fırlatır
        if (!viewResult.Success)
        {
            var searchedLocations = string.Join("\n", viewResult.SearchedLocations);
            throw new InvalidOperationException($"View bulunamadı: {viewName}.\nŞu konumlara bakıldı:\n{searchedLocations}");
        }

        await using var sw = new StringWriter(); //StringWriter (Yazı Kovası): Normalde üretilen HTML doğrudan internet kablolarına (Network) akar. StringWriter, o akan HTML'i yere dökülmeden içine doldurduğumuz dijital bir "kova"dır. Kovamızı yarattık
        var viewDictionary = new ViewDataDictionary<T>( //ViewDataDictionary (Veri Sözlüğü): Bizim faturaya basılacak olan DTO nesnemizi (Koli), Razor motoruna taşımak için kullandığımız çantadır.
            new EmptyModelMetadataProvider(), new ModelStateDictionary()) { Model = model };
        //Buradaki model bizim InvoiceDto nesnemiz. Onu alıyoruz ve Razor motorunun anlayacağı özel bir sözlük (ViewDataDictionary) çantasına koyuyoruz ki HTML'in içine verileri (Müşteri Adı, Fiyat) basabilsin.
        
        
        var viewContext = new ViewContext(
            actionContext, viewResult.View, viewDictionary,
            new TempDataDictionary(httpContext, _tempDataProvider),
            sw, new HtmlHelperOptions());

        
        // Motoru çalıştır ve sonucu 'sw' (StringWriter) kovasına doldur!
        await viewResult.View.RenderAsync(viewContext);
        
        // Kovanın içindeki HTML'i string (metin) olarak bize teslim et.
        return sw.ToString();
    }
}

// RAZOR MOTORU : Görevini en yalın haliyle özetlemek gerekirse: C# kodları ile HTML kodlarını aynı dosya içinde kusursuzca harmanlamaya yarayan bir çevirmen motorudur.
//Senin yazdığın C# mantığını (döngüler, if-else blokları, değişkenler) çalıştırır, HTML şablonuyla birleştirir ve ortaya içinde tek bir satır bile C# kodu kalmamış saf bir HTML çıkarır. (SSR)

//.NET'in yerleşik Razor Motoru, inanılmaz güçlü olmasına rağmen çok katı bir kurala sahiptir: "Ben sadece gerçek bir kullanıcı tarayıcıdan HTTP isteği atarsa çalışırım ve ürettiğim HTML'i doğrudan o tarayıcıya fırlatırım."
//new DefaultHttpContext() diyerek sanki biri siteye girmiş gibi sahte bir oturum yaratıyoruz.
//ActionContext ile sisteme "Şu an şu adresteyiz" diyen sahte bir harita veriyoruz. Böylece Razor motoru kendini güvende ve gerçek bir web isteğinin ortasında sanıyor.
//_viewEngine.GetView ve _viewEngine.FindView komutlarıyla motora, "Benim InvoiceTemplate.cshtml adında bir dosyam var, git onu bul" diyoruz. Motor hem senin verdiğin açık adrese bakar hem de geleneksel klasörleri tarar.
//Senin veritabanından çekip hazırladığın o InvoiceDto nesnesini (Müşteri Adı, Fiyatlar vs.), ViewDataDictionary adındaki özel bir çantaya koyuyoruz.
//motoru çalıştırıp sonucu yakalamak:
//new StringWriter() diyerek dijital bir kova yaratıyoruz.
//Motoru çalıştırdığımızda (RenderAsync), ortaya çıkan o saf HTML kodlarının doğrudan bu kovaya akmasını sağlıyoruz.
//Son olarak sw.ToString() diyerek, kovanın içindeki HTML'i dümdüz bir metin olarak alıp sisteme (sonrasında PDF'e çevirecek olan Chrome motoruna) teslim ediyoruz.

/*Kısacası bu sınıf; verilerini (C#) ve tasarımını (HTML) alıp, onları bir fırında pişiren ve sana çıtır çıtır bir HTML metni veren özel bir fırındır.*/