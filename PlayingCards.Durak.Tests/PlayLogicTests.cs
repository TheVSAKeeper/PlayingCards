using PlayingCards.Durak;
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

    private static CardModel Card(int rank, int suit) => new() { Rank = rank, Suit = suit };

    [Test]
    public void CanPlayCards_StartAttack_WhenActiveAndTableEmpty()
    {
        var table = new TableModel
        {
            Players = [],
            MyPlayerIndex = 0,
            ActivePlayerIndex = 0,
            DefencePlayerIndex = 1,
            Cards = [],
            MyCards = [Card(7, 0)],
        };

        Assert.That(PlayLogic.CanPlayCards(table, [0], []), Is.True);
    }

    [Test]
    public void CanPlayCards_NoStartAttack_WhenNotMyTurn()
    {
        var table = new TableModel
        {
            Players = [],
            MyPlayerIndex = 1,
            ActivePlayerIndex = 0,
            DefencePlayerIndex = 2,
            Cards = [],
            MyCards = [Card(7, 0)],
        };

        Assert.That(PlayLogic.CanPlayCards(table, [0], []), Is.False);
    }

    [Test]
    public void CanPlayCards_Attack_WhenTableNotEmptyAndNotDefender()
    {
        var table = new TableModel
        {
            Players = [],
            MyPlayerIndex = 0,
            ActivePlayerIndex = 0,
            DefencePlayerIndex = 1,
            Cards = [new TableCardModel { AttackCard = Card(7, 0) }],
            MyCards = [Card(7, 3)],
        };

        Assert.That(PlayLogic.CanPlayCards(table, [0], []), Is.True);
    }

    [Test]
    public void CanPlayCards_DefenderCannotAttack()
    {
        var table = new TableModel
        {
            Players = [],
            MyPlayerIndex = 1,
            ActivePlayerIndex = 0,
            DefencePlayerIndex = 1,
            Cards = [new TableCardModel { AttackCard = Card(7, 0) }],
            MyCards = [Card(8, 0)],
            Trump = Card(6, 1),
        };

        Assert.That(PlayLogic.CanPlayCards(table, [0], []), Is.False);
    }

    [TestCase(14, ExpectedResult = true)]  // та же масть, старше — отбивается
    [TestCase(6, ExpectedResult = false)]  // та же масть, младше — нет
    public bool CanPlayCards_Defence_DependsOnCardStrength(int defenceRank)
    {
        var table = new TableModel
        {
            Players = [],
            MyPlayerIndex = 1,
            ActivePlayerIndex = 0,
            DefencePlayerIndex = 1,
            Cards = [new TableCardModel { AttackCard = Card(7, 0) }],
            MyCards = [Card(defenceRank, 0)],
            Trump = Card(6, 1),
        };

        return PlayLogic.CanPlayCards(table, [0], [0]);
    }

    [Test]
    public void CanPlayCards_NothingSelected_False()
    {
        var table = new TableModel
        {
            Players = [],
            MyPlayerIndex = 0,
            ActivePlayerIndex = 0,
            DefencePlayerIndex = 1,
            Cards = [],
            MyCards = [Card(7, 0)],
        };

        Assert.That(PlayLogic.CanPlayCards(table, [], []), Is.False);
    }

    // --- Гейтинг подсветки карт (защита от ошибок) ---

    private static TableModel AttackerTable(int[] handRanks, params TableCardModel[] field) => new()
    {
        Players = [],
        MyPlayerIndex = 0,
        ActivePlayerIndex = 0,
        DefencePlayerIndex = 1,
        Cards = field,
        MyCards = [.. handRanks.Select(r => Card(r, 0))],
    };

    [Test]
    public void GetPlayableHandIndexes_StartAttack_AllWhenNothingSelected()
    {
        var table = AttackerTable([7, 7, 9]);

        Assert.That(PlayLogic.GetPlayableHandIndexes(table, [], []), Is.EquivalentTo(new[] { 0, 1, 2 }));
    }

    [Test]
    public void GetPlayableHandIndexes_StartAttack_OnlySameRankAfterFirstSelected()
    {
        var table = AttackerTable([7, 7, 9]);

        // выбрана семёрка (индекс 0) — подсвечиваем только семёрки
        Assert.That(PlayLogic.GetPlayableHandIndexes(table, [0], []), Is.EquivalentTo(new[] { 0, 1 }));
    }

    [Test]
    public void GetPlayableHandIndexes_ThrowIn_OnlyRanksPresentOnTable()
    {
        var table = new TableModel
        {
            Players = [],
            MyPlayerIndex = 2,                 // не мой ход и не моя защита — подкидывающий
            ActivePlayerIndex = 0,
            DefencePlayerIndex = 1,
            Cards = [new TableCardModel { AttackCard = Card(7, 0) }],
            MyCards = [Card(7, 3), Card(9, 2)], // 7♠ совпадает по рангу, 9♥ — нет
        };

        Assert.That(PlayLogic.GetPlayableHandIndexes(table, [], []), Is.EquivalentTo(new[] { 0 }));
    }

    [Test]
    public void GetPlayableHandIndexes_Defence_OnlyBeatingCards()
    {
        var table = new TableModel
        {
            Players = [],
            MyPlayerIndex = 1,
            ActivePlayerIndex = 0,
            DefencePlayerIndex = 1,
            Trump = Card(6, 1),                // козырь ♦
            Cards = [new TableCardModel { AttackCard = Card(7, 0) }], // 7♣
            MyCards = [Card(8, 0), Card(6, 1), Card(5, 2)], // 8♣ бьёт, 6♦ козырь бьёт, 5♥ — нет
        };

        Assert.That(PlayLogic.GetPlayableHandIndexes(table, [], []), Is.EquivalentTo(new[] { 0, 1 }));
    }

    [Test]
    public void GetBeatableFieldIndexes_ExcludesBeatenAndUnbeatable()
    {
        var table = new TableModel
        {
            Players = [],
            MyPlayerIndex = 1,
            ActivePlayerIndex = 0,
            DefencePlayerIndex = 1,
            Trump = Card(6, 1),
            Cards =
            [
                new TableCardModel { AttackCard = Card(7, 0) },                          // 0: 7♣ — бьётся 8♣
                new TableCardModel { AttackCard = Card(10, 1), DefenceCard = Card(11, 1) }, // 1: уже отбита — исключить
                new TableCardModel { AttackCard = Card(13, 2) },                         // 2: K♥ — нечем бить
            ],
            MyCards = [Card(8, 0)],
        };

        Assert.That(PlayLogic.GetBeatableFieldIndexes(table, []), Is.EquivalentTo(new[] { 0 }));
    }

    [Test]
    public void GetBeatableFieldIndexes_WithHandSelected_OnlyCardsThatChosenBeats()
    {
        var table = new TableModel
        {
            Players = [],
            MyPlayerIndex = 1,
            ActivePlayerIndex = 0,
            DefencePlayerIndex = 1,
            Trump = Card(6, 1),
            Cards = [new TableCardModel { AttackCard = Card(7, 0) }, new TableCardModel { AttackCard = Card(13, 2) }],
            MyCards = [Card(8, 0)],
        };

        // выбрана 8♣ — бьёт только 7♣ (индекс 0), но не K♥
        Assert.That(PlayLogic.GetBeatableFieldIndexes(table, [0]), Is.EquivalentTo(new[] { 0 }));
    }

    [Test]
    public void GetContextHint_NullWhenNotInProcess()
    {
        var table = new TableModel { Players = [], Status = (int)GameStatus.ReadyToStart };

        Assert.That(PlayLogic.GetContextHint(table), Is.Null);
    }

    [Test]
    public void GetContextHint_AttackerOnEmptyTable()
    {
        var table = new TableModel
        {
            Players = [], Status = (int)GameStatus.InProcess,
            MyPlayerIndex = 0, ActivePlayerIndex = 0, DefencePlayerIndex = 1, Cards = [],
        };

        Assert.That(PlayLogic.GetContextHint(table), Is.EqualTo("Ваш ход — выберите карту для атаки"));
    }

    [Test]
    public void GetContextHint_Defender()
    {
        var table = new TableModel
        {
            Players = [], Status = (int)GameStatus.InProcess,
            MyPlayerIndex = 1, ActivePlayerIndex = 0, DefencePlayerIndex = 1,
            Cards = [new TableCardModel { AttackCard = Card(7, 0) }],
        };

        Assert.That(PlayLogic.GetContextHint(table), Is.EqualTo("Выберите атакующую карту и карту для отбоя"));
    }

    [Test]
    public void GetContextHint_ThrowIn()
    {
        var table = new TableModel
        {
            Players = [], Status = (int)GameStatus.InProcess,
            MyPlayerIndex = 2, ActivePlayerIndex = 0, DefencePlayerIndex = 1,
            Cards = [new TableCardModel { AttackCard = Card(7, 0) }],
        };

        Assert.That(PlayLogic.GetContextHint(table), Is.EqualTo("Можно подкинуть карту того же ранга"));
    }
}
