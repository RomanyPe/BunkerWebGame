using System.Net;
using System.Net.Sockets;
using System.Reflection;
using static System.Net.WebRequestMethods;

namespace BunkerGameWeb;

public class ConsoleCommands
{
    private readonly RoomManager _roomManager;
    private readonly Timer _timer;
    private bool _isRunning = true;
    public int httpPort;
    public ConsoleCommands(RoomManager roomManager, int _hhtp)
    {
        _roomManager = roomManager;
        httpPort = _hhtp;
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
            case "guide":
                SnowGuide();
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
        string roomId = _roomManager.CreateRoom(sessionKey);

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

    private void SnowGuide()
    {
        Console.WriteLine(@"
============================================================
            LOCAL BUNKER WEB GAME SERVER GUIDE
============================================================

Если вы запускаете проект в первый раз то это как раз для вас,
на данный момент сервер не работает публично а в области локальной сети 
то есть в области 1 роутера или любого друго маршрутизатора, при возникновение проблемы с серверо

СРАЗУ ОТВЕЧАЮ НА ГЛАВНЫЕ ВОПРОСЫ И ЖАЛОБЫ:
    1. Почему у меня пропала комната?
        На сервере стоит работает быстрая система которая 
        следит за комнатами и если комната живет час, 
        но в нее никто не играет она удаляеться автоматически
    2.Какие его требования к системе?
        Я честно пытался сделать все очень оптимизирована даже там где это уже выглдит бесполезно,
        но как факт сервер почти не исползует процессор 
        только ОЗУ и если игроков будет реально много может занимать от 300 до 600 МБ
    3.Как поменять тему игры?
        В папке проекта будет лежать папка Themes,
        внутри нее файлы расширения .bunk это по сути файлы .txt, 
        но так как я эксперементирую с проектом и своими возможностями
        создал данный формат файла, если его открыт то там будет 
        [*название характеристики*] и с каждой новой строки
        будет сама характеристика, вам позволено как создать новую характеристику 
        (систему полностью позволяет вам создавать любую длину для каждой характеристики).
        Главное чтобы каждая новая характеристика была с новой строки
    4.Как приглосить друга в игру?
        Есть 2 ответа которые зависят от условий, если твой друг не подключен к общему интернету 
        (компьютер где стоит данный сервер считается хостом) 
        к которому подключен хост то либо вы подключаетесь к общему интернету,
        либо не играете так как у нас находится сервер, но у него нет выхода в интернет.
        А если у вас есть возможность создать туннель или использовать системы 
        которые обьединяют ваши интернет соединения в одну (например Radmin) 
        то вам нужно будет скопироть ip друга который виден в сервисе а потом ввести порт:
        http://xxxxxxxxxx:5000
    5.Как получить ссылку на игру?
        Способ 1:
            введите комбинацию Win + R введите cmd ,
            внтури комнадной строки введите ipconfig, 
            чаще всего IPv4 по типу 192.168.0.1 (это только пример если он совпал это случайность)
            ссылка будет выглядить вот так: http://192.168.0.1:5000
        Способ 2:
            Ниже буду ваши готовые ip адреса которые вы можете скопировать 

------------------------------------------------------------

============================================================
");
        // Локальный доступ
        Console.WriteLine($"Локальный доступ:   http://localhost:{httpPort}");

        // Сетевые адреса
        var hostName = Dns.GetHostName();
        var ipAddresses = Dns.GetHostEntry(hostName)
            .AddressList
            .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork)
            .ToList();

        if (ipAddresses.Count != 0)
        {
            Console.WriteLine("\nДля подключения с других устройств:");
            foreach (var ip in ipAddresses)
            {
                Console.WriteLine($"   http://{ip}:{httpPort}");
            }
        }
        else
        {
            Console.WriteLine("\nСетевые адреса не найдены. Проверьте подключение к сети.");
        }
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