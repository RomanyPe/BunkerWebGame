using System.Collections.Concurrent;
using BunkerGameWeb.Models;

namespace BunkerGameWeb
{
    public class RoomManager : IDisposable
    {
        private readonly ConcurrentDictionary<string, GameManager> _rooms = new();
        private readonly ConcurrentDictionary<string, List<(int PlayerId, string SessionKey)>> _roomPlayers = new();
        private readonly ConcurrentDictionary<string, string> _roomPasswords = new();
        private readonly ConcurrentDictionary<string, DateTime> _lastActivity = new();
        private readonly ConcurrentDictionary<string, string> _playerRoom = new(); // sessionKey -> roomId
        private readonly Timer _cleanupTimer;

        private const int MaxRooms = 50; // Максимум комнат на сервере

        private const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        public RoomManager()
        {
            _cleanupTimer = new Timer(CleanupEmptyRooms, null, TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(15));
        }

        public void RemovePlayerFromRoom(string roomId, int playerId)
        {
            if (_roomPlayers.TryGetValue(roomId, out var players))
            {
                var toRemove = players.FirstOrDefault(p => p.PlayerId == playerId);
                if (toRemove.PlayerId != 0)
                {
                    players.Remove(toRemove);
                    Console.WriteLine($"[ROOM] Игрок {playerId} удалён из списка комнаты {roomId}");
                }
            }
        }

        // Создать новую комнату
        public string CreateRoom(string sessionKey, string roomName, string password = "")
        {
            if (_rooms.Count >= MaxRooms)
            {
                Console.WriteLine($"[ROOM] Достигнут лимит комнат ({MaxRooms})");
                return string.Empty;
            }
            // ✅ Проверяем, не создавал ли уже этот пользователь комнату
            if (_playerRoom.TryGetValue(sessionKey, out string? existingRoomId))
            {
                if (_rooms.ContainsKey(existingRoomId))
                {
                    Console.WriteLine($"[ROOM] Пользователь {sessionKey} уже создал комнату {existingRoomId}");
                    return existingRoomId; // Возвращаем существующую комнату
                }
                else
                {
                    _playerRoom.TryRemove(sessionKey, out _);
                }
            }

            string roomId = GenerateRoomId();

            var gameManager = new GameManager();
            _rooms[roomId] = gameManager;
            _roomPlayers[roomId] = [];
            _lastActivity[roomId] = DateTime.UtcNow;

            // Сохраняем связь пользователя с комнатой
            _playerRoom[sessionKey] = roomId;

            if (!string.IsNullOrEmpty(password))
            {
                _roomPasswords[roomId] = password;
            }

            Console.WriteLine($"[ROOM] Комната {roomId} создана пользователем {sessionKey}");
            return roomId;
        }
        // Подключиться к комнате
        public (bool success, string message) JoinRoom(string roomId, int playerId, string sessionKey, string password = "")
        {
            if (!_rooms.TryGetValue(roomId, out GameManager? game))
                return (false, "Комната не найдена");

            // Проверка пароля
            if (_roomPasswords.TryGetValue(roomId, out string? roomPassword) && roomPassword != password)
                return (false, "Неверный пароль");

            if (game.IsGameStarted)
                return (false, "Игра уже началась");

            // Проверяем, нет ли уже игрока с таким sessionKey
            if (_roomPlayers.TryGetValue(roomId, out var players))
            {
                var existing = players.FirstOrDefault(p => p.SessionKey == sessionKey);
                if (existing.PlayerId != 0)
                {
                    // Это тот же самый игрок! Обновляем запись
                    players.Remove(existing);
                    players.Add((playerId, sessionKey));
                    _lastActivity[roomId] = DateTime.UtcNow;
                    Console.WriteLine($"[ROOM] Игрок {playerId} (сессия {sessionKey}) переподключился");
                    return (true, "Переподключено");
                }
            }

            // Новый игрок
            if (!_roomPlayers.ContainsKey(roomId))
                _roomPlayers[roomId] = [];

            _roomPlayers[roomId].Add((playerId, sessionKey));
            _lastActivity[roomId] = DateTime.UtcNow;

            Console.WriteLine($"[ROOM] Новый игрок {playerId} (сессия {sessionKey}) подключился к комнате {roomId}");
            return (true, "Подключено");
        }

        // Отключиться от комнаты (только по кнопке выхода)
        public void LeaveRoom(string roomId, int playerId, string sessionKey)
        {
            if (!_rooms.TryGetValue(roomId, out GameManager? game)) return;

            // Удаляем игрока из активных подключений комнаты
            if (_roomPlayers.TryGetValue(roomId, out var players))
            {
                var toRemove = players.FirstOrDefault(p => p.PlayerId == playerId && p.SessionKey == sessionKey);
                if (toRemove.PlayerId != 0)
                {
                    players.Remove(toRemove);
                    Console.WriteLine($"[ROOM] Игрок {playerId} (сессия {sessionKey}) покинул комнату {roomId}");
                }
            }

            // ✅ ПРОВЕРКА: Если вышел владелец (первый игрок) - удаляем комнату
            var isOwner = game.ArrayPlayers.Count > 0 && game.ArrayPlayers[0].Id == playerId;

            if (isOwner)
            {
                Console.WriteLine($"[ROOM] Владелец комнаты {roomId} вышел. Комната будет удалена");
                RemoveRoom(roomId);
                return;
            }

            // Если игра не начата и в комнате нет игроков - удаляем комнату
            if (!game.IsGameStarted && _roomPlayers.TryGetValue(roomId, out var activePlayers) && activePlayers.Count == 0)
            {
                RemoveRoom(roomId);
                Console.WriteLine($"[ROOM] Комната {roomId} удалена (нет игроков)");
            }

            _lastActivity[roomId] = DateTime.UtcNow;
        }

        // Получить игру комнаты
        public GameManager? GetGame(string roomId)
        {
            if (_rooms.TryGetValue(roomId, out var game))
            {
                _lastActivity[roomId] = DateTime.UtcNow;
                return game;
            }
            return null;
        }

        // Получить список ID игроков комнаты
        public List<int> GetPlayers(string roomId)
        {
            return [.. _roomPlayers.GetValueOrDefault(roomId, []).Select(p => p.PlayerId)];
        }

        // Получить список игроков с сессиями
        public List<(int PlayerId, string SessionKey)> GetPlayersWithSessions(string roomId)
        {
            return _roomPlayers.GetValueOrDefault(roomId, []);
        }

        // Удалить комнату
        public void RemoveRoom(string roomId)
        {
            _rooms.TryRemove(roomId, out _);
            _roomPlayers.TryRemove(roomId, out _);
            _roomPasswords.TryRemove(roomId, out _);
            _lastActivity.TryRemove(roomId, out _);
            Console.WriteLine($"[ROOM] Комната {roomId} полностью удалена");
        }

        // Получить список всех активных комнат
        public List<RoomInfo> GetActiveRooms()
        {
            return [.. _rooms.Select(r => new RoomInfo
            {
                RoomId = r.Key,
                PlayerCount = _roomPlayers.GetValueOrDefault(r.Key)?.Count ?? 0,
                IsStarted = r.Value.IsGameStarted,
                HasPassword = _roomPasswords.ContainsKey(r.Key)
            })];
        }

        // Получить ID комнаты игрока
        public string? GetPlayerRoom(int playerId)
        {
            return _roomPlayers
                .FirstOrDefault(r => r.Value.Any(p => p.PlayerId == playerId))
                .Key;
        }

        // Проверить, в комнате ли игрок
        public bool IsPlayerInRoom(string roomId, int playerId)
        {
            if (!_roomPlayers.TryGetValue(roomId, out var players))
                return false;

            return players.Any(p => p.PlayerId == playerId);
        }

        // Автоматическая очистка пустых и неактивных комнат
        public void CleanupEmptyRooms(object? state)
        {
            var now = DateTime.UtcNow;
            var roomsToRemove = new List<string>();

            foreach (var room in _rooms)
            {
                var roomId = room.Key;
                var game = room.Value;

                // Проверяем последнюю активность
                if (_lastActivity.TryGetValue(roomId, out var lastActivity))
                {
                    var inactiveTime = now - lastActivity;

                    // Если игра не начата и неактивна более 15 минут - удаляем
                    if (!game.IsGameStarted && inactiveTime.TotalMinutes > 15)
                    {
                        roomsToRemove.Add(roomId);
                        Console.WriteLine($"[CLEANUP] Комната {roomId} удалена (неактивна {inactiveTime.TotalMinutes:F1} мин)");
                        continue;
                    }

                    // Если игра начата и неактивна более 60 минут - удаляем
                    if (game.IsGameStarted && inactiveTime.TotalMinutes > 60)
                    {
                        roomsToRemove.Add(roomId);
                        Console.WriteLine($"[CLEANUP] Комната {roomId} удалена (игра зависла на {inactiveTime.TotalMinutes:F1} мин)");
                        continue;
                    }
                }

                // Проверяем количество игроков
                var playerCount = _roomPlayers.GetValueOrDefault(roomId)?.Count ?? 0;
                if (playerCount == 0 && !game.IsGameStarted)
                {
                    roomsToRemove.Add(roomId);
                    Console.WriteLine($"[CLEANUP] Комната {roomId} удалена (нет игроков)");
                }
            }

            // Удаляем найденные комнаты
            foreach (var roomId in roomsToRemove)
            {
                RemoveRoom(roomId);
            }

            if (roomsToRemove.Count > 0)
            {
                Console.WriteLine($"[CLEANUP] Удалено комнат: {roomsToRemove.Count}. Активных: {_rooms.Count}");
            }
        }

        // Генерация ID комнаты
        private static string GenerateRoomId()
        {
            var random = new Random();
            return new string([.. Enumerable.Range(0, 6).Select(_ => Chars[random.Next(Chars.Length)])]);
        }

        // Принудительная очистка (для тестов/рестарта)
        public void ClearAllRooms()
        {
            _rooms.Clear();
            _roomPlayers.Clear();
            _roomPasswords.Clear();
            _lastActivity.Clear();
            Console.WriteLine("[CLEANUP] Все комнаты удалены");
        }

        public void Dispose()
        {
            _cleanupTimer?.Dispose();
            GC.SuppressFinalize(this);
        }
        public void KickPlayerFromRoom(string roomId, string playerName)
        {
            if (!_rooms.TryGetValue(roomId, out var game))
            {
                Console.WriteLine($"Комната {roomId} не найдена");
                return;
            }

            var player = game.ArrayPlayers.FirstOrDefault(p => p.Name.Contains(playerName, StringComparison.OrdinalIgnoreCase));
            if (player == null)
            {
                Console.WriteLine($"Игрок {playerName} не найден в комнате {roomId}");
                return;
            }

            // Удаляем из игры
            game.ArrayPlayers.Remove(player);

            // Удаляем из активных подключений
            if (_roomPlayers.TryGetValue(roomId, out var players))
            {
                var toRemove = players.FirstOrDefault(p => p.PlayerId == player.Id);
                if (toRemove.PlayerId != 0)
                {
                    players.Remove(toRemove);
                }
            }

            Console.WriteLine($"Игрок {player.Name} (ID {player.Id}) кикнут из комнаты {roomId}");

            // Если комната опустела и игра не начата - удаляем комнату
            if (!game.IsGameStarted && players.Count == 0)
            {
                RemoveRoom(roomId);
            }
        }
    }

    public class RoomInfo
    {
        public string RoomId { get; set; } = string.Empty;
        public int PlayerCount { get; set; }
        public bool IsStarted { get; set; }
        public bool HasPassword { get; set; }
    }
}