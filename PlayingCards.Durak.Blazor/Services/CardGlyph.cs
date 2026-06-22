namespace PlayingCards.Durak.Blazor.Services;

/// <summary>
/// Маппинг числовых rank/suit из view-модели (<c>CardModel</c>) в отображаемые глифы и названия.
/// Единая точка для C#-компонентов (CardView, Deck, бейджи); клиентский дубль для анимаций — в
/// <c>GameTable.razor.js</c> (другой рантайм, импортировать C# нельзя).
/// </summary>
public static class CardGlyph
{
    /// <summary>Масть числом → символ Unicode (♣ ♦ ♥ ♠).</summary>
    public static string Suit(int suit) => suit switch
    {
        0 => "♣",
        1 => "♦",
        2 => "♥",
        3 => "♠",
        _ => "?",
    };

    /// <summary>Масть числом → название для aria-label скринридеров.</summary>
    public static string SuitName(int suit) => suit switch
    {
        0 => "трефы",
        1 => "бубны",
        2 => "черви",
        3 => "пики",
        _ => "неизвестная масть",
    };

    /// <summary>Ранг числом → подпись (числа как есть, картинки — буквой).</summary>
    public static string Rank(int rank) => rank switch
    {
        11 => "J",
        12 => "Q",
        13 => "K",
        14 => "A",
        _ => rank.ToString(),
    };

    /// <summary>Красная масть (бубны/червы) — для подсветки.</summary>
    public static bool IsRed(int suit) => suit is 1 or 2;
}
