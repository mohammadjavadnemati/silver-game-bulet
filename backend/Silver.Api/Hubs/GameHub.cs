using Silver.Api.Models;     // ← این خط برای Room لازمه
using Silver.Engine.Cards;   // ← این خط برای CardType در MapAction لازمه (چون از Cards.CardType استفاده کردیم)
using Microsoft.AspNetCore.SignalR;
using Silver.Api.Services;
using Silver.Engine;

namespace Silver.Api.Hubs;

public class GameHub : Hub
{
    private readonly RoomService _roomService;
    private readonly GameSessionService _gameSessionService;

    public GameHub(RoomService roomService, GameSessionService gameSessionService)
    {
        _roomService = roomService;
        _gameSessionService = gameSessionService;
    }

    // --- اتاق (از فاز ۲، بدون تغییر) ---

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var room = _roomService.GetRoomByConnectionId(Context.ConnectionId);
        _roomService.MarkDisconnected(Context.ConnectionId);
        if (room != null)
            await Clients.Group(room.RoomCode).SendAsync("RoomUpdated", MapRoom(room));
        await base.OnDisconnectedAsync(exception);
    }

    public async Task<object> CreateRoom(string playerId, string playerName)
    {
        var room = _roomService.CreateRoom(Context.ConnectionId, playerId, playerName);
        await Groups.AddToGroupAsync(Context.ConnectionId, room.RoomCode);
        await Clients.Group(room.RoomCode).SendAsync("RoomUpdated", MapRoom(room));
        return MapRoom(room);
    }

    public async Task<object> JoinRoom(string roomCode, string playerId, string playerName)
    {
        var (success, error, room) = _roomService.JoinRoom(roomCode.ToUpperInvariant(), Context.ConnectionId, playerId, playerName);
        if (!success || room == null) return new { success = false, error };

        await Groups.AddToGroupAsync(Context.ConnectionId, room.RoomCode);
        await Clients.Group(room.RoomCode).SendAsync("RoomUpdated", MapRoom(room));

        // اگه بازی از قبل شروع شده (وصل مجدد)، بلافاصله state فعلی رو فقط برای همین کلاینت بفرست
        if (room.Status == Models.RoomStatus.InGame)
        {
            var state = await _gameSessionService.GetStateAsync(room.RoomCode);
            if (state != null)
            {
                var view = _gameSessionService.BuildPlayerFacingState(state, playerId);
                await Clients.Caller.SendAsync("GameStateUpdated", view);
            }
        }

        return new { success = true, room = MapRoom(room) };
    }

    // --- بازی (جدید) ---

    public async Task<object> StartGame(string roomCode)
    {
        try
        {
            var state = await _gameSessionService.StartGameAsync(roomCode);
            _roomService.SetRoomStatus(roomCode, Models.RoomStatus.InGame);
            await BroadcastGameState(roomCode, state);
            return new { success = true };
        }
        catch (Exception ex)
        {
            return new { success = false, error = ex.Message };
        }
    }

    /// <summary>
    /// نقطه‌ی ورود واحد برای همه‌ی اکشن‌های بازی. کلاینت نوع اکشن رو مشخص می‌کنه،
    /// سرور اکشن رو می‌سازه، اعتبارسنجی می‌کنه، و نتیجه رو broadcast می‌کنه.
    /// </summary>
    public async Task<object> SendGameAction(string roomCode, string actionType, Dictionary<string, object> payload)
    {
        try
        {
            var playerId = GetPlayerIdFromPayload(payload);

            SilverAction? action = BuildAction(actionType, playerId, payload);
            if (action == null)
                return new { success = false, error = "نوع اکشن نامعتبر است." };

            var result = await _gameSessionService.ApplyActionAsync(roomCode, action);

            if (result.UpdatedState != null)
            {
                await BroadcastGameState(roomCode, result.UpdatedState, result.PrivatelyRevealedCards, forPlayerId: playerId);
            }

            if (!result.Success)
                return new { success = false, error = result.ErrorMessage };

            return new { success = true };
        }
        catch (Exception ex)
        {
            return new { success = false, error = $"خطای غیرمنتظره: {ex.Message}" };
        }
    }

    private async Task BroadcastGameState(
        string roomCode,
        SilverGameState state,
        Dictionary<string, CardType>? privateInfoForActingPlayer = null,
        string? forPlayerId = null)
    {
        var room = _roomService.GetRoom(roomCode);
        if (room == null) return;

        foreach (var player in room.Players.Where(p => p.IsConnected))
        {
            var privateInfo = player.PlayerId == forPlayerId ? privateInfoForActingPlayer : null;
            var view = _gameSessionService.BuildPlayerFacingState(state, player.PlayerId, privateInfo);
            // myInitialPeeksRemaining = SilverGameState.MaxInitialPeeksPerRound - state.InitialPeeksUsedByPlayer.GetValueOrDefault(forPlayerId, 0);
            await Clients.Client(player.ConnectionId).SendAsync("GameStateUpdated", view);
            if (player.PlayerId == forPlayerId && privateInfo != null && privateInfo.Count > 0)
            {
                var revealPayload = privateInfo.ToDictionary(kv => kv.Key, kv => new { type = kv.Value.ToString(), value = (int)kv.Value });
                await Clients.Client(player.ConnectionId).SendAsync("PrivateCardsRevealed", revealPayload);
            }
        }
    }

    private static object MapRoom(Room room) => new
    {
        roomCode = room.RoomCode,
        status = room.Status.ToString(),
        players = room.Players.Select(p => new
        {
            playerId = p.PlayerId,
            name = p.Name,
            isHost = p.IsHost,
            isConnected = p.IsConnected
        })
    };

    private static string GetPlayerIdFromPayload(Dictionary<string, object> payload)
        => payload.TryGetValue("playerId", out var v) ? v.ToString()! : throw new InvalidOperationException("playerId الزامی است.");

    private static SilverAction? BuildAction(string actionType, string playerId, Dictionary<string, object> p)
    {
        string S(string key) => p[key].ToString()!;
        List<string> L(string key) => ((System.Text.Json.JsonElement)p[key]).EnumerateArray().Select(x => x.GetString()!).ToList();

        return actionType switch
        {
            "DrawFromDeck" => new DrawFromDeckAction { PlayerId = playerId },
            "DeclareFinalRound" => new DeclareFinalRoundAction { PlayerId = playerId },
            "TakeFromDiscard" => new TakeFromDiscardAction { PlayerId = playerId },
            "Call" => new CallAction { PlayerId = playerId },
            "DiscardDrawn" => new DiscardDrawnCardAction { PlayerId = playerId, DrawnCardId = S("drawnCardId") },
            "SwapDrawn" => new SwapDrawnCardWithOwnAction { PlayerId = playerId, DrawnCardId = S("drawnCardId"), OwnCardIdsToReplace = L("ownCardIds") },
            "SwapDiscard" => new SwapDiscardCardWithOwnAction { PlayerId = playerId, DiscardCardId = S("discardCardId"), OwnCardIdsToReplace = L("ownCardIds") },
            "SkipAbility" => new SkipCardAbilityAction { PlayerId = playerId },
            "InitialPeek" => new InitialCardPeekAction { PlayerId = playerId, OwnCardId = S("ownCardId") },
            "StartNextRound" => new StartNextRoundAction { PlayerId = playerId },
            "GremlinPenalize" => new GremlinPenalizeAction { PlayerId = playerId, TargetPlayerId = S("targetPlayerId") },
            "TroublemakerSwap" => new TroublemakerSwapAction
            {
                PlayerId = playerId,
                FirstPlayerId = S("firstPlayerId"),
                FirstCardId = S("firstCardId"),
                SecondPlayerId = S("secondPlayerId"),
                SecondCardId = S("secondCardId")
            },
            "TheCountBurnTen" => new TheCountBurnTenAction { PlayerId = playerId },
            "MarksmanActivate" => new MarksmanActivateAction { PlayerId = playerId, TargetCardId = S("targetCardId") },
            "CowFlipDeck" => new CowFlipDeckAction { PlayerId = playerId },
            "InstigatorFlip" => new InstigatorFlipCardAction { PlayerId = playerId, TargetCardId = S("targetCardId") },
            "InsomniaViewAll" => new InsomniaViewAllAction { PlayerId = playerId },
            "ThingShuffleVillage" => new ThingShuffleVillageAction { PlayerId = playerId, TargetPlayerId = S("targetPlayerId") },
            "PriestReveal" => new PriestRevealAction { PlayerId = playerId, OwnCardIdsToReveal = L("ownCardIds") },
            "HunterRemoveCard" => new HunterRemoveCardAction { PlayerId = playerId, CardId = S("cardId") },
            "HunterSkipRemoval" => new HunterSkipRemovalAction { PlayerId = playerId },
            "BulletShoot" => new BulletShootAction { PlayerId = playerId, TargetCardId = S("targetCardId") },
            // کارت‌های جدید که تعریف بشن، case مخصوص خودشون اینجا اضافه می‌شه
            _ => null
        };
    }
}