namespace Silver.Engine;

using Silver.Engine.Cards;

public abstract class SilverAction
{
    public required string PlayerId { get; init; }
}

// نوبت با یکی از این ۲ اکشن شروع می‌شه:

public class DrawFromDeckAction : SilverAction { }

public class TakeFromDiscardAction : SilverAction { }

public class CallAction : SilverAction { }

// بعد از DrawFromDeck، تصمیم بعدی:

public class DiscardDrawnCardAction : SilverAction
{
    public required string DrawnCardId { get; init; }
}

public class SwapDrawnCardWithOwnAction : SilverAction
{
    public required string DrawnCardId { get; init; }
    public required List<string> OwnCardIdsToReplace { get; init; }
}

// بعد از TakeFromDiscard:

public class SwapDiscardCardWithOwnAction : SilverAction
{
    public required string DiscardCardId { get; init; }
    public required List<string> OwnCardIdsToReplace { get; init; }
}

// --- اکشن‌های عمومی مستقل از Draw/Discard ---

public class DeclareFinalRoundAction : SilverAction { }

public class StartNextRoundAction : SilverAction { }

public class InitialCardPeekAction : SilverAction
{
    public required string OwnCardId { get; init; }
}

// اکشن عمومی برای رد کردن قابلیت کارتی که در انتظار resolve شدنه
// (کارت‌های جدید که تعریف بشن، اکشن‌های مخصوص خودشون این پایین اضافه می‌شن)
public class SkipCardAbilityAction : SilverAction { }
public class GremlinPenalizeAction : SilverAction
{
    public required string TargetPlayerId { get; init; }
}

public class TroublemakerSwapAction : SilverAction
{
    public required string FirstPlayerId { get; init; }
    public required string FirstCardId { get; init; }
    public required string SecondPlayerId { get; init; }
    public required string SecondCardId { get; init; }
}

public class TheCountBurnTenAction : SilverAction { }

public class MarksmanActivateAction : SilverAction
{
    public required string TargetCardId { get; init; }
}
public class CowFlipDeckAction : SilverAction { }

public class InstigatorFlipCardAction : SilverAction
{
    public required string TargetCardId { get; init; }
}

public class InsomniaViewAllAction : SilverAction { }

public class ThingShuffleVillageAction : SilverAction
{
    public required string TargetPlayerId { get; init; }
}
public class PriestRevealAction : SilverAction
{
    public required List<string> OwnCardIdsToReveal { get; init; }
}

public class HunterRemoveCardAction : SilverAction
{
    public required string CardId { get; init; }
}

public class HunterSkipRemovalAction : SilverAction { }

public class BulletShootAction : SilverAction
{
    public required string TargetCardId { get; init; }
}