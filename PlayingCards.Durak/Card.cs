namespace PlayingCards.Durak;

/// <summary>
/// Игральная карта.
/// </summary>
public class Card : IEquatable<Card>
{
    /// <summary>
    /// Игральная карта.
    /// </summary>
    /// <param name="rank">
    ///     <see cref="Rank" />
    /// </param>
    /// <param name="suit">
    ///     <see cref="Suit" />
    /// </param>
    public Card(CardRank rank, CardSuit suit)
    {
        Rank = rank;
        Suit = suit;
    }

    /// <summary>
    /// Старшинство.
    /// </summary>
    public CardRank Rank { get; }

    /// <summary>
    /// Масть.
    /// </summary>
    public CardSuit Suit { get; }

    public override string ToString()
    {
        return Rank.ShortName + Suit.IconChar;
    }

    public bool Equals(Card? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Rank.Equals(other.Rank)
               && Suit.Equals(other.Suit);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != GetType())
        {
            return false;
        }

        return Equals((Card)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Rank, Suit);
    }

    public static bool operator ==(Card? left, Card? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(Card? left, Card? right)
    {
        return !Equals(left, right);
    }
}
