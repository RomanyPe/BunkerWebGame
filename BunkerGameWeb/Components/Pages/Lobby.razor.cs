using BunkerGameWeb.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace BunkerGameWeb.Components.Pages
{
    public partial class Lobby : IDisposable
    {
        [Inject] public NavigationManager Navigation { get; set; } = default!;
        [Inject] public RoomManager RoomManager { get; set; } = default!;
        [Inject] public ProtectedSessionStorage SessionStorage { get; set; } = default!;

        private List<RoomInfo> rooms = [];
        private Timer? refreshTimer;

        protected override void OnInitialized()
        {
            rooms = RoomManager.GetActiveRooms();
            // Автообновление каждые 2 секунды
            refreshTimer = new Timer(async _ => await RefreshRooms(), null,
                TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2));
        }

        private async Task RefreshRooms()
        {
            rooms = RoomManager.GetActiveRooms();
            await InvokeAsync(StateHasChanged);
        }

        private async Task CreateRoom()
        {
            var sessionResult = await SessionStorage.GetAsync<string>("SessionKey");
            string sessionKey;
            if (sessionResult.Success && sessionResult.Value != null)
            {
                sessionKey = sessionResult.Value;
            }
            else
            {
                sessionKey = Guid.NewGuid().ToString();
            }

            if (!sessionResult.Success)
            {
                await SessionStorage.SetAsync("SessionKey", sessionKey);
            }

            string roomId = RoomManager.CreateRoom(sessionKey, "Комната");
            await SessionStorage.SetAsync("RoomId", roomId);
            Navigation.NavigateTo($"/game/{roomId}");
        }

        private async Task JoinRoom(string roomId)
        {
            await SessionStorage.SetAsync("RoomId", roomId);
            Navigation.NavigateTo($"/game/{roomId}");
        }

        public void Dispose()
        {
            refreshTimer?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}