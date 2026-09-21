namespace Silver.Engine.Cards;

public record CardDefinition(CardType Type, int Value, string Name, int CountInDeck);

public static class CardDefinitions
{
    public static readonly IReadOnlyDictionary<CardType, CardDefinition> All =
        new Dictionary<CardType, CardDefinition>
        {
            [CardType.Hunter] = new(CardType.Hunter, 0, "Hunter", 2),
            [CardType.Lycan] = new(CardType.Lycan, 1, "Lycan", 4),
            [CardType.Priest] = new(CardType.Priest, 2, "Priest", 4),
            [CardType.GothGirl] = new(CardType.GothGirl, 3, "GothGirl", 4),
            [CardType.Mortician] = new(CardType.Mortician, 4, "Mortician", 4),
            [CardType.Cow] = new(CardType.Cow, 5, "Cow", 4),
            [CardType.Instigator] = new(CardType.Instigator, 6, "Instigator", 4),
            [CardType.Insomnia] = new(CardType.Insomnia, 7, "Insomnia", 4),
            [CardType.Thing] = new(CardType.Thing, 8, "Thing", 4),
            [CardType.Marksman] = new(CardType.Marksman, 9, "Marksman", 4),
            [CardType.TheCount] = new(CardType.TheCount, 10, "TheCount", 4),
            [CardType.Troublemaker] = new(CardType.Troublemaker, 11, "Troublemaker", 4),
            [CardType.Gremlin] = new(CardType.Gremlin, 12, "Gremlin", 4),
            [CardType.Copycat] = new(CardType.Copycat, 13, "Copycat", 2),
            [CardType.Bullet] = new(CardType.Bullet, 0, "Bullet", 0),
        };

    public static int ValueOf(CardType type) => All[type].Value;

    public const int TotalCardsInDeck = 52;

    /// <summary>
    /// یک دسته‌ی کامل و شافل‌نشده از ۵۲ کارت می‌سازه (هر نمونه با CardId یکتا).
    /// شافل کردن در فاز ۴ (موتور بازی) انجام می‌شه، نه اینجا -
    /// این متد فقط مسئول ساخت درست تعداد نسخه‌هاست.
    /// </summary>
    public static List<SilverCard> BuildFullDeck()
    {
        var deck = new List<SilverCard>();
        foreach (var def in All.Values)
        {
            for (int i = 0; i < def.CountInDeck; i++)
            {
                deck.Add(new SilverCard
                {
                    CardId = $"{def.Type}_{i + 1}",
                    Type = def.Type
                });
            }
        }
        return deck;
    }
}