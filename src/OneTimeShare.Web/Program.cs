using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Server.IIS;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using OneTimeShare.Web.Data;
using OneTimeShare.Web.Services;
using OneTimeShare.Web.Hosted;
using OneTimeShare.Web.Models.Options;

var builder = WebApplication.CreateBuilder(args);

var googleClientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID") 
    ?? throw new InvalidOperationException("GOOGLE_CLIENT_ID environment variable is required");
var googleClientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET") 
    ?? throw new InvalidOperationException("GOOGLE_CLIENT_SECRET environment variable is required");

var storageRoot = Environment.GetEnvironmentVariable("STORAGE_ROOT") ?? "./storage";
var sqliteConnectionString = Environment.GetEnvironmentVariable("SQLITE_CONN_STRING") ?? "Data Source=./App_Data/app.db";
var maxUploadBytes = long.Parse(Environment.GetEnvironmentVariable("MAX_UPLOAD_BYTES") ?? (100 * 1024 * 1024).ToString());
var fileRetentionDays = int.Parse(Environment.GetEnvironmentVariable("FILE_RETENTION_DAYS") ?? "30");
var cookieName = Environment.GetEnvironmentVariable("COOKIE_NAME") ?? ".OneTimeShare.Auth";
var cookieSecure = bool.Parse(Environment.GetEnvironmentVariable("COOKIE_SECURE") ?? (builder.Environment.IsProduction() ? "true" : "false"));

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(sqliteConnectionString));

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.Cookie.Name = cookieName;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = cookieSecure ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.LoginPath = "/auth/login";
    options.LogoutPath = "/auth/logout";
})
.AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
{
    options.ClientId = googleClientId;
    options.ClientSecret = googleClientSecret;
    options.CallbackPath = "/signin-google";
    options.SaveTokens = true;
});

builder.Services.AddScoped<IStorageService, LocalFileStorageService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IFileService, FileService>();

builder.Services.AddHostedService<CleanupHostedService>();

builder.Services.Configure<StorageOptions>(options =>
{
    options.StorageRoot = storageRoot;
});

builder.Services.Configure<OneTimeShare.Web.Models.Options.FileOptions>(options =>
{
    options.MaxUploadBytes = maxUploadBytes;
    options.RetentionDays = fileRetentionDays;
});

builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = maxUploadBytes;
});

builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = maxUploadBytes;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.EnsureCreated();
}

Directory.CreateDirectory(storageRoot);

app.Run();