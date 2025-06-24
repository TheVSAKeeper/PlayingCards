namespace PlayingCards.Durak;

/// <summary>
/// Старшинство карты.
/// </summary>
public class CardRank : IEquatable<CardRank>
{
    /// <summary>
    /// Старшинство карты
    /// </summary>
    /// <param name="value">
    ///     <see cref="Value" />
    /// </param>
    /// <param name="name">
    ///     <see cref="Name" />
    /// </param>
    public CardRank(int value, string name)
    {
        Value = value;
        Name = name;
        ShortName = name == "10" ? "10" : name[..1];
    }

    /// <summary>
    /// Значение старшинства.
    /// </summary>
    /// <remarks>
    /// Как правило, чем больше, тем мощнее.
    /// </remarks>
    public int Value { get; }

    /// <summary>
    /// Наименование.
    /// </summary>
    /// <remarks>
    /// Например, дама/queen, 9/девятка.
    /// </remarks>
    public string Name { get; }

    /// <summary>
    /// Короткое наименование.
    /// </summary>
    public string ShortName { get; }

    public override string ToString()
    {
        return Name;
    }

    public static implicit operator int(CardRank rank)
    {
        return rank.Value;
    }

    public bool Equals(CardRank? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Value == other.Value;
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

        return Equals((CardRank)obj);
    }

    public override int GetHashCode()
    {
        return Value;
    }

    public static bool operator ==(CardRank? left, CardRank? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(CardRank? left, CardRank? right)
    {
        return !Equals(left, right);
    }
}
