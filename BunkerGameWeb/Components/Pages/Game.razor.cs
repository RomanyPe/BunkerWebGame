using BunkerGameWeb.Models;
using Microsoft.AspNetCore.Components;

namespace BunkerGameWeb.Components.Pages;
// Ключевое слово partial связывает этот файл с .razor файлом
public partial class Game
{
    public string CurrentCatastrophe { get; set; } = string.Empty;

#pragma warning disable CS8618 // Поле, не допускающее значения NULL, должно содержать значение, отличное от NULL, при выходе из конструктора. Рассмотрите возможность добавления модификатора "required" или объявления значения, допускающего значение NULL.
    [Inject] public GameManager GameManager { get; set; }
#pragma warning restore CS8618 // Поле, не допускающее значения NULL, должно содержать значение, отличное от NULL, при выходе из конструктора. Рассмотрите возможность добавления модификатора "required" или объявления значения, допускающего значение NULL.

    private int MyId = 0; // В будущем здесь будет логика определения ID сессии
    private bool showBunkerInfo = false;
    private Player? CurrentPlayer => GameManager.ArrayPlayers.FirstOrDefault(p => p.Id == MyId);

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Пытаемся достать ID из сессии
            var result = await SessionStorage.GetAsync<int>("PlayerId");

            if (result.Success && result.Value != 0)
            {
                // Проверяем, есть ли такой игрок в списке
                var existingPlayer = GameManager.ArrayPlayers.FirstOrDefault(p => p.Id == result.Value);
                if (existingPlayer != null)
                {
                    // Игрок существует - восстанавливаем
                    MyId = result.Value;
                    Console.WriteLine($"[LOG] Игрок {MyId} восстановлен из сессии");
                }
                else
                {
                    // Игрока нет (был рестарт) - создаем нового
                    await SessionStorage.DeleteAsync("PlayerId");
                    TryJoin();
                    Console.WriteLine($"[LOG] Старая сессия удалена, создан новый игрок {MyId}");
                }
            }
            else
            {
                // Нет сессии - создаем нового
                TryJoin();
                Console.WriteLine($"[LOG] Новый игрок {MyId}");
            }

            StateHasChanged();
        }
    }

    private void TryJoin()
    {
        MyId = GameManager.AddAndInitializePlayer();

        if (MyId == -1)
        {
            // Логика для тех, кто не успел: например, перенаправить на страницу "Игра уже идет"
            // или просто показать сообщение в UI
        }
        else
        {
            ValueTask valueTask = SessionStorage.SetAsync("PlayerId", MyId);
            _ = valueTask;
        }
    }

    private async void HandleNotify()
    {
        try
        {
            // InvokeAsync переключает выполнение на "родной" поток этого игрока
            await InvokeAsync(StateHasChanged);
        }
        catch (ObjectDisposedException)
        {
            // Игрок мог уже закрыть вкладку, просто игнорируем
        }
    }
    protected override void OnInitialized()
    {
        // Подписываемся через промежуточный метод
        GameManager.OnNotify += HandleNotify;
    }


    public void Dispose()
    {
        GameManager.OnNotify -= HandleNotify;
        GC.SuppressFinalize(this);
    }

    public async Task HandleStartClick()
    {
        if (GameManager._isStarting || GameManager.IsGameStarted) return;
        // Вызываем общую логику в синглтоне
        await GameManager.StartGameAsync();
        Console.WriteLine("Запустилось");
    }


    void Open(PlayerFieldType trait, int pId)
    {
        if (GameManager.ArrayPlayers == null || GameManager.ArrayPlayers.Count == 0) return;

        var currentPlayer = GameManager.ArrayPlayers.FirstOrDefault(p => p.Id == MyId);
        if (currentPlayer == null) return;

        // ✅ Если умение уже использовано - ход завершен
        if (currentPlayer.SpecialCondition.IsUsed)
        {
            return;
        }

        // Проверяем специальное умение
        if (trait == PlayerFieldType.SpecialCondition && !currentPlayer.SpecialCondition.IsUsed)
        {
            ShowAbilityPanel();
            return;
        }

        // Стандартная логика открытия
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
    private async Task HandleRestart()
    {
        try
        {
            // Сбрасываем данные на сервере
            GameManager?.FullRestart();

            // Очищаем сессию
            if (SessionStorage != null)
            {
                await SessionStorage.DeleteAsync("PlayerId");
            }

            // Сбрасываем локальный ID
            MyId = 0;

            // Переход на главную с полной перезагрузкой
            Navigation?.NavigateTo("/", forceLoad: true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при рестарте: {ex.Message}");
            Navigation?.NavigateTo("/", forceLoad: true);
        }
    }

    private void ConfirmSelection()
    {
        if (CurrentPlayer == null) return;
        GameManager.ConfirmSelection(MyId);
    }

    private void PassTurn()
    {
        if (CurrentPlayer == null) return;
        GameManager.NextTurn();
    }

    private void CancelSelection()
    {
        if (CurrentPlayer == null) return;

        // Закрываем все временно выбранные характеристики
        foreach (var type in CurrentPlayer.PendingOpenedTypes.ToList())
        {
            GameManager.OpenTrait(MyId, type);
        }

        GameManager.CancelSelection(MyId);
    }


    private bool showAbilityTargetSelection = false;
    private PlayerFieldType selectedTargetField = PlayerFieldType.Health;
    private int selectedTargetPlayerId = -1;

    private void ShowAbilityPanel()
    {
        if (CurrentPlayer == null) return;

        // Предзаполняем поле из умения
        selectedTargetField = CurrentPlayer.SpecialCondition.PlayerFieldType;

        // Если тип Swap - выбираем первого доступного игрока
        if (CurrentPlayer.SpecialCondition.Type == CharacterSpecialConditionType.Swap)
        {
            var firstOther = GameManager.ArrayPlayers.FirstOrDefault(p => p.Id != MyId && !p.IsEliminated);
            if (firstOther != null)
            {
                selectedTargetPlayerId = firstOther.Id;
            }
        }

        showAbilityTargetSelection = true;
    }

    private string GetAbilityDescription(CharacterSpecialCondition ability)
    {
        return ability.Type switch
        {
            CharacterSpecialConditionType.Swap => $"Обменяться характеристикой с другим игроком",
            CharacterSpecialConditionType.Rerole => $"Заменить характеристику на случайную",
            CharacterSpecialConditionType.Upgrade => $"Улучшить характеристику (+20%)",
            CharacterSpecialConditionType.Snow => $"Показать случайную характеристику другого игрока",
            CharacterSpecialConditionType.SnowYourself => $"Показать дополнительную свою характеристику",
            _ => "Неизвестное умение"
        };
    }

    private void UseAbility()
    {
        if (CurrentPlayer == null) return;

        bool success = false;

        switch (CurrentPlayer.SpecialCondition.Type)
        {
            case CharacterSpecialConditionType.Swap:
                success = GameManager.UseSpecialAbility(MyId, selectedTargetField);
                break;

            default:
                success = GameManager.UseSpecialAbility(MyId, selectedTargetField);
                break;
        }

        if (success)
        {
            Console.WriteLine($"Умение использовано успешно!");
            showAbilityTargetSelection = false;

            // ✅ ВАЖНО: Умение считается как использование хода
            // Завершаем ход автоматически
            GameManager.NextTurn();
        }

        StateHasChanged();
    }
}
