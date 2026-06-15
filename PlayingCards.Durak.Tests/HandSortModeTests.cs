namespace PlayingCards.Durak.Tests;

/// <summary>
/// Режимы сортировки руки (issue F3). Рука одного игрока с перемешанными мастями и козырями
/// различает все три режима; козырь — ♦.
/// </summary>
[TestFixture]
public class HandSortModeTests
{
    private const string Hand0 = "7♣ 9♣ 8♠ 10♦ A♦ J♥";
    private const string Hand1 = "8♣ 10♣ 9♠ Q♦ K♦ 7♥";
    private const string Trump = "6♦";

    private static PlayerHand BuildHand(HandSortMode? mode)
    {
        var game = new Game { Deck = new(new SortedDeckCardGenerator([Hand0, Hand1], Trump)) };
        game.AddPlayer("p0");
        game.AddPlayer("p1");
        game.StartGame();

        var hand = game.Players[0].Hand;

        if (mode != null)
        {
            hand.SetSortMode(mode.Value);
        }

        return hand;
    }

    [TestCase(HandSortMode.ByRankTrumpInline, "7♣ 8♠ 9♣ 10♦ J♥ A♦")]
    [TestCase(HandSortMode.TrumpsSeparated, "7♣ 8♠ 9♣ J♥ 10♦ A♦")]
    [TestCase(HandSortMode.BySuit, "7♣ 9♣ J♥ 8♠ 10♦ A♦")]
    public void SortMode_OrdersHandAsExpected(HandSortMode mode, string expected)
    {
        var hand = BuildHand(mode);

        Assert.That(hand.ToString(), Is.EqualTo(expected));
    }

    [Test]
    public void Default_IsByRankTrumpInline_AndMatchesLegacyOrder()
    {
        var hand = BuildHand(null);

        Assert.Multiple(() =>
        {
            Assert.That(hand.SortMode, Is.EqualTo(HandSortMode.ByRankTrumpInline));
            Assert.That(hand.ToString(), Is.EqualTo("7♣ 8♠ 9♣ 10♦ J♥ A♦"));
        });
    }

    [Test]
    public void SetSortMode_SurvivesHandClear()
    {
        var hand = BuildHand(HandSortMode.TrumpsSeparated);

        hand.Clear();

        Assert.That(hand.SortMode, Is.EqualTo(HandSortMode.TrumpsSeparated));
    }
}
