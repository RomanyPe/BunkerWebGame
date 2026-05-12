using BunkerGameWeb.Helpers;
using BunkerGameWeb.Models;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace BunkerGameWeb
{
    public class GameManager
    {

        public List<Player> ArrayPlayers { get; set; } = [];

        private int _idCounter = -1;
        public bool IsGameStarted { get; private set; } = false;
        public bool _isStarting = false;

        // Индекс игрока, который ходит сейчас (в списке PlayerIds)
        public int CurrentPlayerIndex { get; private set; } = 0;

        // Свойство, возвращающее ID текущего ходящего
        public int CurrentTurnPlayerId 
        {
            get
            {
                if (ArrayPlayers == null || ArrayPlayers.Count == 0)
                    return -1; // Возвращаем спец. значение, если игроков нет

                return ArrayPlayers[CurrentPlayerIndex].Id;
            }
        }

        public int GameRounds = 0;

        public Dictionary<int, int> Votes = [];
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

        public event Action? OnNotify; // Событие для SignalR

        public void UpdateAll() => OnNotify?.Invoke();

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

        public int AddAndInitializePlayer()
        {

            if (IsGameStarted) return -1;
            // Атомарно увеличивает счетчик и возвращает новое значение. Без блокировок!
            int newId = Interlocked.Increment(ref _idCounter);

            var newPlayer = new Player
            {
                Id = newId,
                Name = $"Выживший #{newId}"
            };

            // Автоматически выдаем стартовые характеристики
            // Используем те же методы, что мы писали раньше
            AssignRandomStats(newPlayer);

            ArrayPlayers.Add(newPlayer);

            // ВАЖНО: Рассылаем сигнал всем, что в списке новый игрок
            UpdateAll();

            Console.WriteLine($"[LOG] Игрок {newId} добавлен в спискок");
            return newId;

        }

        public void OpenTrait(int playerId, PlayerFieldType traitName)
        {
            var player = ArrayPlayers.FirstOrDefault(p => p.Id == playerId);
            if (player == null) return;

            // Проверяем, не открыта ли уже характеристика
            bool isAlreadyOpened = PlayerTraitHelper.IsOpened(player, traitName);

            if (isAlreadyOpened)
            {
                // Если уже открыта - закрываем (отмена выбора)
                PlayerTraitHelper.SetOpened(player, traitName, false);
            }
            else
            {
                // Открываем характеристику
                PlayerTraitHelper.SetOpened(player, traitName, true);
            }

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
            if (_isStarting || IsGameStarted) return; // Защита от двойного клика

            _isStarting = true;
            await WaiterToStart();

            // Повторная проверка после ожидания
            if (AreAllReady && ArrayPlayers.Count >= 2)
            {
                IsGameStarted = true;
                UpdateAll();
            }

        }
        private async Task WaiterToStart()
        {
            for (int i = 0; i < 30 && AreAllReady; i++)
            {
                await Task.Delay(100);
            }
        }

        // Метод для завершения хода
        public void NextTurn()
        {
            if (ArrayPlayers.Count == 0) return;

            // 1. Увеличиваем индекс
            CurrentPlayerIndex++;

            // 2. Если вышли за пределы — значит круг закончен
            if (CurrentPlayerIndex >= ArrayPlayers.Count)
            {
                CurrentPlayerIndex = 0;

                // ПРОВЕРКА ГОЛОСОВАНИЯ
                if (IsVotingRound())
                {
                    StartVoting();
                    UpdateAll();
                    return; // Останавливаемся здесь!
                }

                GameRounds++; // Если не голосование, то новый раунд
            }

            // 3. Пропускаем изгнанных
            while (ArrayPlayers[CurrentPlayerIndex].IsEliminated)
            {
                CurrentPlayerIndex++;
                if (CurrentPlayerIndex >= ArrayPlayers.Count)
                {
                    CurrentPlayerIndex = 0;
                    GameRounds++;
                    // Здесь тоже в идеале нужна проверка IsVotingRound, 
                    // если последний в списке игрок был изгнан
                }
            }

            // 4. Подготовка игрока
            var nextPlayer = ArrayPlayers[CurrentPlayerIndex];
            nextPlayer.CurrentOpenedCard = 0;
            nextPlayer.CountNeedOpen = GameRounds == 1 ? 2 : 1;

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

        public void AnalyzeVotesAndEliminate()
        {
            try
            {
                // 1. Считаем результаты
                var votingResults = Votes.Values
                    .GroupBy(id => id)
                    .Select(g => new { PlayerId = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .ToList();

                if (votingResults.Count > 0)
                {
                    // ПРАВИЛЬНОЕ СРАВНЕНИЕ (0 и 1 элементы списка)
                    // Ничья, если кандидатов > 1 И у первого столько же голосов, сколько у второго
                    bool isDraw = votingResults.Count > 1 && votingResults[0].Count == votingResults[1].Count;

                    if (!isDraw)
                    {
                        var loserId = votingResults[0].PlayerId;
                        var loser = ArrayPlayers.FirstOrDefault(p => p.Id == loserId);
                        if (loser != null)
                        {
                            loser.IsEliminated = true;
                            Console.WriteLine($"Игрок {loser.Name} изгнан.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Ничья! Никто не покинул игру.");
                    }
                }

                // 2. ОБЯЗАТЕЛЬНЫЙ СБРОС (без него панель не исчезнет)
                IsVotingActive = false;
                Votes.Clear();

                // 3. ПЕРЕХОД К СЛЕДУЮЩЕМУ ЭТАПУ
                GameRounds++;
                CurrentPlayerIndex = -1; // Сбрасываем, чтобы NextTurn начал с 0

                NextTurn();
                UpdateAll(); // Принудительно уведомляем UI
            }
            catch (Exception ex)
            {
                // Если что-то пойдет не так, вы увидите это в консоли Visual Studio
                Console.WriteLine($"ОШИБКА В ГОЛОСОВАНИИ: {ex.Message}");
            }
        }

        public void FullRestart()
        {
            // 1. Очищаем списки
            ArrayPlayers.Clear();
            Votes.Clear();

            // 2. Сбрасываем системные переменные
            IsGameStarted = false;
            IsVotingActive = false;
            GameRounds = 1;
            CurrentPlayerIndex = 0;
            _isStarting = false;

            // 3. Уведомляем всех клиентов, что нужно перерисовать интерфейс (вернуться в лобби)
            UpdateAll();
        }


        // Использование специального умения
        public bool UseSpecialAbility(int playerId, PlayerFieldType targetTrait = PlayerFieldType.Age)
        {
            var player = ArrayPlayers.FirstOrDefault(p => p.Id == playerId);
            if (player == null || player.SpecialCondition.IsUsed) return false;

            var ability = player.SpecialCondition;

            return ability.Type switch
            {
                CharacterSpecialConditionType.Swap => SwapTrait(playerId, ability.PlayerFieldType, targetTrait),
                CharacterSpecialConditionType.Rerole => RerollTrait(playerId, ability.PlayerFieldType),
                CharacterSpecialConditionType.Upgrade => UpgradeTrait(playerId, ability.PlayerFieldType),
                CharacterSpecialConditionType.Snow => ShowOtherTrait(playerId),
                CharacterSpecialConditionType.SnowYourself => ShowSelfTrait(playerId),
                _ => false,
            };
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

            // Даем возможность открыть дополнительную характеристику у другого игрока
            player.CountNeedOpen += 1;
            player.SpecialCondition.IsUsed = true;
            UpdateAll();
            return true;
        }

        // Показать еще одну свою характеристику
        private bool ShowSelfTrait(int playerId)
        {
            var player = ArrayPlayers.FirstOrDefault(p => p.Id == playerId);
            if (player == null) return false;

            // Увеличиваем лимит открываемых карт для себя
            player.CountNeedOpen += 1;
            player.SpecialCondition.IsUsed = true;
            UpdateAll();
            return true;
        }
    }
}
