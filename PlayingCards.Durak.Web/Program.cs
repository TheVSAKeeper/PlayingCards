using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using PlayingCards.Durak.Web.Business;
using PlayingCards.Durak.Web.SignalR.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddSingleton<TableHolder>();
builder.Services.AddHostedService<BackgroundExecutorService>();
builder.Services.AddSignalR();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<ChatHub>("/chatHub");
app.MapHub<GameHub>("/gameHub");

app.Run();
