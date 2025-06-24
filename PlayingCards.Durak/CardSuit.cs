namespace PlayingCards.Durak;

/// <summary>
/// Масть.
/// </summary>
public class CardSuit : IEquatable<CardSuit>
{
    /// <summary>
    /// Масть.
    /// </summary>
    /// <param name="value">
    ///     <see cref="Value" />
    /// </param>
    /// <param name="name">
    ///     <see cref="Name" />
    /// </param>
    /// <param name="iconChar">
    ///     <see cref="IconChar" />
    /// </param>
    public CardSuit(int value, string name, char iconChar)
    {
        Value = value;
        Name = name;
        IconChar = iconChar;
    }

    /// <summary>
    /// Значение.
    /// </summary>
    /// <remarks>Например, 0 - черви, 1 - пики.</remarks>
    public int Value { get; }

    /// <summary>
    /// Наименование.
    /// </summary>
    /// <remarks>
    /// Например: Черви, Буби.
    /// </remarks>
    public string Name { get; }

    /// <summary>
    /// Сокращённое наименование.
    /// </summary>
    /// <remarks>♥/♦/♣/♠</remarks>
    public char IconChar { get; }

    public override string ToString()
    {
        return IconChar.ToString();
    }

    public static implicit operator int(CardSuit suit)
    {
        return suit.Value;
    }

    public bool Equals(CardSuit? other)
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

        return Equals((CardSuit)obj);
    }

    public override int GetHashCode()
    {
        return Value;
    }

    public static bool operator ==(CardSuit? left, CardSuit? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(CardSuit? left, CardSuit? right)
    {
        return !Equals(left, right);
    }
}
