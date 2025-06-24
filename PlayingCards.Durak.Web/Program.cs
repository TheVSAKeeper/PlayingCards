using NLog;
using NLog.Web;
using PlayingCards.Durak.Web;
using PlayingCards.Durak.Web.Business;
using PlayingCards.Durak.Web.Middlewares;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
logger.Debug("init main");

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddSingleton<TableHolder>();
builder.Services.AddHostedService<BackgroundExecutorService>();

builder.Logging.ClearProviders();
builder.Logging.SetMinimumLevel(LogLevel.Trace);
builder.Host.UseNLog();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");

app.UseMiddleware<ExceptionMiddleware>();
app.Run();
