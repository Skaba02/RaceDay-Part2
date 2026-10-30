using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Services;

// Always use the invariant culture, so a decimal is written 10.5 on every computer.
// This matches what web browsers send in number fields (even on a South African Windows setup).
System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;

var builder = WebApplication.CreateBuilder(args);

// MVC. Every POST form is protected against cross-site request forgery.
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

// Session - keeps the logged-in user's details and the API session cookie between requests.
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// The ONLY way this website gets data: the RaceDay API.
// ApiBaseUrl can be changed in appsettings.json or with an environment variable (used by Docker).
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7100";

builder.Services.AddHttpClient<ApiClient>(client =>
    {
        client.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
        client.Timeout = TimeSpan.FromSeconds(30);
    })
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        // UseCookies = false: the cookies of each website user are handled by ApiClient, never shared.
        var handler = new HttpClientHandler { UseCookies = false };

        if (builder.Configuration.GetValue<bool>("ApiAllowUntrustedCertificate"))
        {
            handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        }

        return handler;
    });

var app = builder.Build();

// Friendly error pages: users never see a raw error or stack trace.
app.UseExceptionHandler("/Home/Error");
app.UseStatusCodePagesWithReExecute("/Home/Status", "?code={0}");

app.UseStaticFiles();
app.UseRouting();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
