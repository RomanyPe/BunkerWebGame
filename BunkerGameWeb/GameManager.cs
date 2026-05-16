using BunkerGameWeb.Components.Pages;
using BunkerGameWeb.Helpers;
using BunkerGameWeb.Models;

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

        public ConfigCharacterEquipment ConfigCharacterEquipment = new(); // Если создал класс для одежды
        public ConfigCharacterRelation ConfigCharacterRelation = new(); // Есл

        public CatastropheName CatastropheName = new();

        public event Action? OnNotify; // Событие для SignalR

        public void UpdateAll() => OnNotify?.Invoke();
        // GameManager.cs
        private Timer? _cleanupTimer;

        public GameManager()
        {
            // Запускаем таймер каждые 30 секунд
            _cleanupTimer = new Timer(_ => RemoveDisconnectedPlayers(), null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        }

        private void RemoveDisconnectedPlayers()
        {
            var timeout = TimeSpan.FromSeconds(45); // даём 45 секунд на переподключение
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
        // Теперь метод принимает объект Player напрямую
        public void AssignRandomStats(Player player)
        {
            Random rnd = new();

            int index = Random.Shared.Next(ConfigCharacterName.Text.Length);
            player.Name = ConfigCharacterName.Text[index];

            index = Random.Shared.Next(ConfigCharacterBiologicalSex.Text.Length);
            player.BiologicalSex = ConfigCharacterBiologicalSex.GetConfig(index, rnd);

            index = Random.Shared.Next(100);
            player.Age = ConfigCharacterAge.GetConfig(index);

            index = Random.Shared.Next(ConfigCharacterProfession.Text.Length);
            player.Profession = ConfigCharacterProfession.GetConfig(index, rnd);

            index = Random.Shared.Next(ConfigCharacterHealth.Text.Length);
            player.Health = ConfigCharacterHealth.GetConfig(index, rnd);

            index = Random.Shared.Next(ConfigCharacterBodyBuild.Text.Length);
            player.BodyBuild = ConfigCharacterBodyBuild.GetConfig(index, rnd);

            index = Random.Shared.Next(ConfigCharacterHobby.Text.Length);
            player.Hobby = ConfigCharacterHobby.GetConfig(index, rnd);

            index = Random.Shared.Next(ConfigCharacterPhobia.Text.Length);
            player.Phobia = ConfigCharacterPhobia.GetConfig(index, rnd);

            index = Random.Shared.Next(ConfigCharacterInventory.Text.Length);
            player.Inventory = ConfigCharacterInventory.GetConfig(index, rnd);

            index = Random.Shared.Next(ConfigCharacterTrait.Text.Length);
            player.Trait = ConfigCharacterTrait.GetConfig(index, rnd);

            index = Random.Shared.Next(ConfigCharacterAdditionalInformation.Text.Length);
            player.AdditionalInformation = ConfigCharacterAdditionalInformation.GetConfig(index, rnd);

            index = Random.Shared.Next(ConfigCharacterSpecialCondition.Text.Length);
            player.SpecialCondition = ConfigCharacterSpecialCondition.GetConfig(index, rnd);

            index = Random.Shared.Next(ConfigCharacterBaggage.Text.Length);
            player.Baggage = ConfigCharacterBaggage.GetConfig(index, rnd);

            index = Random.Shared.Next(ConfigCharacterKnowledge.Text.Length);
            player.Knowledge = ConfigCharacterKnowledge.GetConfig(index, rnd);

            index = Random.Shared.Next(ConfigCharacterSecret.Text.Length);
            player.Secret = ConfigCharacterSecret.GetConfig(index, rnd);

            index = Random.Shared.Next(ConfigCharacterReproduction.Text.Length);
            player.Reproduction = ConfigCharacterReproduction.GetConfig(index, rnd);

            index = Random.Shared.Next(ConfigCharacterVision.Text.Length);
            player.Vision = ConfigCharacterVision.GetConfig(index, rnd);

            index = Random.Shared.Next(ConfigCharacterEquipment.Text.Length);
            player.Equipment = ConfigCharacterEquipment.GetConfig(index, rnd);

            index = Random.Shared.Next(ConfigCharacterRelation.Text.Length);
            player.Relation = ConfigCharacterRelation.GetConfig(index, rnd);
        }

        public int AddAndInitializePlayer(string sessionKey = "")
        {
            if (IsGameStarted) return -1;

            // ✅ ПРОВЕРКА: Есть ли уже игрок с таким SessionKey?
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
                Name = $"Выживший #{newId}",
                IsConnected = true,
                LastSeenUtc = DateTime.UtcNow
            };

            AssignRandomStats(newPlayer);
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


        public async Task StartGameAsync()
        {
            if (_isStarting || IsGameStarted) return;

            _isStarting = true;
            await WaiterToStart();

            if (AreAllReady && ArrayPlayers.Count >= 2)
            {
                BunkerStats = GetTextForBunker();
                IsGameStarted = true;
                GameRounds = 1;
                CurrentPlayerIndex = 0;
                PlayersWhoMovedThisRound.Clear(); // ✅ Очищаем список ходивших

                CheckWinCondition();

                // Подготовка первого игрока
                if (ArrayPlayers.Count > 0)
                {
                    ArrayPlayers[0].CountNeedOpen = 2; // В первом раунде 2 карты
                    ArrayPlayers[0].CurrentOpenedCard = 0;
                }

                UpdateAll();
            }
        }
        private async Task WaiterToStart()
        {
            for (int i = 0; i < 10 && AreAllReady; i++)
            {
                await Task.Delay(100);
            }
        }

        // Метод для завершения хода
        // Метод NextTurn остается, но вызывается ПОСЛЕ подтверждения

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

                // Сбрасываем на первого живого
                CurrentPlayerIndex = 0;

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
                var nextPlayer = ArrayPlayers[CurrentPlayerIndex];
                nextPlayer.CurrentOpenedCard = 0;
                nextPlayer.CountNeedOpen = GameRounds == 1 ? 2 : 1;
                nextPlayer.PendingOpenedTypes.Clear();
                nextPlayer.IsSelectionConfirmed = false;
                Console.WriteLine($"[NEXT] Ход -> {nextPlayer.Name} (Раунд {GameRounds})");
            }

            UpdateAll();
        }
        public bool IsVotingRound()
        {
            // 1. Первое голосование на 3 раунде
            if (GameRounds == 3) return true;

            // 2. После 3 раунда голосование каждые 2 раунда (5, 7, 9...)
            if (GameRounds > 3 && (GameRounds - 3) % 2 == 0) return true;

            return false;
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

                            // ✅ Проверяем условие победы после исключения
                            CheckWinCondition();
                        }
                    }
                    else
                    {
                        Console.WriteLine("Ничья! Никто не покинул игру.");
                    }
                }

                // Если игра закончена - не продолжаем
                if (!IsGameStarted) return;

                IsVotingActive = false;
                Votes.Clear();
                GameRounds++;
                MoveToNextAlivePlayerSafe();
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
        private void MoveToNextAlivePlayerSafe()
        {
            if (ArrayPlayers == null || ArrayPlayers.Count == 0)
            {
                CurrentPlayerIndex = 0;
                return;
            }

            int nextIndex = (CurrentPlayerIndex + 1) % ArrayPlayers.Count;
            int checkedCount = 0;

            // Ищем следующего живого игрока
            while (checkedCount < ArrayPlayers.Count)
            {
                if (nextIndex >= 0 && nextIndex < ArrayPlayers.Count && !ArrayPlayers[nextIndex].IsEliminated)
                {
                    CurrentPlayerIndex = nextIndex;
                    var nextPlayer = ArrayPlayers[CurrentPlayerIndex];
                    nextPlayer.CurrentOpenedCard = 0;
                    nextPlayer.CountNeedOpen = GameRounds == 1 ? 2 : 1;
                    nextPlayer.PendingOpenedTypes.Clear();
                    nextPlayer.IsSelectionConfirmed = false;
                    Console.WriteLine($"[NEXT] Ход перешел к {nextPlayer.Name} (индекс {CurrentPlayerIndex})");
                    return;
                }

                nextIndex = (nextIndex + 1) % ArrayPlayers.Count;
                checkedCount++;
            }

            // Если не нашли живого - сбрасываем на 0
            CurrentPlayerIndex = 0;
            Console.WriteLine("[NEXT] Не найдено живых игроков, сброс на 0");
        }
        public void FullRestart()
        {

            _cleanupTimer?.Dispose();
            _cleanupTimer = new Timer(_ => RemoveDisconnectedPlayers(), null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
            // 1. Очищаем списки
            ArrayPlayers.Clear();
            Votes.Clear();

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
        // Использование специального умения
        public bool UseSpecialAbility(int playerId, PlayerFieldType? targetField = null)
        {
            var player = ArrayPlayers.FirstOrDefault(p => p.Id == playerId);
            if (player == null || player.SpecialCondition.IsUsed) return false;

            var ability = player.SpecialCondition;
            PlayerFieldType fieldToUse = targetField ?? ability.PlayerFieldType;

            bool success = false;

            switch (ability.Type)
            {
                case CharacterSpecialConditionType.Swap:
                    success = SwapTrait(playerId, fieldToUse);
                    break;

                case CharacterSpecialConditionType.Rerole:
                    success = RerollTrait(playerId, fieldToUse);
                    break;

                case CharacterSpecialConditionType.Upgrade:
                    success = UpgradeTrait(playerId, fieldToUse);
                    break;

                case CharacterSpecialConditionType.Snow:
                    success = ShowOtherTrait(playerId);
                    break;

                case CharacterSpecialConditionType.SnowYourself:
                    success = ShowSelfTrait(playerId);
                    break;
            }

            if (success)
            {
                player.SpecialCondition.IsUsed = true;

                // Если это умение показа чужой карты - открываем случайную характеристику
                if (ability.Type == CharacterSpecialConditionType.Snow)
                {
                    RevealRandomTraitOfOther(playerId);
                }

                UpdateAll();
            }

            return success;
        }

        private void RevealRandomTraitOfOther(int playerId)
        {
            var currentPlayer = ArrayPlayers.FirstOrDefault(p => p.Id == playerId);
            if (currentPlayer == null) return;

            // Получаем список других живых игроков
            var otherPlayers = ArrayPlayers.Where(p => p.Id != playerId && !p.IsEliminated).ToList();
            if (otherPlayers.Count == 0) return;

            // Выбираем случайного игрока
            var randomPlayer = otherPlayers[Random.Shared.Next(otherPlayers.Count)];

            // Получаем список неоткрытых характеристик
            var unopenedFields = Enum.GetValues<PlayerFieldType>()
                .Where(f => f != PlayerFieldType.SpecialCondition && !PlayerTraitHelper.IsOpened(randomPlayer, f))
                .ToList();

            if (unopenedFields.Count > 0)
            {
                // Открываем случайную характеристику
                var randomField = unopenedFields[Random.Shared.Next(unopenedFields.Count)];
                PlayerTraitHelper.SetOpened(randomPlayer, randomField, true);

                Console.WriteLine($"[УМЕНИЕ] Открыта характеристика {randomField} игрока {randomPlayer.Name}");
            }
        }

        // Обмен характеристикой с другим игроком
        private bool SwapTrait(int playerId, PlayerFieldType fieldType)
        {
            var currentPlayer = ArrayPlayers.FirstOrDefault(p => p.Id == playerId);
            var targetPlayer = ArrayPlayers.FirstOrDefault(p => p.Id == CurrentTurnPlayerId && p.Id != playerId);

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

        // Добавьте метод для получения имени характеристики
        public string GetFieldDisplayName(PlayerFieldType fieldType)
        {
            return fieldType switch
            {
                PlayerFieldType.BiologicalSex => "Пол",
                PlayerFieldType.Age => "Возраст",
                PlayerFieldType.Profession => "Профессия",
                PlayerFieldType.Health => "Здоровье",
                PlayerFieldType.BodyBuild => "Телосложение",
                PlayerFieldType.Hobby => "Хобби",
                PlayerFieldType.Phobia => "Фобия",
                PlayerFieldType.Inventory => "Инвентарь",
                PlayerFieldType.Trait => "Черта характера",
                PlayerFieldType.AdditionalInformation => "Доп. информация",
                PlayerFieldType.SpecialCondition => "Спец. условие",
                PlayerFieldType.Baggage => "Багаж",
                PlayerFieldType.Knowledge => "Знания",
                PlayerFieldType.Secret => "Секрет",
                PlayerFieldType.Reproduction => "Репродукция",
                PlayerFieldType.Vision => "Видение",
                PlayerFieldType.Equipment => "Снаряжение",
                PlayerFieldType.Relation => "Отношение",
                _ => "Неизвестно"
            };
        }
        // Получить список всех полей (кроме специального условия)
        public List<PlayerFieldType> GetAllPlayerFields()
        {
            return [.. Enum.GetValues<PlayerFieldType>().Where(f => f != PlayerFieldType.SpecialCondition)];
        }

        // Обмен характеристиками между игроками
        private bool SwapTrait(int playerId, PlayerFieldType fieldType1, PlayerFieldType fieldType2)
        {
            var currentPlayer = ArrayPlayers.FirstOrDefault(p => p.Id == playerId);
            var targetPlayer = ArrayPlayers.FirstOrDefault(p => p.Id == CurrentTurnPlayerId && p.Id != playerId);

            if (currentPlayer == null || targetPlayer == null) return false;

            // Временное сохранение значений
            var tempValue1 = PlayerTraitHelper.GetValue(currentPlayer, fieldType1);
            var tempValue2 = PlayerTraitHelper.GetValue(targetPlayer, fieldType2);

            // Обмен значениями
            PlayerTraitHelper.SetValue(currentPlayer, fieldType1, tempValue2);
            PlayerTraitHelper.SetValue(targetPlayer, fieldType2, tempValue1);

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
                    player.BodyBuild = ConfigCharacterBodyBuild.GetConfig(bodyIndex, rnd);
                    break;
                case PlayerFieldType.Hobby:
                    int hobbyIndex = Random.Shared.Next(ConfigCharacterHobby.Text.Length);
                    player.Hobby = ConfigCharacterHobby.GetConfig(hobbyIndex, rnd);
                    break;
                case PlayerFieldType.Phobia:
                    int phobiaIndex = Random.Shared.Next(ConfigCharacterPhobia.Text.Length);
                    player.Phobia = ConfigCharacterPhobia.GetConfig(phobiaIndex, rnd);
                    break;
                case PlayerFieldType.Inventory:
                    int invIndex = Random.Shared.Next(ConfigCharacterInventory.Text.Length);
                    player.Inventory = ConfigCharacterInventory.GetConfig(invIndex, rnd);
                    break;
                case PlayerFieldType.Trait:
                    int traitIndex = Random.Shared.Next(ConfigCharacterTrait.Text.Length);
                    player.Trait = ConfigCharacterTrait.GetConfig(traitIndex, rnd);
                    break;
                case PlayerFieldType.AdditionalInformation:
                    int addIndex = Random.Shared.Next(ConfigCharacterAdditionalInformation.Text.Length);
                    player.AdditionalInformation = ConfigCharacterAdditionalInformation.GetConfig(addIndex, rnd);
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
                    player.Secret = ConfigCharacterSecret.GetConfig(secretIndex, rnd);
                    break;
                case PlayerFieldType.Reproduction:
                    int reproIndex = Random.Shared.Next(ConfigCharacterReproduction.Text.Length);
                    player.Reproduction = ConfigCharacterReproduction.GetConfig(reproIndex, rnd);
                    break;
                case PlayerFieldType.Vision:
                    int visionIndex = Random.Shared.Next(ConfigCharacterVision.Text.Length);
                    player.Vision = ConfigCharacterVision.GetConfig(visionIndex, rnd);
                    break;
                case PlayerFieldType.Equipment:
                    int equipIndex = Random.Shared.Next(ConfigCharacterEquipment.Text.Length);
                    player.Equipment = ConfigCharacterEquipment.GetConfig(equipIndex, rnd);
                    break;
                case PlayerFieldType.Relation:
                    int relIndex = Random.Shared.Next(ConfigCharacterRelation.Text.Length);
                    player.Relation = ConfigCharacterRelation.GetConfig(relIndex, rnd);
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
        private bool ShowOtherTrait(int playerId)
        {
            var player = ArrayPlayers.FirstOrDefault(p => p.Id == playerId);
            if (player == null) return false;

            // Умение используется, но характеристика откроется в RevealRandomTraitOfOther
            // Здесь просто подтверждаем использование
            return true;
        }

        // Показать еще одну свою характеристику
        private bool ShowSelfTrait(int playerId)
        {
            var player = ArrayPlayers.FirstOrDefault(p => p.Id == playerId);
            if (player == null) return false;

            // Увеличиваем лимит открываемых карт для себя
            player.CountNeedOpen += 1;

            // Открываем случайную неоткрытую характеристику
            var unopenedFields = Enum.GetValues<PlayerFieldType>()
                .Where(f => f != PlayerFieldType.SpecialCondition && !PlayerTraitHelper.IsOpened(player, f))
                .ToList();

            if (unopenedFields.Count > 0)
            {
                var randomField = unopenedFields[Random.Shared.Next(unopenedFields.Count)];
                PlayerTraitHelper.SetOpened(player, randomField, true);
                Console.WriteLine($"[УМЕНИЕ] Открыта своя характеристика: {randomField}");
            }

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
        }        // Метод проверки условия победы
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
