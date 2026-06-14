namespace PlayingCards.Durak.Tests;

/// <summary>
/// Тесты, которые проверяют, что метод PlayCards не ломает логику игры.
/// </summary>
[TestFixture]
public class PlayCardsTests
{
    [SetUp]
    public void SetUp()
    {
        string[] playerCards =
        [
            "A♠ Q♦ Q♣ Q♥ 10♥ 7♦",
            "Q♠ A♦ A♣ A♥ 10♣ 10♠",
        ];

        var trumpValue = "6♦";

        _game = new()
        {
            Deck = new(new SortedDeckCardGenerator(playerCards, trumpValue)),
        };

        _game.AddPlayer("1");
        _game.AddPlayer("2");

        _attackPlayer = _game.Players[0];
        _defencePlayer = _game.Players[1];

        _game.StartGame();
    }

    private Game _game;
    private Player _attackPlayer;
    private Player _defencePlayer;

    /// <summary>
    /// Атакующий игрок успешно начинает атаку.
    /// </summary>
    /// <remarks>
    /// Проверяет, что карта атаки добавлена в игровое поле.
    /// </remarks>
    [Test]
    public void StartAttackValidCardsTest()
    {
        _attackPlayer.Hand.PlayCards("A♠");

        Assert.That(_game.Cards, Has.Count.EqualTo(1));
        Assert.That(_game.Cards[0].AttackCard.ToString(), Is.EqualTo("A♠"));
    }

    /// <summary>
    /// Атакующий игрок пытается начать атаку с недопустимыми картами
    /// </summary>
    /// <remarks>
    /// Проверяет, что попытка начать атаку с недопустимыми картами вызывает исключение.
    /// </remarks>
    [Test]
    public void StartAttackInvalidCardsTest()
    {
        Assert.Throws<BusinessException>(() => _attackPlayer.Hand.PlayCards("A♠ Q♦"));
    }

    /// <summary>
    /// Защищающийся игрок успешно отбивается
    /// </summary>
    /// <remarks>
    /// Проверяет, что игрок может сыграть допустимую карту атаки.
    /// Проверяет, что защищающийся игрок может сыграть допустимую карту защиты.
    /// Проверяет, что карты атаки и защиты добавлены в игровое поле.
    /// </remarks>
    [Test]
    public void AttackValidCardsTest()
    {
        _attackPlayer.Hand.PlayCards("Q♦");
        _defencePlayer.Hand.PlayCards("A♦", "Q♦");

        Assert.That(_game.Cards, Has.Count.EqualTo(1));

        Assert.Multiple(() =>
        {
            Assert.That(_game.Cards[0].AttackCard.ToString(), Is.EqualTo("Q♦"));
            Assert.That(_game.Cards[0].DefenceCard?.ToString(), Is.EqualTo("A♦"));
        });
    }

    /// <summary>
    /// Атакующий игрок пытается подкинуть недопустимую карту.
    /// </summary>
    /// <remarks>
    /// Проверяет, что попытка сыграть недопустимую карту атаки вызывает исключение.
    /// </remarks>
    [Test]
    public void AttackInvalidCardsTest()
    {
        _attackPlayer.Hand.PlayCards("Q♦");

        Assert.Throws<BusinessException>(() => _attackPlayer.Hand.PlayCards("7♦"));
    }

    /// <summary>
    /// Защищающийся игрок пытается подкинуть допустимую карту.
    /// </summary>
    /// <remarks>
    /// Проверяет, что защищающийся игрок не может покинуть карту.
    /// </remarks>
    [Test]
    public void AttackInvalidPlayerTest()
    {
        _attackPlayer.Hand.PlayCards("10♥");

        Assert.Throws<BusinessException>(() => _defencePlayer.Hand.PlayCards("10♣"));
    }

    /// <summary>
    /// Защищающийся игрок успешно отбивается допустимой картой.
    /// </summary>
    /// <remarks>
    /// Проверяет, что защищающийся игрок может сыграть допустимую карту защиты.
    /// Проверяет, что карты атаки и защиты добавлены в игровое поле.
    /// </remarks>
    [Test]
    public void DefenceCardSuitValidCardTest()
    {
        _attackPlayer.Hand.PlayCards("Q♦");
        _defencePlayer.Hand.PlayCards("A♦", "Q♦");

        Assert.That(_game.Cards, Has.Count.EqualTo(1));

        Assert.Multiple(() =>
        {
            Assert.That(_game.Cards[0].AttackCard.ToString(), Is.EqualTo("Q♦"));
            Assert.That(_game.Cards[0].DefenceCard?.ToString(), Is.EqualTo("A♦"));
        });
    }

    /// <summary>
    /// Защищающийся игрок пытается отбиться недопустимой картой.
    /// </summary>
    /// <remarks>
    /// Проверяет, что попытка сыграть недопустимую карту защиты вызывает исключение.
    /// </remarks>
    [Test]
    public void DefenceInvalidCardTest()
    {
        _attackPlayer.Hand.PlayCards("Q♦");

        Assert.Throws<BusinessException>(() => _defencePlayer.Hand.PlayCards("10♣", "Q♦"));
    }

    /// <summary>
    /// Атакующий игрок пытается отбить свою же карту.
    /// </summary>
    /// <remarks>
    /// Проверяет, что атакующий игрок не может сыграть допустимую карту защиты.
    /// </remarks>
    [Test]
    public void DefenceInvalidPlayerTest()
    {
        _attackPlayer.Hand.PlayCards("Q♥");

        Assert.Throws<BusinessException>(() => _attackPlayer.Hand.PlayCards("Q♦", "Q♥"));
    }

    /// <summary>
    /// После удачной защиты карты со стола уходят в бито — счётчик отбоя растёт.
    /// </summary>
    /// <remarks>
    /// Проверяет, что до конца раунда счётчик нулевой (обнуляется на старте),
    /// а после удачного отбоя пары атака+защита он равен двум картам.
    /// </remarks>
    [Test]
    public void StopRoundSuccessDefenceIncrementsDiscardCount()
    {
        _attackPlayer.Hand.PlayCards("Q♦");
        _defencePlayer.Hand.PlayCards("A♦", "Q♦");

        Assert.That(_game.DiscardCardsCount, Is.EqualTo(0));

        _game.StopRound();

        Assert.That(_game.DiscardCardsCount, Is.EqualTo(2));
    }

    /// <summary>
    /// Когда защищающийся забирает карты, в бито ничего не уходит — счётчик отбоя не меняется.
    /// </summary>
    [Test]
    public void StopRoundTakeKeepsDiscardCountZero()
    {
        _attackPlayer.Hand.PlayCards("Q♦");

        _game.StopRound();

        Assert.That(_game.DiscardCardsCount, Is.EqualTo(0));
    }
}
