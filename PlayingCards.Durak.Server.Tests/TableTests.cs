using PlayingCards.Durak.Server;
using static PlayingCards.Durak.Server.Tests.ServerTestHelper;

namespace PlayingCards.Durak.Server.Tests;

[TestFixture]
public class TableTests
{
    private static readonly string[] Hands =
    [
        "A♠ Q♦ Q♣ Q♥ 10♥ 7♦",
        "Q♠ A♦ A♣ A♥ 10♣ 10♠",
    ];

    private Table _table = null!;
    private TablePlayer _p0 = null!;
    private TablePlayer _p1 = null!;

    [SetUp]
    public void SetUp()
    {
        _table = BuildStartedTable(Hands, "6♦", out var players);
        _p0 = players[0];
        _p1 = players[1];
    }

    [Test]
    public void PlayCards_StartAttack_PutsCardOnTableAndBumpsVersion()
    {
        var versionBefore = _table.Version;

        _table.PlayCards(_p0.AuthSecret, [HandIndex(_p0.Player, "A♠")]);

        Assert.Multiple(() =>
        {
            Assert.That(_table.Game.Cards, Has.Count.EqualTo(1));
            Assert.That(_table.Game.Cards[0].AttackCard.ToString(), Is.EqualTo("A♠"));
            Assert.That(_table.Version, Is.GreaterThan(versionBefore));
        });
    }

    [Test]
    public void PlayCards_Defence_CoversAttackCard()
    {
        _table.PlayCards(_p0.AuthSecret, [HandIndex(_p0.Player, "Q♦")]);

        _table.PlayCards(_p1.AuthSecret, [HandIndex(_p1.Player, "A♦")], attackCardIndex: 0);

        Assert.Multiple(() =>
        {
            Assert.That(_table.Game.Cards[0].AttackCard.ToString(), Is.EqualTo("Q♦"));
            Assert.That(_table.Game.Cards[0].DefenceCard?.ToString(), Is.EqualTo("A♦"));
        });
    }

    [Test]
    public void StartAttack_MovesAfkTimerToDefencePlayer()
    {
        _table.PlayCards(_p0.AuthSecret, [HandIndex(_p0.Player, "A♠")]);

        Assert.Multiple(() =>
        {
            Assert.That(_p0.AfkStartTime, Is.Null, "у походившего таймер снимается");
            Assert.That(_p1.AfkStartTime, Is.Not.Null, "защищающемуся таймер ставится");
        });
    }

    [Test]
    public void Take_ByNonDefencePlayer_Throws()
    {
        _table.PlayCards(_p0.AuthSecret, [HandIndex(_p0.Player, "A♠")]);

        var ex = Assert.Throws<BusinessException>(() => _table.Take(_p0.AuthSecret));
        Assert.That(ex!.Message, Is.EqualTo("Забирать карты может только защищающийся"));
    }

    [Test]
    public void Take_WithEmptyTable_Throws()
    {
        var ex = Assert.Throws<BusinessException>(() => _table.Take(_p1.AuthSecret));
        Assert.That(ex!.Message, Is.EqualTo("На столе нет карт"));
    }

    [Test]
    public void Take_StartsStopRoundAndClearsDefenceAfk()
    {
        _table.PlayCards(_p0.AuthSecret, [HandIndex(_p0.Player, "A♠")]);

        _table.Take(_p1.AuthSecret);

        Assert.Multiple(() =>
        {
            Assert.That(_table.StopRoundStatus, Is.EqualTo(StopRoundStatus.Take));
            Assert.That(_table.StopRoundBeginDate, Is.Not.Null);
            Assert.That(_p1.AfkStartTime, Is.Null);
        });
    }

    [Test]
    public void PlayCards_WhenGameNotInProcess_Throws()
    {
        var game = new Game { Deck = new(new SortedDeckCardGenerator(Hands, "6♦")) };
        var p0 = game.AddPlayer("p0");
        game.AddPlayer("p1");

        var table = new Table
        {
            Id = Guid.NewGuid(),
            Number = 1,
            Game = game,
            Players = [new TablePlayer { Player = p0, AuthSecret = "s0" }],
            Owner = p0,
        };

        var ex = Assert.Throws<BusinessException>(() => table.PlayCards("s0", [0]));
        Assert.That(ex!.Message, Is.EqualTo("Игра не в процессе"));
    }
}
