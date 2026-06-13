using NLog;
using NLog.Web;
using PlayingCards.Durak.Blazor.Components;
using PlayingCards.Durak.Blazor.Services;
using PlayingCards.Durak.Server;

var logger = LogManager.Setup().LoadConfigurationFromFile("nlog.config").GetCurrentClassLogger();
logger.Debug("init main");

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<TableHolder>();
builder.Services.AddHostedService<BackgroundExecutorService>();
builder.Services.AddScoped<PlayerSession>();

builder.Logging.ClearProviders();
builder.Host.UseNLog();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
