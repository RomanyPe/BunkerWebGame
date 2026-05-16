using BunkerGameWeb;
using BunkerGameWeb.Components;
using Microsoft.AspNetCore.Identity;


var builder = WebApplication.CreateBuilder(args);
// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var roomManager = new RoomManager();
builder.Services.AddSingleton(roomManager);
builder.Services.AddSingleton<GameManager>();
builder.WebHost.UseStaticWebAssets();
//builder.WebHost.UseUrls("http://*:7234");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
//app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// ✅ ЗАПУСК КОНСОЛЬНЫХ КОМАНД
// Чтобы консоль не блокировала основной поток
_ = Task.Run(() =>
{
    var consoleCommands = new ConsoleCommands(app.Services.GetRequiredService<RoomManager>());
    Console.WriteLine("\n=== АДМИНСКАЯ КОНСОЛЬ ЗАПУЩЕНА ===");
    Console.WriteLine("Введите 'help' для списка команд\n");
});
app.Run();
