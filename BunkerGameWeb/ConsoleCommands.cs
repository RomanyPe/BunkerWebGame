using System.Reflection;

namespace BunkerGameWeb;

public class ConsoleCommands
{
    private readonly RoomManager _roomManager;
    private readonly Timer _timer;
    private bool _isRunning = true;

    public ConsoleCommands(RoomManager roomManager)
    {
        _roomManager = roomManager;
        _timer = new Timer(_ => CheckInput(), null, 100, 100);
    }

    private void CheckInput()
    {
        if (!_isRunning) return;

        if (Console.KeyAvailable)
        {
            var input = Console.ReadLine()?.Trim().ToLower();
            if (!string.IsNullOrEmpty(input))
            {
                ProcessCommand(input);
            }
        }
    }

    private void ProcessCommand(string command)
    {
        var parts = command.Split(' ');
        var cmd = parts[0];

        switch (cmd)
        {
            case "help":
            case "?":
                ShowHelp();
                break;

            case "rooms":
            case "list":
                ShowRooms();
                break;

            case "clean":
            case "cleanup":
                _roomManager.CleanupEmptyRooms(null);
                Console.WriteLine("[ADMIN] Принудительная очистка выполнена");
                break;

            case "delete":
            case "remove":
                if (parts.Length > 1)
                {
                    var roomId = parts[1].ToUpper();
                    _roomManager.RemoveRoom(roomId);
                    Console.WriteLine($"[ADMIN] Комната {roomId} удалена");
                }
                else
                {
                    Console.WriteLine("Использование: delete <ID комнаты>");
                }
                break;

            case "kick":
                if (parts.Length > 2)
                {
                    var roomId = parts[1].ToUpper();
                    var playerName = parts[2];
                    _roomManager.KickPlayerFromRoom(roomId, playerName);
                }
                else
                {
                    Console.WriteLine("Использование: kick <ID комнаты> <имя игрока>");
                }
                break;

            case "players":
                if (parts.Length > 1)
                {
                    var roomId = parts[1].ToUpper();
                    ShowPlayers(roomId);
                }
                else
                {
                    Console.WriteLine("Использование: players <ID комнаты>");
                }
                break;

            case "exit":
            case "quit":
                _isRunning = false;
                break;

            default:
                Console.WriteLine($"Неизвестная команда: {cmd}. Введите 'help' для списка команд");
                break;
        }
    }

    private static void ShowHelp()
    {
        Console.WriteLine("\n=== Доступные команды ===");
        Console.WriteLine("rooms / list     - показать все комнаты");
        Console.WriteLine("players <ID>     - показать игроков в комнате");
        Console.WriteLine("clean / cleanup  - принудительная очистка пустых комнат");
        Console.WriteLine("delete / remove <ID> - удалить конкретную комнату");
        Console.WriteLine("kick <ID> <имя>  - кикнуть игрока из комнаты");
        Console.WriteLine("help / ?         - показать эту справку");
        Console.WriteLine("exit / quit      - выйти\n");
    }

    private void ShowRooms()
    {
        var rooms = _roomManager.GetActiveRooms();

        if (rooms.Count == 0)
        {
            Console.WriteLine("Нет активных комнат");
            return;
        }

        Console.WriteLine($"\n=== Активные комнаты ({rooms.Count}) ===");
        foreach (var room in rooms)
        {
            var status = room.IsStarted ? "Игра идёт" : "Ожидание";
            var password = room.HasPassword ? "🔒" : "🔓";
            Console.WriteLine($"  {password} {room.RoomId} - {room.PlayerCount} игроков - {status}");
        }
        Console.WriteLine();
    }

    private void ShowPlayers(string roomId)
    {
        var game = _roomManager.GetGame(roomId);
        if (game == null)
        {
            Console.WriteLine($"Комната {roomId} не найдена");
            return;
        }

        Console.WriteLine($"\n=== Игроки в комнате {roomId} ===");
        foreach (var player in game.ArrayPlayers)
        {
            var status = player.IsReady ? "✓ Готов" : "○ Не готов";
            var eliminated = player.IsEliminated ? " [Исключён]" : "";
            Console.WriteLine($"  {player.Id}: {player.Name} - {status}{eliminated}");
        }
        Console.WriteLine();
    }

    public void Stop()
    {
        _isRunning = false;
        _timer?.Dispose();
    }
}