namespace Silver.Engine;

using Silver.Engine.Cards;

public class SilverPlayerVillage
{
    public required string PlayerId { get; init; }
    public List<SilverCard> Cards { get; init; } = new();

    public int TotalScore(string? bulletZeroedCardId = null)
    {
        var nonCopycatCards = Cards.Where(c => c.Type != CardType.Copycat).ToList();
        int minOtherValue = nonCopycatCards.Count > 0 ? nonCopycatCards.Min(c => c.Value) : 0;

        int SingleCardValue(SilverCard c)
        {
            if (c.CardId == bulletZeroedCardId) return 0;
            if (c.Type == CardType.Copycat) return minOtherValue;
            return c.Value;
        }

        return Cards.Sum(SingleCardValue);
    }
}