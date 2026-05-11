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
    private Player? CurrentPlayer => GameManager.ArrayPlayers.FirstOrDefault(p => p.Id == MyId);

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // 1. Пытаемся достать ID из сессии браузера (если игрок обновил страницу)
            var result = await SessionStorage.GetAsync<int>("PlayerId");

            if (result.Success && result.Value != 0)
            {
                // Игрок уже был в игре, восстанавливаем его ID
                MyId = result.Value;
            }
            else
            {
                // 2. Если ID нет (новый игрок), пытаемся присоединиться
                TryJoin();
            }

            StateHasChanged(); // Уведомляем Blazor, что MyId обновился
            if (GameManager.ArrayPlayers.Count == 0 && MyId != -1)
            {
                // Если на сервере пусто, а у нас есть ID — значит был рестарт.
                await SessionStorage.DeleteAsync("PlayerId");
                Navigation.NavigateTo("/", forceLoad: true);
            }
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
    
    private void ConfirmTurn()
    {
        // Выполняем логику (например, сохраняем изменения персонажа)

        // Переключаем ход
        GameManager.NextTurn();

        // Если вы используете Blazor Server и хотите, чтобы у других 
        // экран обновился мгновенно, здесь нужен механизм уведомлений (Action или SignalR)
        StateHasChanged();
    }

    void Open(PlayerFieldType trait, int pId)
    {
        var currentPlayer = GameManager.ArrayPlayers.FirstOrDefault(p => p.Id == MyId);
        if (currentPlayer == null) return;

        bool alreadySelected = currentPlayer.ListOpenedTypes.Contains(trait);

        if (!alreadySelected)
        {
            if (currentPlayer.CurrentOpenedCard < currentPlayer.CountNeedOpen)
            {
                GameManager.OpenTrait(pId, trait);
                currentPlayer.ListOpenedTypes.Add(trait);
                currentPlayer.CurrentOpenedCard++;
            }
        }
        else
        {
            GameManager.CloseTrait(pId, trait);
            currentPlayer.ListOpenedTypes.Remove(trait);
            currentPlayer.CurrentOpenedCard--;
        }
    }


}



