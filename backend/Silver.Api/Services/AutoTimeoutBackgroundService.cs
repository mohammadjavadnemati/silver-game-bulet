using Microsoft.AspNetCore.SignalR;
using Silver.Api.Hubs;
using Silver.Api.Models;

namespace Silver.Api.Services;

public class AutoTimeoutBackgroundService : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan TimeoutThreshold = TimeSpan.FromSeconds(60);

    private readonly IServiceProvider _services;

    public AutoTimeoutBackgroundService(IServiceProvider services)
    {
        _services = services;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAllGamesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AutoTimeoutBackgroundService] خطا: {ex.Message}");
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    private async Task CheckAllGamesAsync()
    {
        using var scope = _services.CreateScope();
        var roomService = scope.ServiceProvider.GetRequiredService<RoomService>();
        var gameSessionService = scope.ServiceProvider.GetRequiredService<GameSessionService>();
        var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<GameHub>>();

        foreach (var room in roomService.GetAllInGameRooms())
        {
            var state = await gameSessionService.GetStateAsync(room.RoomCode);
            if (state == null) continue;

            var currentPlayer = room.Players.FirstOrDefault(p => p.PlayerId == state.CurrentPlayerId);
            if (currentPlayer == null || currentPlayer.IsConnected || currentPlayer.DisconnectedAt == null)
                continue;

            if (DateTime.UtcNow - currentPlayer.DisconnectedAt.Value < TimeoutThreshold)
                continue;

            var updatedState = await gameSessionService.ApplyAutoTimeoutForCurrentPlayerAsync(room.RoomCode);
            if (updatedState == null) continue;

            foreach (var player in room.Players.Where(p => p.IsConnected))
            {
                var view = gameSessionService.BuildPlayerFacingState(updatedState, player.PlayerId);
                await hubContext.Clients.Client(player.ConnectionId).SendAsync("GameStateUpdated", view);
            }
        }
    }
}