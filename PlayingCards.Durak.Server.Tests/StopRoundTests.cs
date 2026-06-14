using PlayingCards.Durak.Server;
using static PlayingCards.Durak.Server.Tests.ServerTestHelper;

namespace PlayingCards.Durak.Server.Tests;

/// <summary>
/// Issue #5: при «беру» подкидывать особо некому — отсчёт короче (5с), при удачной защите остаётся 10с.
/// </summary>
[TestFixture]
public class StopRoundTests
{
    private static readonly string[] Hands =
    [
        "A♠ Q♦ Q♣ Q♥ 10♥ 7♦",
        "Q♠ A♦ A♣ A♥ 10♣ 10♠",
    ];

    [TestCase(StopRoundStatus.Take, TableHolder.STOP_ROUND_TAKE_SECONDS)]
    [TestCase(StopRoundStatus.SuccessDefence, TableHolder.STOP_ROUND_SECONDS)]
    public void GetStopRoundSeconds_DependsOnStatus(StopRoundStatus status, int expected)
    {
        Assert.That(TableHolder.GetStopRoundSeconds(status), Is.EqualTo(expected));
    }

    [TestCase(StopRoundStatus.Take, 5)]
    [TestCase(StopRoundStatus.SuccessDefence, 10)]
    public void BuildTable_StopRoundEndDate_UsesStatusDuration(StopRoundStatus status, int expectedSeconds)
    {
        var table = BuildStartedTable(Hands, "6♦", out var players);
        var begin = DateTime.UtcNow;
        table.StopRoundBeginDate = begin;
        table.StopRoundStatus = status;

        var vm = TableViewBuilder.BuildTable(table, players[0]);

        Assert.That(vm.StopRoundEndDate, Is.Not.Null);
        Assert.That((vm.StopRoundEndDate!.Value - begin).TotalSeconds, Is.EqualTo(expectedSeconds).Within(0.001));
    }

    [TestCase(StopRoundStatus.Take, 6, true)]
    [TestCase(StopRoundStatus.Take, 3, false)]
    [TestCase(StopRoundStatus.SuccessDefence, 6, false)]
    [TestCase(StopRoundStatus.SuccessDefence, 11, true)]
    public void BackgroundProcess_StopsRound_ByStatusSpecificTimeout(StopRoundStatus status, int elapsedSeconds, bool shouldFire)
    {
        var holder = new TableHolder();
        var table = holder.CreateTable();
        holder.Join(table.Id, "s1", "Alice");
        holder.Join(table.Id, "s2", "Bob");
        table.StartGame();

        table.StopRoundStatus = status;
        table.StopRoundBeginDate = DateTime.UtcNow.AddSeconds(-elapsedSeconds);

        holder.BackgroundProcess();

        if (shouldFire)
        {
            Assert.Multiple(() =>
            {
                Assert.That(table.StopRoundBeginDate, Is.Null, "раунд должен был остановиться");
                Assert.That(table.StopRoundStatus, Is.Null);
            });
        }
        else
        {
            Assert.That(table.StopRoundBeginDate, Is.Not.Null, "раунд ещё должен тикать");
        }
    }

    [Test]
    public void StopRoundDurations_TakeIsShorterThanSuccessDefence()
    {
        Assert.That(TableHolder.STOP_ROUND_TAKE_SECONDS, Is.LessThan(TableHolder.STOP_ROUND_SECONDS));
    }

    [Test]
    public void Leave_DuringStopRound_Interrupts_ClearsStopRound()
    {
        var (holder, table) = StartedHolderTable(2);
        table.StopRoundStatus = StopRoundStatus.Take;
        table.StopRoundBeginDate = DateTime.UtcNow;

        holder.Leave("s0");

        Assert.Multiple(() =>
        {
            Assert.That(table.Game.Status, Is.Not.EqualTo(GameStatus.InProcess));
            Assert.That(table.StopRoundBeginDate, Is.Null);
            Assert.That(table.StopRoundStatus, Is.Null);
        });
    }

    [Test]
    public void Leave_NoCardsPlayerDuringStopRound_KeepsStopRound()
    {
        var (holder, table) = StartedHolderTable(3);
        var begin = DateTime.UtcNow;
        table.StopRoundStatus = StopRoundStatus.Take;
        table.StopRoundBeginDate = begin;

        var neutral = table.Players.First(x =>
            x.Player != table.Game.ActivePlayer && x.Player != table.Game.DefencePlayer);
        neutral.Player.Hand.Clear();

        holder.Leave(neutral.AuthSecret);

        Assert.Multiple(() =>
        {
            Assert.That(table.Game.Status, Is.EqualTo(GameStatus.InProcess));
            Assert.That(table.StopRoundBeginDate, Is.EqualTo(begin));
            Assert.That(table.StopRoundStatus, Is.EqualTo(StopRoundStatus.Take));
        });
    }

    [Test]
    public void StartGame_ClearsStaleStopRound()
    {
        var game = new Game { Deck = new(new SortedDeckCardGenerator(Hands, "6♦")) };
        game.AddPlayer("p0");
        game.AddPlayer("p1");

        var table = new Table
        {
            Id = Guid.NewGuid(),
            Number = 1,
            Game = game,
            Players =
            [
                new TablePlayer { Player = game.Players[0], AuthSecret = "s0" },
                new TablePlayer { Player = game.Players[1], AuthSecret = "s1" },
            ],
            Owner = game.Players[0],
        };
        table.StopRoundStatus = StopRoundStatus.SuccessDefence;
        table.StopRoundBeginDate = DateTime.UtcNow.AddSeconds(-30);

        table.StartGame();

        Assert.Multiple(() =>
        {
            Assert.That(table.StopRoundBeginDate, Is.Null);
            Assert.That(table.StopRoundStatus, Is.Null);
        });
    }

    private static (TableHolder holder, Table table) StartedHolderTable(int playerCount)
    {
        var holder = new TableHolder();
        var table = holder.CreateTable();

        for (var i = 0; i < playerCount; i++)
        {
            holder.Join(table.Id, "s" + i, "p" + i);
        }

        table.StartGame();
        return (holder, table);
    }
}
