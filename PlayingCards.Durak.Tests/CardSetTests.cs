namespace PlayingCards.Durak.Tests;

public class CardSetTests
{
    [TestCase(true, 36, 6)]
    [TestCase(false, 52, 2)]
    public void CardSetTest(bool small, int expectedCount, int expectedMinRank)
    {
        var cards = small ? CardsHolder.SmallDeck : CardsHolder.FullDeck;

        Assert.That(cards, Has.Count.EqualTo(expectedCount));
        Assert.That(cards.Min(x => x.Rank.Value), Is.EqualTo(expectedMinRank));
        Assert.That(cards.Distinct().Count(), Is.EqualTo(expectedCount));
    }

    [Test]
    public void RandomGeneratorUsesSourceCardsTest()
    {
        var cards = new RandomDeckCardGenerator(CardsHolder.FullDeck).GetCards();

        Assert.That(cards, Is.EquivalentTo(CardsHolder.FullDeck));
    }
}
