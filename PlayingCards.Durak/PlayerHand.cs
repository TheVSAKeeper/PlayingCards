namespace PlayingCards.Durak;

/// <summary>
/// Рука игрока с картами.
/// </summary>
public class PlayerHand
{
    /// <inheritdoc cref="Cards" />
    private List<Card> _cards;

    /// <summary>
    /// Рука игрока с картами.
    /// </summary>
    public PlayerHand(Game game, Player player)
    {
        _cards = new(6);
        Game = game;
        Player = player;
    }

    /// <summary>
    /// Игрок.
    /// </summary>
    private Player Player { get; }

    /// <summary>
    /// Игра.
    /// </summary>
    public Game Game { get; }

    /// <summary>
    /// Карты в руке.
    /// </summary>
    public IReadOnlyList<Card> Cards => _cards;

    /// <summary>
    /// Взять карту в руку.
    /// </summary>
    /// <param name="card">Карта.</param>
    public void TakeCard(Card card)
    {
        _cards.Add(card);

        SortCards();
    }

    /// <summary>
    /// Начать раунд, сыграв карту.
    /// </summary>
    /// <param name="cardIndexes">Индексы карт.</param>
    public void StartAttack(int[] cardIndexes)
    {
        if (cardIndexes.Length == 0)
        {
            throw new BusinessException("Нужна одна или несколько карт для начала атаки");
        }

        List<Card> cards = [];

        foreach (var cardIndex in cardIndexes)
        {
            if (cardIndex < 0 || cardIndex >= _cards.Count)
            {
                throw new BusinessException($"Карта с индексом {cardIndex} не существует");
            }

            var card = _cards[cardIndex];
            cards.Add(card);
        }

        Game.StartAttack(Player, cards);
    }

    /// <summary>
    /// Подкинуть карту.
    /// </summary>
    /// <param name="cardIndexes">Индексы карт.</param>
    public void Attack(int[] cardIndexes)
    {
        List<Card> cards = [];

        foreach (var cardIndex in cardIndexes)
        {
            if (cardIndex < 0 || cardIndex >= _cards.Count)
            {
                throw new BusinessException($"Карта с индексом {cardIndex} не существует");
            }

            var card = _cards[cardIndex];
            cards.Add(card);
        }

        Game.Attack(Player, cards);
    }

    /// <summary>
    /// Защититься.
    /// </summary>
    /// <param name="defenceCardIndex">Индекс карты.</param>
    /// <param name="attackCardIndex">Индекс атакующей карты.</param>
    public void Defence(int defenceCardIndex, int attackCardIndex)
    {
        if (defenceCardIndex < 0 || defenceCardIndex >= _cards.Count)
        {
            throw new BusinessException($"Карта с индексом {defenceCardIndex} не существует");
        }

        var card = _cards[defenceCardIndex];
        var attackTableCard = Game.Cards[attackCardIndex];
        Game.Defence(Player, card, attackTableCard.AttackCard);
    }

    /// <summary>
    /// Игрок сыграл карту.
    /// </summary>
    /// <param name="cardIndexes">Индексы карт.</param>
    /// <param name="attackCardIndex">Индекс атакующей карты (если это защита).</param>
    public void PlayCards(int[] cardIndexes, int? attackCardIndex = null)
    {
        List<Card> cards = [];

        foreach (var cardIndex in cardIndexes)
        {
            if (cardIndex < 0 || cardIndex >= _cards.Count)
            {
                throw new BusinessException($"Карта с индексом {cardIndex} не существует");
            }

            var card = _cards[cardIndex];
            cards.Add(card);
        }

        var attackCard = attackCardIndex == null ? null : Game.Cards[attackCardIndex.Value].AttackCard;
        Game.PlayCards(Player, cards, attackCard);
    }

    /// <summary>
    /// Очистить руку от карт.
    /// </summary>
    public void Clear()
    {
        _cards = [];
    }

    /// <summary>
    /// Получить карту с минимальным значением козыря.
    /// </summary>
    /// <param name="suit">Масть козыря.</param>
    /// <returns>Карту, если она есть, иначе null.</returns>
    public Card? GetMinSuitCard(CardSuit suit)
    {
        return _cards
            .Where(card => card.Suit == suit)
            .MinBy(card => card.Rank.Value);
    }

    // TODO избавиться от прослойки совместимости
    public void Remove(Card card)
    {
        _cards.Remove(card);
    }

    // TODO избавиться от прослойки совместимости с тестами
    public void RemoveRange(int index, int count)
    {
        _cards.RemoveRange(index, count);
    }

    public override string ToString()
    {
        return string.Join(' ', Cards);
    }

    /// <summary>
    /// Сортировка сначала по старшинству, затем по масти.
    /// </summary>
    /// <remarks>Козырная карта при сортировке по масти будет крайней правой.</remarks>
    private void SortCards()
    {
        _cards = _cards
            .OrderBy(card => card.Rank.Value)
            .ThenBy(card => card.Suit == Game.Deck.TrumpCard.Suit)
            .ThenBy(card => card.Suit.Value)
            .ToList();
    }
}
