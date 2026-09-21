namespace Silver.Engine;

using Silver.Engine.Cards;

public enum RoundEndReason
{
    None,
    Call,
    FinalRoundDeclared,
    CardsExhausted,
}

public enum GamePhase
{
    WaitingToStart,
    RoundInProgress,
    FinalTurnsAfterCall,
    AwaitingFinalHunterDecisions,
    RoundScoring,
    GameFinished
}

public enum PendingDrawnCardSource
{
    None,
    Deck,
    Discard
}

public class SilverGameState
{
    public required string GameId { get; init; }
    public GamePhase Phase { get; set; } = GamePhase.WaitingToStart;

    public int RoundNumber { get; set; } = 1;
    public const int TotalRounds = 4;

    public List<string> PlayerIdsInTurnOrder { get; init; } = new();
    public Dictionary<string, SilverPlayerVillage> Villages { get; init; } = new();
    public Dictionary<string, int> CumulativeScores { get; init; } = new();

    public string CurrentPlayerId { get; set; } = string.Empty;

    public List<SilverCard> DrawPile { get; init; } = new();
    public List<SilverCard> DiscardPile { get; init; } = new();

    // چه کسی برای این راند "دارنده‌ی آمیولت" حساب می‌شه (تعیین‌کننده‌ی شروع‌کننده‌ی راند بعد + بونوس Call)
    public string? AmuletHolderPlayerId { get; set; }

    public Dictionary<string, int> InitialPeeksUsedByPlayer { get; init; } = new();
    public const int MaxInitialPeeksPerRound = 2;
    public const int InitialPeekDurationSeconds = 10;
    public DateTime InitialPeekDeadlineUtc { get; set; }

    public bool HasBeenCalled { get; set; } = false;
    public string? CallerPlayerId { get; set; }
    public RoundEndReason RoundEndReason { get; set; } = RoundEndReason.None;

    public string? WinnerPlayerId { get; set; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // اکشن جانبی یک‌بارمصرف در نوبت (برای کارت‌های آینده‌ای که چنین چیزی نیاز دارن)
    public bool SideActionUsedThisTurn { get; set; } = false;

    // مکانیزم عمومی برای قابلیت کارتی که منتظر resolve شدنه (کارت‌های جدید ازش استفاده می‌کنن)
    public string? PendingAbilityPlayerId { get; set; }
    public Cards.CardType? PendingAbilityCardType { get; set; }
    public string? PendingAbilityCardId { get; set; }
    public bool AbilityStepUsedThisResolution { get; set; }

    public SilverCard? PendingDrawnCard { get; set; }
    public PendingDrawnCardSource DrawnCardSource { get; set; } = PendingDrawnCardSource.None;

    public Dictionary<string, int> LastRoundScores { get; set; } = new();
    public bool IsFinalRoundDeclared { get; set; }
    public string? FinalRoundDeclarerPlayerId { get; set; }

    // کارت فیزیکی یکتای آمیولت (بعداً وقتی مکانیزم Bullet رو توضیح دادی، اینجا بازنگری می‌شه)
        public SilverCard BulletCard { get; init; } = new SilverCard
    {
        CardId = "bullet-singleton",
        Type = CardType.Bullet,
        IsPubliclyRevealed = true
    };

    // کارتی که Bullet امتیازش رو صفر کرده (فقط برای همین راند، هر راند ریست می‌شه)
    public string? BulletTargetCardId { get; set; }

    // موقع پایان بازی (راند آخر)، بازیکنانی که Hunter رو‌شده دارن و هنوز تصمیم نگرفتن
    public List<string>? PendingHunterDecisionPlayerIds { get; set; }
}


