using PlayingCards.Durak.Server;
using static PlayingCards.Durak.Server.GetStatusModel;

namespace PlayingCards.Durak.Tests;

[TestFixture]
public class PlayLogicTests
{
    // Козырь — ♦ (suit 1)
    private static readonly CardModel Trump = new() { Rank = 6, Suit = 1 };

    [TestCase(2, 2, 13, 12, true)]   // защита козырем по козырю, старше — ок
    [TestCase(2, 2, 12, 13, false)]  // защита козырем по козырю, младше — нет
    [TestCase(0, 1, 7, 14, true)]    // козырем (1) по не-козырю (0) — ок
    [TestCase(0, 3, 14, 7, false)]   // не-козырь по не-козырю иной масти — нет
    [TestCase(3, 3, 14, 13, true)]   // одна масть не-козырь, старше — ок
    public void IsValidDefence_Cases(int attackSuit, int defenceSuit, int defenceRank, int attackRank, bool expected)
    {
        var attack = new CardModel { Rank = attackRank, Suit = attackSuit };
        var defence = new CardModel { Rank = defenceRank, Suit = defenceSuit };

        Assert.That(PlayLogic.IsValidDefence(attack, defence, Trump), Is.EqualTo(expected));
    }
}
