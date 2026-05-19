using BunkerGameWeb.Components.Pages;
using BunkerGameWeb.Helpers;
using BunkerGameWeb.Models;
using System.Buffers;
using System.Reflection.PortableExecutable;
using System.Runtime.Intrinsics.Arm;

namespace BunkerGameWeb
{
    public class GameManager
    {

        public List<Player> ArrayPlayers { get; set; } = [];

        private int _idCounter = -1;
        public bool IsGameStarted { get; private set; } = false;
        public bool _isStarting = false;

        public event Action<string>? OnGameEnded;
        // Индекс игрока, который ходит сейчас (в списке PlayerIds)
        public int CurrentPlayerIndex { get; private set; } = 0;

        public string CurrentTheme { get; set; } = "FirstTheme";
        // Свойство, возвращающее ID текущего ходящего
        public int CurrentTurnPlayerId
        {
            get
            {
                if (ArrayPlayers == null || ArrayPlayers.Count == 0)
                    return -1;

                if (CurrentPlayerIndex < 0 || CurrentPlayerIndex >= ArrayPlayers.Count)
                {
                    // Сбрасываем индекс на 0 если он некорректный
                    CurrentPlayerIndex = 0;

                    // Если после сброса список пуст - возвращаем -1
                    if (ArrayPlayers.Count == 0)
                        return -1;
                }

                return ArrayPlayers[CurrentPlayerIndex].Id;
            }
        }

        public int GameRounds = 0;

        public BunkerStats BunkerStats;

        public Dictionary<int, int> Votes = [];
        public HashSet<int> PlayersWhoMovedThisRound = [];

        public bool IsVotingActive { get; set; }

        public ConfigCharacterName ConfigCharacterName = new();
        public ConfigCharacterBiologicalSex ConfigCharacterBiologicalSex = new();
        public ConfigCharacterAge ConfigCharacterAge = new();
        public ConfigCharacterProfession ConfigCharacterProfession = new();
        public ConfigCharacterHealth ConfigCharacterHealth = new();
        public ConfigCharacterBodyBuild ConfigCharacterBodyBuild = new();
        public ConfigCharacterHobby ConfigCharacterHobby = new();
        public ConfigCharacterPhobia ConfigCharacterPhobia = new();
        public ConfigCharacterInventory ConfigCharacterInventory = new();
        public ConfigCharacterTrait ConfigCharacterTrait = new();
        public ConfigCharacterAdditionalInformation ConfigCharacterAdditionalInformation = new();
        public ConfigCharacterSpecialCondition ConfigCharacterSpecialCondition = new();

        public ConfigCharacterBaggage ConfigCharacterBaggage = new();
        public ConfigCharacterKnowledge ConfigCharacterKnowledge = new();
        public ConfigCharacterSecret ConfigCharacterSecret = new();
        public ConfigCharacterReproduction ConfigCharacterReproduction = new();
        public ConfigCharacterVision ConfigCharacterVision = new();

        public ConfigCharacterEquipment ConfigCharacterEquipment = new(); 
        public ConfigCharacterRelation ConfigCharacterRelation = new(); 

        public CatastropheName CatastropheName = new();

        public event Action? OnNotify; // Событие для SignalR

        public void UpdateAll() => OnNotify?.Invoke();
        public List<string> AvailableThemes;
        // GameManager.cs
        private Timer? _cleanupTimer;

        public GameManager()
        {
            // Запускаем таймер каждые 30 секунд
            AvailableThemes = ThemeManager.GetAvailableThemes();
            _cleanupTimer = new Timer(_ => RemoveDisconnectedPlayers(), null, TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(2));
        }

        private void RemoveDisconnectedPlayers()
        {
            if (IsGameStarted && ArrayPlayers.Count > 0)
            {
                Console.WriteLine("[CLEANUP] Игра идёт, пропускаем удаление игроков");
                return;
            }

            var timeout = TimeSpan.FromMinutes(1); // даём 45 секунд на переподключение
            var now = DateTime.UtcNow;
            var toRemove = ArrayPlayers
                .Where(p => !p.IsConnected && now - p.LastSeenUtc > timeout && !p.IsEliminated)
                .ToList();

            foreach (var player in toRemove)
            {
                player.IsEliminated = true;   // выбывает из игры
                Console.WriteLine($"[CLEANUP] Игрок {player.Name} (ID {player.Id}) удалён за долгое отсутствие");
            }

            // Если после очистки живых игроков не осталось – игра заканчивается
            if (IsGameStarted && !ArrayPlayers.Any(p => !p.IsEliminated))
            {
                IsGameStarted = false;
                OnGameEnded?.Invoke("Все игроки отключились. Игра завершена.");
                UpdateAll();
            }
        }

        private static void FillUniqueIndices(Span<int> destination, int maxValue)
        {
            int count = destination.Length;
            if (count > maxValue) count = maxValue;

            // Берем временный массив из пула для перемешивания
            int[] tempIndices = ArrayPool<int>.Shared.Rent(maxValue);

            try
            {
                // Заполняем временный массив индексами от 0 до maxValue
                for (int i = 0; i < maxValue; i++)
                {
                    tempIndices[i] = i;
                }

                // Перемешиваем только нужную нам часть (Фишер-Йетс)
                // Использование Random.Shared избавляет от new Random()
                for (int i = 0; i < count; i++)
                {
                    int j = Random.Shared.Next(i, maxValue);
                    (tempIndices[i], tempIndices[j]) = (tempIndices[j], tempIndices[i]);
                }

                // Копируем перемешанные индексы в наш destination Span
                // tempIndices как Span позволяет использовать быстрый метод CopyTo
                tempIndices.AsSpan(0, count).CopyTo(destination);
            }
            finally
            {
                // Обязательно возвращаем массив в пул
                ArrayPool<int>.Shared.Return(tempIndices);
            }
        }
        public void GenerateAllCharactersWithoutAllocation()
        {
            int playerCount = ArrayPlayers.Count;
            Random rnd = Random.Shared; // Используем Shared, чтобы не аллоцировать `new Random()`
            ReloadTheme();
            // 1. Создаем буфер для уникальных индексов прямо НА СТЕКЕ текущего потока.
            // Выделение памяти занимает 0 наносекунд, нагрузка на GC = 0.
            // Если игроков больше 128 (что в Бункере невозможно), безопасно берем обычный массив.
            Span<int> indicesBuffer = playerCount <= 128 ? stackalloc int[playerCount] : new int[playerCount];

            // 2. ГЕНЕРИРУЕМ УНИКАЛЬНЫЕ ИНДЕКСЫ И СРАЗУ ПРИСВАИВАЕМ ИГРОКАМ
            // Мы повторно используем один и тот же буфер на стеке для каждого конфига!

            // Имена
            FillUniqueIndices(indicesBuffer, ConfigCharacterName.Text.Length);
            for (int i = 0; i < playerCount; i++)
                ArrayPlayers[i].Name = ConfigCharacterName.Text[indicesBuffer[i]];

            // Профессии
            FillUniqueIndices(indicesBuffer, ConfigCharacterProfession.Text.Length);
            for (int i = 0; i < playerCount; i++)
                ArrayPlayers[i].Profession = ConfigCharacterProfession.GetConfig(indicesBuffer[i], rnd);

            // Телосложение
            FillUniqueIndices(indicesBuffer, ConfigCharacterBodyBuild.Text.Length);
            for (int i = 0; i < playerCount; i++)
                ArrayPlayers[i].BodyBuild = ConfigCharacterBodyBuild.GetConfig(indicesBuffer[i]);

            // Хобби
            FillUniqueIndices(indicesBuffer, ConfigCharacterHobby.Text.Length);
            for (int i = 0; i < playerCount; i++)
                ArrayPlayers[i].Hobby = ConfigCharacterHobby.GetConfig(indicesBuffer[i], rnd);

            // Фобии
            FillUniqueIndices(indicesBuffer, ConfigCharacterPhobia.Text.Length);
            for (int i = 0; i < playerCount; i++)
                ArrayPlayers[i].Phobia = ConfigCharacterPhobia.GetConfig(indicesBuffer[i]);

            // Инвентарь
            FillUniqueIndices(indicesBuffer, ConfigCharacterInventory.Text.Length);
            for (int i = 0; i < playerCount; i++)
                ArrayPlayers[i].Inventory = ConfigCharacterInventory.GetConfig(indicesBuffer[i]);

            // Черты характера
            FillUniqueIndices(indicesBuffer, ConfigCharacterTrait.Text.Length);
            for (int i = 0; i < playerCount; i++)
                ArrayPlayers[i].Trait = ConfigCharacterTrait.GetConfig(indicesBuffer[i]);

            // Доп. информация
            FillUniqueIndices(indicesBuffer, ConfigCharacterAdditionalInformation.Text.Length);
            for (int i = 0; i < playerCount; i++)
                ArrayPlayers[i].AdditionalInformation = ConfigCharacterAdditionalInformation.GetConfig(indicesBuffer[i]);

            // Багаж
            FillUniqueIndices(indicesBuffer, ConfigCharacterBaggage.Text.Length);
            for (int i = 0; i < playerCount; i++)
                ArrayPlayers[i].Baggage = ConfigCharacterBaggage.GetConfig(indicesBuffer[i], rnd);

            // Знания
            FillUniqueIndices(indicesBuffer, ConfigCharacterKnowledge.Text.Length);
            for (int i = 0; i < playerCount; i++)
                ArrayPlayers[i].Knowledge = ConfigCharacterKnowledge.GetConfig(indicesBuffer[i], rnd);

            // Секрет
            FillUniqueIndices(indicesBuffer, ConfigCharacterSecret.Text.Length);
            for (int i = 0; i < playerCount; i++)
                ArrayPlayers[i].Secret = ConfigCharacterSecret.GetConfig(indicesBuffer[i]);

            // Рождаемость
            FillUniqueIndices(indicesBuffer, ConfigCharacterReproduction.Text.Length);
            for (int i = 0; i < playerCount; i++)
                ArrayPlayers[i].Reproduction = ConfigCharacterReproduction.GetConfig(indicesBuffer[i], rnd);

            // Зрение
            FillUniqueIndices(indicesBuffer, ConfigCharacterVision.Text.Length);
            for (int i = 0; i < playerCount; i++)
                ArrayPlayers[i].Vision = ConfigCharacterVision.GetConfig(indicesBuffer[i]);

            // Экипировка
            FillUniqueIndices(indicesBuffer, ConfigCharacterEquipment.Text.Length);
            for (int i = 0; i < playerCount; i++)
                ArrayPlayers[i].Equipment = ConfigCharacterEquipment.GetConfig(indicesBuffer[i]);

            // Отношения
            FillUniqueIndices(indicesBuffer, ConfigCharacterRelation.Text.Length);
            for (int i = 0; i < playerCount; i++)
                ArrayPlayers[i].Relation = ConfigCharacterRelation.GetConfig(indicesBuffer[i]);


            // 3. СБРОС СТАТУСОВ И ОЧИСТКА ХЭШ-ТАБЛИЦ (ZERO-ALLOCATION)
            for (int i = 0; i < playerCount; i++)
            {
                var player = ArrayPlayers[i];

                // Случайные неуникальные характеристики
                int indexAge = rnd.Next(100);
                player.Age = ConfigCharacterAge.GetConfig(indexAge);

                int indexBiologicalSex = rnd.Next(ConfigCharacterBiologicalSex.Text.Length);
                player.BiologicalSex = ConfigCharacterBiologicalSex.GetConfig(indexBiologicalSex, rnd);

                int indexSpecialCondition = rnd.Next(ConfigCharacterSpecialCondition.Text.Length);
                player.SpecialCondition = ConfigCharacterSpecialCondition.GetConfig(indexSpecialCondition, rnd);

                // Системные флаги
                player.IsConnected = true;
                player.LastSeenUtc = DateTime.UtcNow;
                player.IsReady = false;
                player.IsEliminated = false;
                player.IsWinner = false;
                player.IsSelectionConfirmed = false;
                player.CountNeedOpen = 0;
                player.CurrentOpenedCard = 0;

                // ЗАЩИТА: Вместо пересоздания HashSet ([]), инициализируем если null, 
                // и просто очищаем старые данные (.Clear()). Память переиспользуется!
                player.PendingOpenedTypes ??= [];
                player.PendingOpenedTypes.Clear();

                player.ListOpenedTypes ??= [];
                player.ListOpenedTypes.Clear();
            }

#if DEBUG
            // Этот лог скомпилируется и выведется ТОЛЬКО при локальной отладке.
            // На продакшене компилятор полностью сотрет эти строки для экономии памяти.
            Console.WriteLine("[STATS] Уникальные характеристики успешно назначены БЕЗ аллокаций");
#endif
        }

        public int AddAndInitializePlayer(string sessionKey = "")
        {
            if (IsGameStarted) return -1;

            // ПРОВЕРКА: Есть ли уже игрок с таким SessionKey?
            if (!string.IsNullOrEmpty(sessionKey))
            {
                var existingPlayer = ArrayPlayers.FirstOrDefault(p => p.SessionKey == sessionKey);
                if (existingPlayer != null)
                {
                    Console.WriteLine($"[LOG] Игрок уже существует с SessionKey {sessionKey}, возвращаем ID {existingPlayer.Id}");
                    return existingPlayer.Id;
                }
            }

            if (ArrayPlayers.Count == 0)
            {
                _idCounter = -1;
            }

            int newId = Interlocked.Increment(ref _idCounter);

            var newPlayer = new Player
            {
                Id = newId,
                SessionKey = sessionKey,
                Name = $"Выживший #{newId + 1}",
                IsConnected = true,
                LastSeenUtc = DateTime.UtcNow
            };

            ArrayPlayers.Add(newPlayer);
            UpdateAll();

            Console.WriteLine($"[LOG] Новый игрок {newId} добавлен (SessionKey: {sessionKey})");
            return newId;
        }
        public void OpenTrait(int playerId, PlayerFieldType traitName)
        {
            var player = ArrayPlayers.FirstOrDefault(p => p.Id == playerId);
            if (player == null) return;

            if (player.IsSelectionConfirmed) return;

            // Работаем с временным списком
            bool isOpen = player.ListOpenedTypes.Contains(traitName);
            bool isInPending = player.PendingOpenedTypes.Contains(traitName);
            if (!isOpen)
            {
                if (isInPending)
                {
                    // Убираем из временного списка
                    player.PendingOpenedTypes.Remove(traitName);
                    player.CurrentOpenedCard--;
                }
                else
                {
                    // Добавляем во временный список
                    if (player.CurrentOpenedCard < player.CountNeedOpen)
                    {
                        player.PendingOpenedTypes.Add(traitName);
                        player.CurrentOpenedCard++;
                    }
                }
            }

            // Обновляем ТОЛЬКО для текущего игрока
            UpdateAll();
        }



        public void CloseTrait(int playerId, PlayerFieldType traitName)
        {
            var player = ArrayPlayers.FirstOrDefault(p => p.Id == playerId);
            if (player == null) return;

            PlayerTraitHelper.SetOpened(player, traitName, false);
            UpdateAll();
        }

        public void ToggleReady(int playerId)
        {
            if (_isStarting || IsGameStarted) return;

            var player = ArrayPlayers.FirstOrDefault(p => p.Id == playerId);
            if (player != null)
            {
                player.IsReady = !player.IsReady;
                UpdateAll(); // Оповещаем всех, чтобы галочки обновились
            }
        }
        // Проверка: все ли игроки нажали "Готов"
        public bool AreAllReady => ArrayPlayers.Count > 0 && ArrayPlayers.All(p => p.IsReady);

        private void ReloadTheme()
        {
            var theme = ThemeManager.RegisterAndGetTheme(CurrentTheme);

            ConfigCharacterName.Text = theme.Names;
            ConfigCharacterHealth.Text = theme.Health;
            ConfigCharacterProfession.Text = theme.Professions;
            ConfigCharacterBodyBuild.Text = theme.BodyBuilds;
            ConfigCharacterHobby.Text = theme.Hobbies;
            ConfigCharacterPhobia.Text = theme.Phobias;
            ConfigCharacterInventory.Text = theme.Inventories;
            ConfigCharacterTrait.Text = theme.Traits;
            ConfigCharacterAdditionalInformation.Text = theme.AdditionalInformations;
            ConfigCharacterBaggage.Text = theme.Baggages;
            ConfigCharacterKnowledge.Text = theme.Knowledges;
            ConfigCharacterSecret.Text = theme.Secrets;
            ConfigCharacterReproduction.Text = theme.Reproductions;
            ConfigCharacterVision.Text = theme.Visions;
            ConfigCharacterEquipment.Text = theme.Equipments;
            ConfigCharacterRelation.Text = theme.Relations;

            CatastropheName.CatastropheText = theme.CatastropheText;
            CatastropheName.itemNames = theme.CatastropheText;
            CatastropheName.itemDescription = theme.CatastrophItemDescription;
        }
        public void StartGameAsync()
        {
            if (_isStarting || IsGameStarted) return;
            
            _isStarting = true;

            if (AreAllReady && ArrayPlayers.Count >= 2)
            {
                GenerateAllCharactersWithoutAllocation();

                ShufflePlayers();

                BunkerStats = GetTextForBunker();
                IsGameStarted = true;
                GameRounds = 1;
                CurrentPlayerIndex = 0;
                PlayersWhoMovedThisRound.Clear(); // Очищаем список ходивших

                // Подготовка первого игрока
                if (ArrayPlayers.Count > 0)
                {
                    ArrayPlayers[0].CountNeedOpen = 2; // В первом раунде 2 карты
                    ArrayPlayers[0].CurrentOpenedCard = 0;
                }
                
                UpdateAll();
            }
        }
        

        // Метод для завершения хода
        public void NextTurn()
        {
            if (!IsGameStarted) return;

            if (ArrayPlayers == null || ArrayPlayers.Count == 0)
            {
                CurrentPlayerIndex = 0;
                return;
            }

            // Применяем выбор текущего игрока
            if (CurrentPlayerIndex >= 0 && CurrentPlayerIndex < ArrayPlayers.Count)
            {
                var currentPlayer = ArrayPlayers[CurrentPlayerIndex];
                currentPlayer.ApplyPendingSelections();
                currentPlayer.IsSelectionConfirmed = false;
                currentPlayer.CurrentOpenedCard = 0;

                // Отмечаем что игрок походил
                PlayersWhoMovedThisRound.Add(currentPlayer.Id);
                Console.WriteLine($"[NEXT] Игрок {currentPlayer.Name} завершил ход. Походило: {PlayersWhoMovedThisRound.Count}");
            }

            // Считаем сколько живых игроков
            int aliveCount = ArrayPlayers.Count(p => !p.IsEliminated);

            // Если все живые походили - конец раунда
            if (PlayersWhoMovedThisRound.Count >= aliveCount)
            {
                GameRounds++;
                PlayersWhoMovedThisRound.Clear();
                Console.WriteLine($"[NEXT] Раунд {GameRounds} начат!");

                // Ищем ПЕРВОГО ЖИВОГО игрока, а не просто индекс 0
                CurrentPlayerIndex = ArrayPlayers.FindIndex(p => !p.IsEliminated);
                if (CurrentPlayerIndex == -1) CurrentPlayerIndex = 0;

                // Проверка на голосование
                if (IsVotingRound())
                {
                    StartVoting();
                    return;
                }
            }
            else
            {
                // Ищем следующего живого
                int nextIndex = (CurrentPlayerIndex + 1) % ArrayPlayers.Count;
                int checkedCount = 0;

                while (checkedCount < ArrayPlayers.Count)
                {
                    if (!ArrayPlayers[nextIndex].IsEliminated && !PlayersWhoMovedThisRound.Contains(ArrayPlayers[nextIndex].Id))
                    {
                        CurrentPlayerIndex = nextIndex;
                        break;
                    }
                    nextIndex = (nextIndex + 1) % ArrayPlayers.Count;
                    checkedCount++;
                }
            }

            // Подготовка следующего игрока
            if (CurrentPlayerIndex >= 0 && CurrentPlayerIndex < ArrayPlayers.Count)
            {
                PreparePlayerForTurn(ArrayPlayers[CurrentPlayerIndex]);
                Console.WriteLine($"[NEXT] Ход -> {ArrayPlayers[CurrentPlayerIndex].Name} (Раунд {GameRounds})");
            }

            UpdateAll();
        }
        public bool IsVotingRound()
        {
            // 1. Первое голосование на 3 раунде
            return GameRounds switch
            {
                4 => true,
                > 4 when (GameRounds - 4) % 3 == 0 => true,
                _ => false
            };
        }

        public void StartVoting()
        {
            Votes.Clear();        
            IsVotingActive = true;
            UpdateAll();
        }

        public void SubmitVote(int voterId, int candidateId)
        {
            if (!IsVotingActive) return;

            // 1. Записываем голос (используем ID голосующего как ключ)
            Votes[voterId] = candidateId;

            // 2. Считаем, сколько живых игроков должны проголосовать
            int totalAlive = ArrayPlayers.Count(p => !p.IsEliminated);

            // 3. ЛОГ ДЛЯ ПРОВЕРКИ (посмотри его в консоли Visual Studio)
            Console.WriteLine($"Голос принят от ID:{voterId}. Всего голосов: {Votes.Count}/{totalAlive}");

            // 4. Автоматический запуск анализа
            if (Votes.Count >= totalAlive)
            {
                Console.WriteLine("Все проголосовали! Запускаю анализ...");
                AnalyzeVotesAndEliminate();
            }
            else
            {
                UpdateAll(); // Просто обновляем экран, чтобы другие увидели счетчик
            }
        }

        // Вызывайте проверку после каждого исключения игрока
        public void AnalyzeVotesAndEliminate()
        {
            try
            {
                if (ArrayPlayers == null || ArrayPlayers.Count == 0)
                {
                    IsVotingActive = false;
                    Votes.Clear();
                    UpdateAll();
                    return;
                }

                var votingResults = Votes.Values
                    .GroupBy(id => id)
                    .Select(g => new { PlayerId = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .ToList();

                if (votingResults.Count > 0)
                {
                    bool isDraw = votingResults.Count > 1 && votingResults[0].Count == votingResults[1].Count;

                    if (!isDraw)
                    {
                        var loserId = votingResults[0].PlayerId;
                        var loser = ArrayPlayers.FirstOrDefault(p => p.Id == loserId);
                        if (loser != null)
                        {
                            loser.IsEliminated = true;
                            Console.WriteLine($"Игрок {loser.Name} изгнан.");

                            // Проверяем условие победы после исключения
                            CheckWinCondition();
                        }
                    }
                    else
                    {
                        Console.WriteLine("Ничья! Никто не покинул игру.");
                    }
                }

                if (!IsGameStarted) return;

                IsVotingActive = false;
                Votes.Clear();

                // Находим первого живого игрока
                CurrentPlayerIndex = ArrayPlayers.FindIndex(p => !p.IsEliminated);
                if (CurrentPlayerIndex == -1) CurrentPlayerIndex = 0;

                // КРИТИЧЕСКИ ВАЖНО: Сбрасываем состояние ходов для НОВОГО раунда
                PlayersWhoMovedThisRound.Clear();

                // Сбрасываем состояние выбора карт для ВСЕХ живых игроков
                foreach (var player in ArrayPlayers.Where(p => !p.IsEliminated))
                {
                    player.CurrentOpenedCard = 0;
                    player.PendingOpenedTypes.Clear();
                    player.IsSelectionConfirmed = false;
                    player.CountNeedOpen = GameRounds == 1 ? 2 : 1;
                }

                // Подготавливаем текущего игрока
                PreparePlayerForTurn(ArrayPlayers[CurrentPlayerIndex]);
                IsVotingActive = false;
                UpdateAll();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ОШИБКА В ГОЛОСОВАНИИ: {ex.Message}");
                IsVotingActive = false;
                Votes.Clear();
                UpdateAll();
            }
        }

        public void ShufflePlayers()
        {
            Random rng = Random.Shared;
            int n = ArrayPlayers.Count;

            // Алгоритм Фишера-Йетса (Fisher-Yates shuffle)
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                (ArrayPlayers[k], ArrayPlayers[n]) = (ArrayPlayers[n], ArrayPlayers[k]);
            }

            // После перемешивания нужно сбросить индексы
            CurrentPlayerIndex = 0;
            PlayersWhoMovedThisRound.Clear();

#if DEBUG
            Console.WriteLine("[SHUFFLE] Порядок ходов:");
            for (int i = 0; i < ArrayPlayers.Count; i++)
            {
                Console.WriteLine($"  {i + 1}. {ArrayPlayers[i].Name} (ID: {ArrayPlayers[i].Id})");
            }
#endif
        }


        private void PreparePlayerForTurn(Player player)
        {
            player.CurrentOpenedCard = 0;
            player.CountNeedOpen = GameRounds == 1 ? 2 : 1;
            player.PendingOpenedTypes.Clear();
            player.IsSelectionConfirmed = false;
#if DEBUG
            Console.WriteLine($"[PREPARE] Игрок {player.Name} готов к ходу (Раунд {GameRounds}, нужно открыть {player.CountNeedOpen} карт)");
#endif
        }


        public void FullRestart(bool againPlay = false)
        {

            _cleanupTimer?.Dispose();
            _cleanupTimer = new Timer(_ => RemoveDisconnectedPlayers(), null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
            // 1. Очищаем списки
            if (!againPlay) 
            { 
                ArrayPlayers.Clear();
                Votes.Clear(); 
            }
            else
            {
                GenerateAllCharactersWithoutAllocation();
            }

            // 2. Сбрасываем системные переменные
            IsGameStarted = false;
            IsVotingActive = false;
            GameRounds = 1;
            CurrentPlayerIndex = 0;
            _isStarting = false;
            _idCounter = -1;

            // 3. Сбрасываем состояние бункера
            BunkerStats = default;

            // 4. Уведомляем всех клиентов
            UpdateAll();
        }

        // Использование специального умения
        // Новый метод - принимает ID цели, а характеристика берётся из умения
        public bool UseSpecialAbility(int playerId, int targetPlayerId)
        {
            var player = ArrayPlayers.FirstOrDefault(p => p.Id == playerId);
            if (player == null || player.SpecialCondition.IsUsed) return false;

            var ability = player.SpecialCondition;
            var targetPlayer = ArrayPlayers.FirstOrDefault(p => p.Id == targetPlayerId);
            if (targetPlayer == null) return false;

            bool success = false;

            switch (ability.Type)
            {
                case CharacterSpecialConditionType.Swap:
                    // Обмен характеристикой (которая задана в ability.PlayerFieldType)
                    success = SwapTrait(playerId, targetPlayerId, ability.PlayerFieldType);
                    break;

                case CharacterSpecialConditionType.Rerole:
                    // Переброс своей характеристики
                    success = RerollTrait(playerId, ability.PlayerFieldType);
                    break;

                case CharacterSpecialConditionType.Upgrade:
                    // Улучшение своей характеристики
                    success = UpgradeTrait(playerId, ability.PlayerFieldType);
                    break;

                case CharacterSpecialConditionType.Snow:
                    // Показать чужую характеристику (случайную, не выбираем)
                    success = ShowOtherTrait(playerId, targetPlayerId);
                    break;
            }

            if (success)
            {
                player.SpecialCondition.IsUsed = true;
                UpdateAll();
#if DEBUG
                Console.WriteLine($"[DEBUG] UseSpecialAbility: UpdateAll вызван");
#endif
            }

            return success;
        }

        // Обмен характеристикой с другим игроком
        private bool SwapTrait(int playerId, int targetPlayerId, PlayerFieldType fieldType)
        {
            var currentPlayer = ArrayPlayers.FirstOrDefault(p => p.Id == playerId);
            var targetPlayer = ArrayPlayers[targetPlayerId];

            if (currentPlayer == null || targetPlayer == null) return false;

            // Временное сохранение значений
            var tempValue1 = PlayerTraitHelper.GetValue(currentPlayer, fieldType);
            var tempValue2 = PlayerTraitHelper.GetValue(targetPlayer, fieldType);

            // Обмен значениями
            PlayerTraitHelper.SetValue(currentPlayer, fieldType, tempValue2);
            PlayerTraitHelper.SetValue(targetPlayer, fieldType, tempValue1);

            currentPlayer.SpecialCondition.IsUsed = true;
            UpdateAll();
            return true;
        }

        // Переброс характеристики
        private bool RerollTrait(int playerId, PlayerFieldType fieldType)
        {
            var player = ArrayPlayers.FirstOrDefault(p => p.Id == playerId);
            if (player == null) return false;

            Random rnd = new();

            switch (fieldType)
            {
                case PlayerFieldType.BiologicalSex:
                    int index = Random.Shared.Next(ConfigCharacterBiologicalSex.Text.Length);
                    player.BiologicalSex = ConfigCharacterBiologicalSex.GetConfig(index, rnd);
                    break;
                case PlayerFieldType.Age:
                    int ageIndex = Random.Shared.Next(100);
                    player.Age = ConfigCharacterAge.GetConfig(ageIndex);
                    break;
                case PlayerFieldType.Profession:
                    int profIndex = Random.Shared.Next(ConfigCharacterProfession.Text.Length);
                    player.Profession = ConfigCharacterProfession.GetConfig(profIndex, rnd);
                    break;
                case PlayerFieldType.Health:
                    int healthIndex = Random.Shared.Next(ConfigCharacterHealth.Text.Length);
                    player.Health = ConfigCharacterHealth.GetConfig(healthIndex, rnd);
                    break;
                case PlayerFieldType.BodyBuild:
                    int bodyIndex = Random.Shared.Next(ConfigCharacterBodyBuild.Text.Length);
                    player.BodyBuild = ConfigCharacterBodyBuild.GetConfig(bodyIndex);
                    break;
                case PlayerFieldType.Hobby:
                    int hobbyIndex = Random.Shared.Next(ConfigCharacterHobby.Text.Length);
                    player.Hobby = ConfigCharacterHobby.GetConfig(hobbyIndex, rnd);
                    break;
                case PlayerFieldType.Phobia:
                    int phobiaIndex = Random.Shared.Next(ConfigCharacterPhobia.Text.Length);
                    player.Phobia = ConfigCharacterPhobia.GetConfig(phobiaIndex);
                    break;
                case PlayerFieldType.Inventory:
                    int invIndex = Random.Shared.Next(ConfigCharacterInventory.Text.Length);
                    player.Inventory = ConfigCharacterInventory.GetConfig(invIndex);
                    break;
                case PlayerFieldType.Trait:
                    int traitIndex = Random.Shared.Next(ConfigCharacterTrait.Text.Length);
                    player.Trait = ConfigCharacterTrait.GetConfig(traitIndex);
                    break;
                case PlayerFieldType.AdditionalInformation:
                    int addIndex = Random.Shared.Next(ConfigCharacterAdditionalInformation.Text.Length);
                    player.AdditionalInformation = ConfigCharacterAdditionalInformation.GetConfig(addIndex);
                    break;
                case PlayerFieldType.SpecialCondition:
                    int specIndex = Random.Shared.Next(ConfigCharacterSpecialCondition.Text.Length);
                    player.SpecialCondition = ConfigCharacterSpecialCondition.GetConfig(specIndex, rnd);
                    break;
                case PlayerFieldType.Baggage:
                    int bagIndex = Random.Shared.Next(ConfigCharacterBaggage.Text.Length);
                    player.Baggage = ConfigCharacterBaggage.GetConfig(bagIndex, rnd);
                    break;
                case PlayerFieldType.Knowledge:
                    int knowIndex = Random.Shared.Next(ConfigCharacterKnowledge.Text.Length);
                    player.Knowledge = ConfigCharacterKnowledge.GetConfig(knowIndex, rnd);
                    break;
                case PlayerFieldType.Secret:
                    int secretIndex = Random.Shared.Next(ConfigCharacterSecret.Text.Length);
                    player.Secret = ConfigCharacterSecret.GetConfig(secretIndex);
                    break;
                case PlayerFieldType.Reproduction:
                    int reproIndex = Random.Shared.Next(ConfigCharacterReproduction.Text.Length);
                    player.Reproduction = ConfigCharacterReproduction.GetConfig(reproIndex, rnd);
                    break;
                case PlayerFieldType.Vision:
                    int visionIndex = Random.Shared.Next(ConfigCharacterVision.Text.Length);
                    player.Vision = ConfigCharacterVision.GetConfig(visionIndex);
                    break;
                case PlayerFieldType.Equipment:
                    int equipIndex = Random.Shared.Next(ConfigCharacterEquipment.Text.Length);
                    player.Equipment = ConfigCharacterEquipment.GetConfig(equipIndex);
                    break;
                case PlayerFieldType.Relation:
                    int relIndex = Random.Shared.Next(ConfigCharacterRelation.Text.Length);
                    player.Relation = ConfigCharacterRelation.GetConfig(relIndex);
                    break;
                default:
                    return false;
            }

            player.SpecialCondition.IsUsed = true;
            UpdateAll();
            return true;
        }

        // Улучшение характеристики
        private bool UpgradeTrait(int playerId, PlayerFieldType fieldType)
        {
            var player = ArrayPlayers.FirstOrDefault(p => p.Id == playerId);
            if (player == null) return false;

            float currentValue = PlayerTraitHelper.GetFinalAmount(player, fieldType);
            float maxValue = PlayerTraitHelper.GetMaxAmount(player, fieldType);

            // Увеличиваем на 20%, но не больше максимума
            float newValue = Math.Min(maxValue, currentValue * 1.2f);
            PlayerTraitHelper.SetFinalAmount(player, fieldType, newValue);

            player.SpecialCondition.IsUsed = true;
            UpdateAll();
            return true;
        }

        // Показать чужую характеристику
        private bool ShowOtherTrait(int playerId, int targetPlayerId)
        {
            var player = ArrayPlayers.FirstOrDefault(p => p.Id == playerId);
            var targetPlayer = ArrayPlayers.FirstOrDefault(p => p.Id == targetPlayerId);

            if (targetPlayer == null)
            {
                Console.WriteLine($"[ERROR] ShowOtherTrait: targetPlayer не найден (ID={targetPlayerId})");
                return false;
            }

            var fieldType = player.SpecialCondition.PlayerFieldType;
            Console.WriteLine($"[DEBUG] ShowOtherTrait: {player.Name} показывает {fieldType} у {targetPlayer.Name}");

            // Открываем характеристику у цели
            PlayerTraitHelper.SetOpened(targetPlayer, fieldType, true);

            // Проверяем, открылась ли
            bool isOpened = PlayerTraitHelper.IsOpened(targetPlayer, fieldType);
            Console.WriteLine($"[DEBUG] Характеристика {fieldType} открыта: {isOpened}");
            UpdateAll();
            return true;
        }

        // Добавьте метод для подтверждения выбранных карт
        public bool ConfirmSelection(int playerId)
        {
            var player = ArrayPlayers.FirstOrDefault(p => p.Id == playerId);
            if (player == null) return false;

            if (player.CurrentOpenedCard < player.CountNeedOpen)
                return false;

            // Применяем временный выбор - открываем характеристики
            foreach (var type in player.PendingOpenedTypes)
            {
                PlayerTraitHelper.SetOpened(player, type, true);
            }

            player.IsSelectionConfirmed = true;
            UpdateAll();
            return true;
        }

        // Отмена выбора (например, для умения)
        public void CancelSelection(int playerId)
        {
            var player = ArrayPlayers.FirstOrDefault(p => p.Id == playerId);
            if (player == null) return;

            player.PendingOpenedTypes.Clear();
            player.CurrentOpenedCard = 0;
            player.IsSelectionConfirmed = false;
            UpdateAll();
        }

        public BunkerStats GetTextForBunker()
        {
            int idCatastropheText = Random.Shared.Next(0, CatastropheName.CatastropheText.Length);

            int min = (ArrayPlayers.Count + 3) / 4;
            int max = ArrayPlayers.Count * 2 / 3;

            if (min > max) min = max;
            int MaxPlayerCount = Random.Shared.Next(min, max + 1);

            int TimeToNeedInBunker = Random.Shared.Next(0, 72);
            int Count = Random.Shared.Next(0, 20);

            // Массив структур - одна аллокация в куче
            BunkerItem[] items = new BunkerItem[Count];

            for (int i = 0; i < Count; i++)
            {
                // Создаем структуру на стеке и копируем в массив
                int idItemName = Random.Shared.Next(0, CatastropheName.itemNames.Length);
                int itemCount = Random.Shared.Next(0, 40);
                int idItemDescription = Random.Shared.Next(0, CatastropheName.itemDescription.Length);

                items[i] = new BunkerItem(idItemName, idItemDescription, itemCount);
            }

            // Структура BunkerStats создается и копируется при возврате
            return new BunkerStats(items, TimeToNeedInBunker, MaxPlayerCount, idCatastropheText)
            {
                IsCreate = true
            };
        }       
        // Метод проверки условия победы
        private void CheckWinCondition()
        {
            if (!IsGameStarted) return;
            if (BunkerStats.MaxPlayerCount <= 0) return;

            int alivePlayers = ArrayPlayers.Count(p => !p.IsEliminated);

            if (alivePlayers <= BunkerStats.MaxPlayerCount)
            {
                // Условие победы выполнено!
                IsGameStarted = false;
                IsVotingActive = false;

                var survivors = ArrayPlayers.Where(p => !p.IsEliminated).ToList();
                string message = $"ИГРА ОКОНЧЕНА! Выжившие ({alivePlayers}/{BunkerStats.MaxPlayerCount}):\n";

                Console.WriteLine(message);
                OnGameEnded?.Invoke(message);
                UpdateAll();
            }
        }

        public Dictionary<int, DateTime> LastSeen { get; set; } = [];

        public void UpdatePlayerActivity(int playerId)
        {
            LastSeen[playerId] = DateTime.UtcNow;
        }

        public void RemoveInactivePlayers()
        {
            var timeout = TimeSpan.FromSeconds(30); // 30 секунд на переподключение
            var now = DateTime.UtcNow;
            var toRemove = LastSeen.Where(kv => now - kv.Value > timeout).Select(kv => kv.Key).ToList();

            foreach (var playerId in toRemove)
            {
                var player = ArrayPlayers.FirstOrDefault(p => p.Id == playerId);
                if (player != null && !player.IsEliminated)
                {
                    // Игрок не вернулся – исключаем из игры
                    player.IsEliminated = true;
                    Console.WriteLine($"[TIMEOUT] Игрок {player.Name} удалён за неактивность");
                }
                LastSeen.Remove(playerId);
            }
        }


    }

}
