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
            case "debugroom":
            case "droom":
                CreateDebugRoom();
                break;
            case "transfer":
            case "transferadmin":
            case "giveowner":
            case "adm":
                if (parts.Length > 2)
                {
                    var roomId = parts[1].ToUpper();
                    var targetPlayerName = parts[2];
                    TransferAdmin(roomId, targetPlayerName);
                }
                else
                {
                    Console.WriteLine("Использование: transfer <ID комнаты> <имя игрока>");
                    Console.WriteLine("Пример: transfer ABC123 Саня Штрих");
                }
                break;
            default:
                Console.WriteLine($"Неизвестная команда: {cmd}. Введите 'help' для списка команд");
                break;
        }
    }
    private void TransferAdmin(string roomId, string targetPlayerName)
    {
        var game = _roomManager.GetGame(roomId);
        if (game == null)
        {
            Console.WriteLine($"❌ Комната {roomId} не найдена");
            return;
        }

        var targetPlayer = game.ArrayPlayers.FirstOrDefault(p =>
            p.Name.Contains(targetPlayerName, StringComparison.OrdinalIgnoreCase));

        if (targetPlayer == null)
        {
            Console.WriteLine($"❌ Игрок '{targetPlayerName}' не найден");
            return;
        }

        // Просто перемещаем владельца в начало списка
        var currentOwner = game.ArrayPlayers.FirstOrDefault(p => p.Id == 0);
        if (currentOwner != null && currentOwner.Id != targetPlayer.Id)
        {
            // Меняем ID (0 становится у целевого игрока)
            int oldOwnerId = currentOwner.Id;
            int targetId = targetPlayer.Id;

            currentOwner.Id = targetId;
            targetPlayer.Id = oldOwnerId;

            Console.WriteLine($"Права администратора переданы игроку {targetPlayer.Name}");
            Console.WriteLine($"Новый ID владельца: {targetPlayer.Id}");
        }
    }
    private void CreateDebugRoom()
    {
        var sessionKey = Guid.NewGuid().ToString();
        string roomId = _roomManager.CreateRoom(sessionKey, "ТЕСТ");

        var game = _roomManager.GetGame(roomId);
        if (game != null)
        {
            for (int i = 0; i < 6; i++)
            {
                var playerSessionKey = Guid.NewGuid().ToString();
                var playerId = game.AddAndInitializePlayer(playerSessionKey);
                _roomManager.JoinRoom(roomId, playerId, playerSessionKey);
                Console.WriteLine($"Добавлен тестовый игрок {playerId}");
            }

            foreach (var player in game.ArrayPlayers)
            {
                player.IsReady = true;
            }

            Console.WriteLine($"Тестовая комната создана: {roomId}");
            Console.WriteLine($"Подключиться по адресу: http://localhost:5000/game/{roomId}");
        }
    }
    private static void ShowHelp()
    {
        Console.WriteLine("\n=== Доступные команды ===");
        Console.WriteLine("rooms / list                                             - показать все комнаты");
        Console.WriteLine("players <ID>                                             - показать игроков в комнате");
        Console.WriteLine("clean / cleanup                                          - принудительная очистка пустых комнат");
        Console.WriteLine("delete / remove <ID>                                     - удалить конкретную комнату");
        Console.WriteLine("kick <ID> <имя>                                          - кикнуть игрока из комнаты");
        Console.WriteLine("debugroom / droom                                        - автоматическое подготовка к тесту");
        Console.WriteLine("adm / transfer / transferadmin / giveowner <ID> <имя>    - выдача прав администратора");
        Console.WriteLine("help / ?                                                 - показать эту справку");
        Console.WriteLine("exit / quit                                              - выйти\n");
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
            var password = room.HasPassword ? "Open" : "Close";
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
            var status = player.IsReady ? "Готов" : "Не готов";
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