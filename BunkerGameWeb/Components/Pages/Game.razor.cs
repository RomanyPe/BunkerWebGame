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
    private PlayerFieldType selectedTargetField = PlayerFieldType.Health;
    private int selectedTargetPlayerId = -1;

    [Parameter] public string RoomId { get; set; } = string.Empty;

    private GameManager? GameManager => string.IsNullOrEmpty(RoomId) ? null : RoomManager.GetGame(RoomId);
    private Player? CurrentPlayer => GameManager?.ArrayPlayers.FirstOrDefault(p => p.Id == MyId);

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
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
        int savedPlayerId = playerIdResult.Success ? playerIdResult.Value : 0;

        await TryJoin(savedPlayerId);
    }

    private async Task TryJoin(int savedPlayerId = 0)
    {
        if (GameManager == null) return;

        if (GameManager.IsGameStarted)
        {
            MyId = -1;
            return;
        }

        // ✅ СНАЧАЛА проверяем, нет ли игрока с таким SessionKey в GameManager.ArrayPlayers
        var existingBySession = GameManager.ArrayPlayers.FirstOrDefault(p => p.SessionKey == _sessionKey);

        if (existingBySession != null)
        {
            // Такой игрок уже существует! Используем его
            MyId = existingBySession.Id;
            existingBySession.IsConnected = true;
            existingBySession.LastSeenUtc = DateTime.UtcNow;

            // Добавляем в активные подключения комнаты
            RoomManager.JoinRoom(RoomId, MyId, _sessionKey);

            Console.WriteLine($"[REJOIN] Игрок {MyId} найден по SessionKey, восстанавливаем");
            return;
        }

        // Проверяем, есть ли игрок с такой сессией в активных подключениях комнаты
        var playersInRoom = RoomManager.GetPlayersWithSessions(RoomId);
        var existingSession = playersInRoom.FirstOrDefault(p => p.SessionKey == _sessionKey);

        if (existingSession.PlayerId != 0)
        {
            MyId = existingSession.PlayerId;
            Console.WriteLine($"[REJOIN] Игрок {MyId} переподключился (сессия {_sessionKey})");
            return;
        }

        // Проверяем, есть ли сохранённый игрок
        if (savedPlayerId != 0)
        {
            var existingPlayer = GameManager.ArrayPlayers.FirstOrDefault(p => p.Id == savedPlayerId);
            if (existingPlayer != null)
            {
                MyId = savedPlayerId;
                existingPlayer.IsConnected = true;
                existingPlayer.LastSeenUtc = DateTime.UtcNow;
                existingPlayer.SessionKey = _sessionKey; // ✅ Обновляем SessionKey
                RoomManager.JoinRoom(RoomId, MyId, _sessionKey);
                Console.WriteLine($"[RESTORE] Игрок {MyId} восстановлен (сессия {_sessionKey})");
                return;
            }
        }

        // Создаём нового игрока (передаём SessionKey)
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

    public async Task HandleStartClick()
    {
        if (GameManager == null) return;
        if (GameManager._isStarting || GameManager.IsGameStarted) return;
        await GameManager.StartGameAsync();
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
        GameManager.ConfirmSelection(MyId);
    }

    private void PassTurn()
    {
        if (CurrentPlayer == null || GameManager == null) return;
        Console.WriteLine($"[UI] PassTurn нажат игроком {MyId}");
        CurrentPlayer.IsSelectionConfirmed = false;
        GameManager.NextTurn();
        StateHasChanged();
    }

    private void ConfirmTurn()
    {
        if (CurrentPlayer == null || GameManager == null) return;
        Console.WriteLine($"[UI] ConfirmTurn нажат игроком {MyId}");

        if (!CurrentPlayer.IsSelectionConfirmed && CurrentPlayer.CurrentOpenedCard == CurrentPlayer.CountNeedOpen)
        {
            GameManager.ConfirmSelection(MyId);
            Console.WriteLine("[UI] Выбор подтвержден");
        }

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

        selectedTargetField = CurrentPlayer.SpecialCondition.PlayerFieldType;

        if (CurrentPlayer.SpecialCondition.Type == CharacterSpecialConditionType.Swap)
        {
            var firstOther = GameManager?.ArrayPlayers.FirstOrDefault(p => p.Id != MyId && !p.IsEliminated);
            if (firstOther != null)
            {
                selectedTargetPlayerId = firstOther.Id;
            }
        }

        showAbilityTargetSelection = true;
    }

    private static string GetAbilityDescription(CharacterSpecialCondition ability) => ability.Type switch
    {
        CharacterSpecialConditionType.Swap => "Обменяться характеристикой с другим игроком",
        CharacterSpecialConditionType.Rerole => "Заменить характеристику на случайную",
        CharacterSpecialConditionType.Upgrade => "Улучшить характеристику (+20%)",
        CharacterSpecialConditionType.Snow => "Показать случайную характеристику другого игрока",
        CharacterSpecialConditionType.SnowYourself => "Показать дополнительную свою характеристику",
        _ => "Неизвестное умение"
    };

    private void UseAbility()
    {
        if (CurrentPlayer == null || GameManager == null) return;

        var success = CurrentPlayer.SpecialCondition.Type switch
        {
            CharacterSpecialConditionType.Swap => GameManager.UseSpecialAbility(MyId, selectedTargetField),
            _ => GameManager.UseSpecialAbility(MyId, selectedTargetField),
        };

        if (success)
        {
            Console.WriteLine("Умение использовано успешно!");
            showAbilityTargetSelection = false;
            GameManager.NextTurn();
        }

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

            if (GameManager == null || CurrentPlayer == null || CurrentCatastrophe == null)
                RoomManager.RemoveRoom(RoomId);
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
}