using Microsoft.AspNetCore.DataProtection;
using NLog;
using NLog.Web;
using PlayingCards.Durak.Blazor.Components;
using PlayingCards.Durak.Blazor.Services;
using PlayingCards.Durak.Server;
using PlayingCards.Server.Core;

var logger = LogManager.Setup().LoadConfigurationFromFile("nlog.config").GetCurrentClassLogger();
logger.Debug("init main");

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<TableHolder>();
builder.Services.AddSingleton<IBackgroundProcessor>(sp => sp.GetRequiredService<TableHolder>());
builder.Services.AddSingleton<BuildInfo>();
builder.Services.AddHostedService<BackgroundExecutorService>();
builder.Services.AddScoped<PlayerSession>();

var dataProtectionKeysPath = builder.Configuration["DataProtection:KeysPath"];
if (!string.IsNullOrWhiteSpace(dataProtectionKeysPath))
{
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));
}

builder.Services.AddAntiforgery(options => options.SuppressXFrameOptionsHeader = true);

builder.Logging.ClearProviders();
builder.Host.UseNLog();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
