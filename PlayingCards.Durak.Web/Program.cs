using NLog;
using NLog.Web;
using PlayingCards.Durak.Web.Business;
using PlayingCards.Durak.Web.Hubs;
using PlayingCards.Durak.Web.Middlewares;

var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
logger.Debug("init main");

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddControllersWithViews();

    var signalRConfig = new SignalRConfiguration();
    builder.Configuration.GetSection("SignalR").Bind(signalRConfig);
    builder.Services.AddSingleton(signalRConfig);

    builder.Services.AddSignalR(options =>
    {
        options.EnableDetailedErrors = signalRConfig.EnableDetailedErrors;
        options.MaximumReceiveMessageSize = signalRConfig.MaxMessageSize;
        options.StreamBufferCapacity = signalRConfig.StreamBufferCapacity;
        options.ClientTimeoutInterval = TimeSpan.FromSeconds(signalRConfig.ConnectionTimeoutSeconds);
        options.KeepAliveInterval = TimeSpan.FromSeconds(signalRConfig.KeepAliveIntervalSeconds);
    });

    if (signalRConfig.EnableCors)
    {
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("SignalRCorsPolicy", policy =>
            {
                policy.WithOrigins(signalRConfig.CorsOrigins.Length > 0 ? signalRConfig.CorsOrigins : ["*"])
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });
    }

    builder.Services.AddSingleton<TableHolder>();
    builder.Services.AddHostedService<BackgroundExecutorService>();

    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    var app = builder.Build();

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();

    var appSignalRConfig = app.Services.GetRequiredService<SignalRConfiguration>();

    if (appSignalRConfig.EnableCors)
    {
        app.UseCors("SignalRCorsPolicy");
    }

    app.UseRouting();

    app.UseAuthorization();

    app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");

    app.MapHub<GameHub>(appSignalRConfig.HubPath);

    app.UseMiddleware<ExceptionMiddleware>();
    app.Run();
}
catch (Exception exception)
{
    logger.Error(exception, "Stopped program because of exception");
    throw;
}
finally
{
    LogManager.Shutdown();
}
