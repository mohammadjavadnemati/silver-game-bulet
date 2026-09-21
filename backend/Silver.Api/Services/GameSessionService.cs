using Silver.Engine;
using Silver.Engine.Cards;

namespace Silver.Api.Services;

public record PlayerFacingUpdate(
    object PublicState,        // چیزی که همه می‌بینن (بدون مقدار کارت‌های پشت‌ورو)
    Dictionary<string, object> PrivateReveals // playerId -> اطلاعات خصوصی مخصوص همون بازیکن (اگه باشه)
);

public class GameSessionService
{
    private readonly SilverGameEngine _engine = new();
    private readonly IGameStateStore _store;
    private readonly RoomService _roomService;

    public GameSessionService(IGameStateStore store, RoomService roomService)
    {
        _store = store;
        _roomService = roomService;
    }

    public async Task<SilverGameState> StartGameAsync(string roomCode)
    {
        var room = _roomService.GetRoom(roomCode)
            ?? throw new InvalidOperationException("اتاق پیدا نشد.");

        var playerIds = room.Players.Select(p => p.PlayerId).ToList();
        var state = _engine.StartGame(roomCode, playerIds); // از roomCode به‌عنوان gameId استفاده می‌کنیم

        await _store.SaveAsync(state);
        return state;
    }

    public async Task<SilverActionResult> ApplyActionAsync(string gameId, SilverAction action)
    {
        var state = await _store.GetAsync(gameId);
        if (state == null)
            return SilverActionResult.Fail("بازی پیدا نشد.");

        var result = _engine.ApplyAction(state, action);

        // حتی وقتی Fail شده، ممکنه state واقعاً تغییر کرده باشه (نوبت رد شده، کارت سوخته) - پس هر وقت UpdatedState داریم، سیوش کن
        if (result.UpdatedState != null)
            await _store.SaveAsync(result.UpdatedState);

        return result;
    }

    public async Task<SilverGameState?> GetStateAsync(string gameId) => await _store.GetAsync(gameId);
    /// <summary>
    /// وقتی بازیکن فعلی ۶۰ ثانیه قطع بوده: یه کارت می‌کشه و همون رو می‌سوزونه.
    /// اگه قابلیتی نیاز به resolve داشته باشه، Skip می‌شه تا نوبت گیر نکنه.
    /// </summary>
    public async Task<SilverGameState?> ApplyAutoTimeoutForCurrentPlayerAsync(string gameId)
    {
        var state = await _store.GetAsync(gameId);
        if (state == null) return null;

        if (state.Phase != GamePhase.RoundInProgress && state.Phase != GamePhase.FinalTurnsAfterCall)
            return state;

        var playerId = state.CurrentPlayerId;

        var drawResult = _engine.ApplyAction(state, new DrawFromDeckAction { PlayerId = playerId });
        if (drawResult.Success && drawResult.UpdatedState != null)
            state = drawResult.UpdatedState;

        if (state.PendingDrawnCard != null)
        {
            var discardResult = _engine.ApplyAction(state, new DiscardDrawnCardAction
            {
                PlayerId = playerId,
                DrawnCardId = state.PendingDrawnCard.CardId
            });
            if (discardResult.Success && discardResult.UpdatedState != null)
                state = discardResult.UpdatedState;
        }

        if (state.PendingAbilityPlayerId == playerId)
        {
            var skipResult = _engine.ApplyAction(state, new SkipCardAbilityAction { PlayerId = playerId });
            if (skipResult.Success && skipResult.UpdatedState != null)
                state = skipResult.UpdatedState;
        }

        await _store.SaveAsync(state);
        return state;
    }

    /// <summary>
    /// نسخه‌ای از state که برای یک بازیکن خاص امنه: کارت‌های پشت‌ورو دیگران مقدار Type ندارن،
    /// مگر همون کارت‌هایی که به‌واسطه‌ی PrivatelyRevealedCards به این بازیکن نشون داده شده.
    /// </summary>
    public object BuildPlayerFacingState(SilverGameState state, string forPlayerId, Dictionary<string, CardType>? privatelyRevealed = null)
    {
        object MapCard(SilverCard card)
        {
            bool viewerCanSeeType = card.IsPubliclyRevealed
                || (privatelyRevealed != null && privatelyRevealed.ContainsKey(card.CardId));

            return new
            {
                cardId = card.CardId,
                type = viewerCanSeeType ? card.Type.ToString() : null,
                value = viewerCanSeeType ? (int?)card.Value : null,
                isPubliclyRevealed = card.IsPubliclyRevealed
            };
        }

        return new
        {
            gameId = state.GameId,
            phase = state.Phase.ToString(),
            roundNumber = state.RoundNumber,
            initialPeekDeadlineUtc = state.InitialPeekDeadlineUtc,
            currentPlayerId = state.CurrentPlayerId,
            playerIdsInTurnOrder = state.PlayerIdsInTurnOrder,
            cumulativeScores = state.CumulativeScores,
            hasBeenCalled = state.HasBeenCalled,
            isFinalRoundDeclared = state.IsFinalRoundDeclared,
            finalRoundDeclarerPlayerId = state.FinalRoundDeclarerPlayerId,
            callerPlayerId = state.CallerPlayerId,
            drawPileCount = state.DrawPile.Count,
            drawPile = state.DrawPile.AsEnumerable().Reverse().Select(MapCard).ToList(),
            discardPileTop = state.DiscardPile.Count > 0 ? MapCard(state.DiscardPile[^1]) : null,
            discardPile = state.DiscardPile.Select(MapCard).ToList(),
            discardPileCount = state.DiscardPile.Count,
            pendingAbilityPlayerId = state.PendingAbilityPlayerId,
            pendingAbilityCardType = state.PendingAbilityCardType?.ToString(),
            abilityUsedThisTurn = forPlayerId == state.PendingAbilityPlayerId && state.AbilityStepUsedThisResolution,
            drawnCardSource = state.DrawnCardSource.ToString(),
            pendingDrawnCard = state.PendingDrawnCard != null
                ? (forPlayerId == state.CurrentPlayerId
                    ? new
                    {
                        cardId = state.PendingDrawnCard.CardId,
                        type = state.PendingDrawnCard.Type.ToString(),
                        value = (int?)state.PendingDrawnCard.Value,
                        isPubliclyRevealed = false
                    }
                    : new
                    {
                        cardId = state.PendingDrawnCard.CardId,
                        type = (string?)null,
                        value = (int?)null,
                        isPubliclyRevealed = false
                    })
                : null,
            villages = state.Villages.ToDictionary(
                kv => kv.Key,
                kv => new
                {
                    playerId = kv.Key,
                    cards = kv.Value.Cards.Select(MapCard).ToList()
                }
            ),
            winnerPlayerId = state.WinnerPlayerId,
            roundEndReason = state.RoundEndReason.ToString(),
            lastRoundScores = state.LastRoundScores,
            myInitialPeeksRemaining = SilverGameState.MaxInitialPeeksPerRound - state.InitialPeeksUsedByPlayer.GetValueOrDefault(forPlayerId, 0),
            sideActionUsedThisTurn = forPlayerId == state.CurrentPlayerId && state.SideActionUsedThisTurn,
            bulletTargetCardId = state.BulletTargetCardId,
            pendingHunterDecisionPlayerIds = state.PendingHunterDecisionPlayerIds,
        };
    }
}