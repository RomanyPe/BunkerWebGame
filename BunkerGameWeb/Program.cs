using BunkerGameWeb.Components;
using System.Net;
using System.Net.Sockets;

namespace BunkerGameWeb;
public partial class Program
{
    
    private static void Main(string[] args)
    {
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
            var consoleCommands = new ConsoleCommands(app.Services.GetRequiredService<RoomManager>(), builder.Configuration.GetValue("Http_Ports", 5000));
            Console.WriteLine("\n=== АДМИНСКАЯ КОНСОЛЬ ЗАПУЩЕНА ===");
            Console.WriteLine("Введите 'help' для списка команд\n");
        });

        Console.WriteLine("\n═══════════════════════════════════════");
        Console.WriteLine("         БУНКЕР - СЕРВЕР ЗАПУЩЕН");
        Console.WriteLine("═══════════════════════════════════════");
        Console.WriteLine("для запуска инфомационного меню введите guide");

        Console.WriteLine("\n Нажмите Ctrl+C для остановки сервера");
        Console.WriteLine("═══════════════════════════════════════\n");
        Console.WriteLine(@"


        
");
        app.Run();
    }
}