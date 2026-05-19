using BunkerGameWeb.Helpers;
using BunkerGameWeb.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace BunkerGameWeb.Components.Pages;

public partial class Game : IDisposable
{
    public string CurrentCatastrophe { get; set; } = string.Empty;

    private int MyId = 0;
    private bool showBunkerInfo = false;
    private string _sessionKey = string.Empty;
    private bool showAbilityTargetSelection = false;
    private int selectedTargetPlayerId = -1;

    [Parameter] public string RoomId { get; set; } = string.Empty;

    private GameManager? GameManager => string.IsNullOrEmpty(RoomId) ? null : RoomManager.GetGame(RoomId);
    private Player? CurrentPlayer => GameManager?.ArrayPlayers.FirstOrDefault(p => p.Id == MyId);

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
#if DEBUG
            DebugState();
#endif
            if (string.IsNullOrEmpty(RoomId))
            {
                Navigation.NavigateTo("/lobby");
                return;
            }

            var game = RoomManager?.GetGame(RoomId);
            if (game == null)
            {
                StateHasChanged();
                return;
            }

            await InitializeSessionAndJoin();

            // ✅ Если игра идёт и CurrentPlayer = null - показываем ошибку
            if (game.IsGameStarted && CurrentPlayer == null)
            {
                Console.WriteLine("[ERROR] Игра идёт, но CurrentPlayer не найден!");
            }
            if (MyId >= 0)
            {
                await SessionStorage.SetAsync("PlayerId", MyId);
            }

            StateHasChanged();
        }
    }

    private async Task InitializeSessionAndJoin()
    {
        // Получаем или создаём уникальный ключ сессии
        var sessionResult = await SessionStorage.GetAsync<string>("SessionKey");
        if (sessionResult.Success && !string.IsNullOrEmpty(sessionResult.Value))
        {
            _sessionKey = sessionResult.Value;
        }
        else
        {
            _sessionKey = Guid.NewGuid().ToString();
            await SessionStorage.SetAsync("SessionKey", _sessionKey);
        }

        // Получаем сохранённый ID игрока
        var playerIdResult = await SessionStorage.GetAsync<int>("PlayerId");

        int? savedPlayerId = null;
        if (playerIdResult.Success)
        {
            savedPlayerId = playerIdResult.Value;  // Может быть 0, 1, 2...
        }
        // else savedPlayerId остаётся null

        await TryJoin(savedPlayerId);
    }

    private async Task TryJoin(int? savedPlayerId = null)
    {
        if (GameManager == null)
        {
            MyId = -1;
            return;
        }

        // Если игра идёт - только восстановление существующего игрока
        if (GameManager.IsGameStarted)
        {
#if DEBUG
            DebugState();
#endif
            // 1. Ищем по SessionKey
            var existingBySession = GameManager.ArrayPlayers.FirstOrDefault(p => p.SessionKey == _sessionKey);

            if (existingBySession != null)
            {
                MyId = existingBySession.Id;
                existingBySession.IsConnected = true;
                existingBySession.LastSeenUtc = DateTime.UtcNow;
                existingBySession.SessionKey = _sessionKey;
                RoomManager.JoinRoom(RoomId, MyId, _sessionKey);
                Console.WriteLine($"[REJOIN] Игрок {MyId} ({existingBySession.Name}) восстановлен по SessionKey");
                return;
            }

            // 2. Ищем по сохранённому ID (если есть)
            if (savedPlayerId.HasValue)
            {
                var existingById = GameManager.ArrayPlayers.FirstOrDefault(p => p.Id == savedPlayerId.Value);
                if (existingById != null)
                {
                    MyId = existingById.Id;
                    existingById.IsConnected = true;
                    existingById.LastSeenUtc = DateTime.UtcNow;
                    existingById.SessionKey = _sessionKey;
                    RoomManager.JoinRoom(RoomId, MyId, _sessionKey);
                    Console.WriteLine($"[REJOIN] Игрок {MyId} ({existingById.Name}) восстановлен по SavedId={savedPlayerId}");
                    return;
                }
            }

            // 3. Не нашли - игрок не может играть
            Console.WriteLine($"[ERROR] Не удалось восстановить игрока. SavedId={savedPlayerId}, SessionKey={_sessionKey}");
            MyId = -1;
            return;

        }

        //.......................Код для лобби (игра не началась).......................

        // Проверяем по SessionKey
        var existingBySessionLobby = GameManager.ArrayPlayers.FirstOrDefault(p => p.SessionKey == _sessionKey);
        if (existingBySessionLobby != null)
        {
            MyId = existingBySessionLobby.Id;
            existingBySessionLobby.IsConnected = true;
            existingBySessionLobby.LastSeenUtc = DateTime.UtcNow;
            existingBySessionLobby.SessionKey = _sessionKey;
            RoomManager.JoinRoom(RoomId, MyId, _sessionKey);
            Console.WriteLine($"[REJOIN] Игрок {MyId} восстановлен в лобби по SessionKey");
            return;
        }

        // Проверяем по сохранённому ID
        if (savedPlayerId.HasValue)
        {
            var existingByIdLobby = GameManager.ArrayPlayers.FirstOrDefault(p => p.Id == savedPlayerId.Value);
            if (existingByIdLobby != null)
            {
                MyId = existingByIdLobby.Id;
                existingByIdLobby.IsConnected = true;
                existingByIdLobby.LastSeenUtc = DateTime.UtcNow;
                existingByIdLobby.SessionKey = _sessionKey;
                RoomManager.JoinRoom(RoomId, MyId, _sessionKey);
                Console.WriteLine($"[RESTORE] Игрок {MyId} восстановлен в лобби по SavedId={savedPlayerId}");
                return;
            }
        }

        // Создаём нового игрока
        MyId = GameManager.AddAndInitializePlayer(_sessionKey);
        if (MyId != -1)
        {
            await SessionStorage.SetAsync("PlayerId", MyId);
            RoomManager.JoinRoom(RoomId, MyId, _sessionKey);
            Console.WriteLine($"[JOIN] Новый игрок {MyId} (сессия {_sessionKey})");
        }
    }
    private void HandleNotify()
    {
        try
        {
            if (GameManager != null)
            {
                InvokeAsync(StateHasChanged);
            }
        }
        catch (ObjectDisposedException)
        {
            // Игрок закрыл вкладку
        }
    }

    protected override void OnInitialized()
    {
        GameManager?.OnNotify += HandleNotify;
    }

    public void Dispose()
    {
        GameManager?.OnNotify -= HandleNotify;
        GC.SuppressFinalize(this);
    }

    public void HandleStartClick()
    {
        if (GameManager == null) return;
        if (GameManager._isStarting || GameManager.IsGameStarted) return;
        GameManager.StartGameAsync();
        Console.WriteLine("Запустилось");
    }

    private async Task HandleRestart()
    {
        try
        {
            GameManager?.FullRestart();
            await SessionStorage.DeleteAsync("PlayerId");
            MyId = 0;
            Navigation?.NavigateTo("/", forceLoad: true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при рестарте: {ex.Message}");
            Navigation?.NavigateTo("/", forceLoad: true);
        }
    }

    private async Task LeaveRoom()
    {
        if (!string.IsNullOrEmpty(RoomId) && MyId >= 0 && !string.IsNullOrEmpty(_sessionKey))
        {
            RoomManager.LeaveRoom(RoomId, MyId, _sessionKey);
            await SessionStorage.DeleteAsync("PlayerId");
            await SessionStorage.DeleteAsync("SessionKey");
            StateHasChanged();
            Navigation.NavigateTo("/lobby", forceLoad: true);
        }
    }

    private void Open(PlayerFieldType trait, int pId)
    {
        if (GameManager?.ArrayPlayers == null || GameManager.ArrayPlayers.Count == 0) return;

        var currentPlayer = GameManager.ArrayPlayers.FirstOrDefault(p => p.Id == MyId);
        if (currentPlayer == null) return;

        if (currentPlayer.SpecialCondition.IsUsed)
        {
            return;
        }

        if (trait == PlayerFieldType.SpecialCondition && !currentPlayer.SpecialCondition.IsUsed)
        {
            ShowAbilityPanel();
            return;
        }

        bool alreadySelected = currentPlayer.ListOpenedTypes.Contains(trait);

        if (!alreadySelected)
        {
            if (currentPlayer.CurrentOpenedCard < currentPlayer.CountNeedOpen)
            {
                GameManager.OpenTrait(pId, trait);
            }
        }
        else
        {
            GameManager.OpenTrait(pId, trait);
        }

        StateHasChanged();
    }

    private void ConfirmSelection()
    {
        if (CurrentPlayer == null || GameManager == null) return;
        if (GameManager.ConfirmSelection(MyId))
            PassTurn();
    }

    private void PassTurn()
    {
        if (CurrentPlayer == null || GameManager == null) return;
        Console.WriteLine($"[UI] PassTurn нажат игроком {MyId}");
        CurrentPlayer.IsSelectionConfirmed = false;
        GameManager.NextTurn();
        StateHasChanged();
    }

    private void CancelSelection()
    {
        if (CurrentPlayer == null || GameManager == null) return;

        foreach (var type in CurrentPlayer.PendingOpenedTypes.ToList())
        {
            GameManager.OpenTrait(MyId, type);
        }

        GameManager.CancelSelection(MyId);
    }

    private void ShowAbilityPanel()
    {
        if (CurrentPlayer == null) return;

        // Для Swap - автоматически выбираем первого доступного игрока
        if (CurrentPlayer.SpecialCondition.Type == CharacterSpecialConditionType.Swap)
        {
            var firstOther = GameManager?.ArrayPlayers
                .FirstOrDefault(p => p.Id != MyId && !p.IsEliminated);

            if (firstOther != null)
            {
                selectedTargetPlayerId = firstOther.Id;
            }
        }

        showAbilityTargetSelection = true;
    }
    private static string GetAbilityDescription(CharacterSpecialCondition ability)
    {
        string TypeAbility = ability.Type switch
        {
            CharacterSpecialConditionType.Swap => "Обменяться характеристикой с другим игроком",
            CharacterSpecialConditionType.Rerole => "Заменить характеристику на случайную",
            CharacterSpecialConditionType.Upgrade => "Улучшить характеристику (+20%)",
            CharacterSpecialConditionType.Snow => "Показать случайную характеристику другого игрока",
            _ => "Неизвестное умение"
        };

        string TypeField = ability.PlayerFieldType switch
        {
            PlayerFieldType.BiologicalSex => " (Пол)",
            PlayerFieldType.Age => " (Возраст)",
            PlayerFieldType.Profession => " (Профессия)",
            PlayerFieldType.Health => " (Здоровье)",
            PlayerFieldType.BodyBuild => "(Телосложение)",
            PlayerFieldType.Hobby => " (Хобби)",
            PlayerFieldType.Phobia => " (Фобия)",
            PlayerFieldType.Inventory => " (Инвентарь)",
            PlayerFieldType.Trait => " (Черта характера)",
            PlayerFieldType.AdditionalInformation => " (Дополнительная информация)",
            PlayerFieldType.SpecialCondition => " (Особое условие)",
            PlayerFieldType.Baggage => " (Багаж)",
            PlayerFieldType.Knowledge => " (Знания)",
            PlayerFieldType.Secret => " (Секрет)",
            PlayerFieldType.Reproduction => " (Репродукция)",
            PlayerFieldType.Vision => " (Видение)",
            PlayerFieldType.Equipment => " (Снаряжение)",
            PlayerFieldType.Relation => " (Отношение)",
            _ => " (Неизвестное поле)"
        };

        return TypeAbility + TypeField;
    }

    private void UseAbility()
    {
        if (CurrentPlayer == null || GameManager == null) return;

        // Убираем selectedTargetField, он больше не нужен
        var success = GameManager.UseSpecialAbility(MyId, selectedTargetPlayerId);

        if (success)
        {
            Console.WriteLine("Умение использовано успешно!");
            showAbilityTargetSelection = false;
            GameManager.NextTurn();
        }
        PlayerTraitHelper.SetOpened(CurrentPlayer, PlayerFieldType.SpecialCondition, true);
        StateHasChanged();
    }
    private async Task EmergencyExit()
    {
        try
        {
            // Выходим из комнаты (если есть)
            RoomManager.LeaveRoom(RoomId, MyId, _sessionKey);

            // Полная очистка сессии
            await SessionStorage.DeleteAsync("PlayerId");
            await SessionStorage.DeleteAsync("SessionKey");
            await SessionStorage.DeleteAsync("RoomId");

            // Сбрасываем всё
            MyId = -1;
            _sessionKey = string.Empty;

            if (GameManager == null || CurrentPlayer == null || CurrentCatastrophe == null || GameManager.ArrayPlayers.Count <= 0)
            {
                Console.WriteLine();
                Console.WriteLine($"[DENUG] GameManager = {GameManager != null}");
                Console.WriteLine($"[DENUG] CurrentPlayer = {CurrentPlayer != null}");
                Console.WriteLine($"[DENUG] CurrentCatastrophe = {CurrentCatastrophe != null}");
                if (GameManager != null)
                Console.WriteLine($"[DENUG] Count Players in Room = {GameManager.ArrayPlayers.Count}");
                RoomManager.RemoveRoom(RoomId);
            }
            // Переход в лобби
            Navigation.NavigateTo("/lobby", forceLoad: true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] EmergencyExit: {ex.Message}");
            // Всё равно пытаемся уйти
            Navigation.NavigateTo("/lobby", forceLoad: true);
        }
    }

    private List<Player> GetAvailableTargets()
    {
        if (CurrentPlayer == null || GameManager == null) return [];

        return CurrentPlayer.SpecialCondition.Type switch
        {
            // Переброс своей характеристики
            CharacterSpecialConditionType.Rerole => [.. GameManager.ArrayPlayers.Where(p => !p.IsEliminated)],

            // Показать чужую характеристику
            CharacterSpecialConditionType.Snow => [.. GameManager.ArrayPlayers.Where(p => p.Id != MyId && !p.IsEliminated && !PlayerTraitHelper.IsOpened(p, CurrentPlayer.SpecialCondition.PlayerFieldType))],

            // Обмен характеристикой
            CharacterSpecialConditionType.Swap => [.. GameManager.ArrayPlayers.Where(p => p.Id != MyId && !p.IsEliminated)],

            // Улучшение характеристики
            CharacterSpecialConditionType.Upgrade => [.. GameManager.ArrayPlayers.Where(p => p.Id == MyId && !p.IsEliminated && !PlayerTraitHelper.IsOpened(p, CurrentPlayer.SpecialCondition.PlayerFieldType))],

            _ => []
        };
    }


    private static string GetFieldDescription(PlayerFieldType fieldType)
    {
        return fieldType switch
        {
            PlayerFieldType.BiologicalSex => "Влияет на физические характеристики и возможные мутации. Определяет базовые параметры выживания.",
            PlayerFieldType.Age => "Влияет на выносливость, здоровье и жизненный опыт. Молодые быстрее адаптируются, пожилые имеют больше навыков.",
            PlayerFieldType.Profession => "Определяет ваши профессиональные навыки. Даёт бонусы при определённых действиях в бункере.",
            PlayerFieldType.Health => "Ваше физическое состояние. Влияет на сопротивляемость болезням и радиации. Низкое здоровье может привести к смерти.",
            PlayerFieldType.BodyBuild => "Телосложение влияет на физическую силу, скорость и способность выполнять тяжёлые работы.",
            PlayerFieldType.Hobby => "Хобби даёт дополнительные навыки и возможность проводить время с пользой в бункере.",
            PlayerFieldType.Phobia => "Фобия - ваш страх. В определённых ситуациях может вызвать панику или снизить эффективность.",
            PlayerFieldType.Inventory => "Предметы, которые вы взяли с собой. Могут спасти жизнь или помочь в сложных ситуациях.",
            PlayerFieldType.Trait => "Черта характера определяет ваше поведение. Может помочь или навредить во взаимодействии с другими.",
            PlayerFieldType.AdditionalInformation => "Дополнительная информация о персонаже. Может содержать важные подсказки или сюжетные детали.",
            PlayerFieldType.SpecialCondition => "Особое умение вашего персонажа. Можно использовать один раз за игру для изменения ситуации.",
            PlayerFieldType.Baggage => "Багаж - крупные предметы или груз. Могут быть полезны, но занимают место и замедляют движение.",
            PlayerFieldType.Knowledge => "Знания и навыки. Влияют на способность чинить вещи, готовить еду и другие полезные действия.",
            PlayerFieldType.Secret => "Ваш секрет. Может стать как преимуществом, так и уязвимостью, если другие игроки его узнают.",
            PlayerFieldType.Reproduction => "Биологическая полезность. Влияет на возможность продолжения рода после катастрофы.",
            PlayerFieldType.Vision => "Видение катастрофы - ваше мировоззрение. Влияет на психическое состояние и отношение к другим.",
            PlayerFieldType.Equipment => "Снаряжение и одежда. Защищает от внешних факторов и даёт дополнительные возможности.",
            PlayerFieldType.Relation => "Отношение к другим выжившим. Влияет на социальные взаимодействия и возможные союзы.",
            _ => "Характеристика влияет на различные аспекты выживания в бункере."
        };
    }

    private static string GetFieldExample(PlayerFieldType fieldType)
    {
        return fieldType switch
        {
            PlayerFieldType.BiologicalSex => "Пример: Может влиять на размножение или медицинские процедуры.",
            PlayerFieldType.Age => "Пример: Ребёнок требует больше заботы, но быстрее учится. Пожилой имеет опыт, но слабее здоровьем.",
            PlayerFieldType.Profession => "Пример: Врач может лечить раны, инженер - чинить оборудование.",
            PlayerFieldType.Health => "Пример: Здоровье влияет на шансы выжить при радиации или голоде.",
            PlayerFieldType.BodyBuild => "Пример: Крепкое телосложение даёт бонус к силе, худое - к скрытности.",
            PlayerFieldType.Hobby => "Пример: Рыболов может ловить рыбу, музыкант - поднимать мораль.",
            PlayerFieldType.Phobia => "Пример: Боязнь замкнутого пространства может вызвать панику в бункере.",
            PlayerFieldType.Inventory => "Пример: Аптечка спасёт от смерти, нож поможет в драке или охоте.",
            PlayerFieldType.Trait => "Пример: Храбрость помогает в экстремальных ситуациях, трусость - убегать.",
            PlayerFieldType.AdditionalInformation => "Пример: 'Умеет взламывать замки' - откроет доступ к закрытым помещениям.",
            PlayerFieldType.SpecialCondition => "Пример: 'Обмен характеристиками' - вы можете поменяться полезным навыком с другим игроком.",
            PlayerFieldType.Baggage => "Пример: Генератор даёт электричество, но его тяжело переносить.",
            PlayerFieldType.Knowledge => "Пример: Знание химии поможет создать лекарства или отравить еду.",
            PlayerFieldType.Secret => "Пример: 'Я убил предыдущего лидера' - другие могут изгнать вас, если узнают.",
            PlayerFieldType.Reproduction => "Пример: Высокий шанс передачи генов даёт преимущество при голосовании за выживание.",
            PlayerFieldType.Vision => "Пример: Фаталист не боится смерти, оптимист поддерживает мораль группы.",
            PlayerFieldType.Equipment => "Пример: Противогаз защитит от газов, бронежилет - от пуль.",
            PlayerFieldType.Relation => "Пример: Лидер имеет авторитет, изгой никому не может доверять.",
            _ => "Используйте характеристику в игровых ситуациях согласно логике и вашей фантазии."
        };
    }

    private static string GetFieldDisplayName(PlayerFieldType fieldType) => fieldType switch
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
        PlayerFieldType.SpecialCondition => "Особое условие",
        PlayerFieldType.Baggage => "Багаж",
        PlayerFieldType.Knowledge => "Знания",
        PlayerFieldType.Secret => "Секрет",
        PlayerFieldType.Reproduction => "Репродукция",
        PlayerFieldType.Vision => "Видение",
        PlayerFieldType.Equipment => "Снаряжение",
        PlayerFieldType.Relation => "Отношение",
        _ => "Характеристика"
    };
    private async Task RestartGameInRoom()
    {
        if (GameManager == null) return;

        // 1. Полностью сбрасываем состояние игры
        GameManager.FullRestart();

        // 2. Очищаем данные голосования
        GameManager.Votes.Clear();
        GameManager.IsVotingActive = false;

        // 3. Сбрасываем всех игроков (делаем их живыми и неготовыми)
        foreach (var player in GameManager.ArrayPlayers)
        {
            player.IsEliminated = false;
            player.IsReady = false;
            player.IsWinner = false;
            player.IsSelectionConfirmed = false;
            player.CurrentOpenedCard = 0;
            player.PendingOpenedTypes.Clear();
            player.ListOpenedTypes.Clear();
            player.SpecialCondition.IsUsed = false;
        }

        // 5. Обновляем UI
        GameManager.UpdateAll();
        StateHasChanged();

        Console.WriteLine("[GAME] Игра перезапущена в комнате");
    }

   
    private static string GetTooltipPosition(PlayerFieldType fieldType)
    {
        // Для верхних характеристик (профессия, возраст) - показываем снизу
        var topFields = new[] {
            PlayerFieldType.BiologicalSex,
            PlayerFieldType.Age,
            PlayerFieldType.BodyBuild,
            PlayerFieldType.Reproduction
        };

        return topFields.Contains(fieldType) ? "bottom" : "top";
    }

    private RenderFragment RenderTraitWithTooltip(PlayerFieldType fieldType, string label, bool isOpened, int playerId, string value) => __builder =>
    {
        var title = GetFieldDisplayName(fieldType);
        var description = GetFieldDescription(fieldType);
        var example = GetFieldExample(fieldType);
        var position = GetTooltipPosition(fieldType);

        __builder.OpenElement(0, "div");
        __builder.AddAttribute(1, "class", "trait-tooltip");

        // Характеристика
        __builder.AddContent(2, renderTrait(fieldType, label, isOpened, playerId, value));

        // Тултип
        __builder.OpenElement(3, "div");
        __builder.AddAttribute(4, "class", $"tooltip-popup {position}");

        __builder.OpenElement(5, "div");
        __builder.AddAttribute(6, "class", "tooltip-title");
        __builder.AddContent(7, $"📖 {title}");
        __builder.CloseElement();

        __builder.OpenElement(8, "div");
        __builder.AddAttribute(9, "class", "tooltip-desc");
        __builder.AddContent(10, description);
        __builder.CloseElement();

        __builder.OpenElement(11, "div");
        __builder.AddAttribute(12, "class", "tooltip-example");
        __builder.AddContent(13, $"💡 {example}");
        __builder.CloseElement();

        __builder.CloseElement(); // tooltip-popup
        __builder.CloseElement(); // trait-tooltip
    };

    private void DebugState()
    {
        Console.WriteLine($"[DEBUG] MyId = {MyId}");
        Console.WriteLine($"[DEBUG] RoomId = {RoomId}");
        Console.WriteLine($"[DEBUG] GameManager null? {GameManager == null}");

        if (GameManager != null)
        {
            Console.WriteLine($"[DEBUG] ArrayPlayers.Count = {GameManager.ArrayPlayers.Count}");
            foreach (var p in GameManager.ArrayPlayers)
            {
                Console.WriteLine($"[DEBUG]   Player: Id={p.Id}, Name={p.Name}, SessionKey={p.SessionKey}");
            }
        }

        var playersInRoom = RoomManager.GetPlayersWithSessions(RoomId);
        Console.WriteLine($"[DEBUG] Players in RoomManager: {playersInRoom.Count}");
        foreach (var (pid, skey) in playersInRoom)
        {
            Console.WriteLine($"[DEBUG]   RoomPlayer: Id={pid}, SessionKey={skey}");
        }
    }
}
