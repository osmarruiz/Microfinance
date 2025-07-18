using System.Text.Json.Serialization;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.SQLAdmin.v1beta4;
using Microfinance.Data;
using Microfinance.Helpers;
using Microfinance.Middleware;
using Microfinance.Services;
using Microfinance.Workers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

QuestPDF.Settings.License = LicenseType.Community;

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options => 
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    // Obtiene el puerto de la variable de entorno PORT, o usa 8080 como fallback
    var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
    serverOptions.ListenAnyIP(int.Parse(port)); // Escucha en cualquier IP en el puerto especificado
});

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();
builder.Services.AddControllersWithViews()
    .AddRazorRuntimeCompilation(); 
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<IEmailSender, SendGridEmailSender>();
builder.Services.AddHostedService<LateInterestCalculatorService>();
builder.Services.AddHostedService<LoanStatusBackgroundService>();
builder.Services.AddHostedService<InstallmentStatusBackgroundService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<ReportService>();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login"; // O la ruta de tu vista de login si no usas las vistas por defecto de Identity
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.SlidingExpiration = true;
});

var googleCloudConfig = builder.Configuration.GetSection("GoogleCloud");
var projectId = googleCloudConfig["ProjectId"];
var serviceAccountKeyFilePath = googleCloudConfig["ServiceAccountKeyFilePath"];

GoogleCredential credential;
using (var stream = new FileStream(serviceAccountKeyFilePath, FileMode.Open, FileAccess.Read))
{
    
    credential = GoogleCredential.FromStream(stream)
        .CreateScoped(SQLAdminService.Scope.CloudPlatform); 
}

builder.Services.AddSingleton<ApplicationStatusService>();
builder.Services.AddSingleton(new SQLAdminService(new BaseClientService.Initializer()
{
    HttpClientInitializer = credential,
    ApplicationName = "MyCloudSqlMvcApp"
}));

builder.Services.AddScoped<CloudSqlService>();
builder.Services.AddHostedService<CloudSqlOperationMonitor>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseMaintenanceMode();

// using (var scope = app.Services.CreateScope())
// {
//     var services = scope.ServiceProvider;
//     await IdentitySeeder.CrearRoles(services);
// }

app.UseRouting();

// ... el resto de tu configuración
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
