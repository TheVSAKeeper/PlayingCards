using PlayingCards.Durak.Server;

namespace PlayingCards.Durak.Tests;

/// <summary>
/// Issue #6: если игрок без карт (уже вышедший из партии) покидает стол — игра остальных не прерывается.
/// </summary>
[TestFixture]
public class GameLeavePlayerTests
{
    // active = p0 (минимальный козырь 7♦), defence = p1, p2 — сбоку.
    private static readonly string[] ActiveFirstHands =
    [
        "7♦ 8♠ 9♠ 10♠ J♠ Q♠",
        "A♦ 8♥ 9♥ 10♥ J♥ Q♥",
        "8♣ 9♣ 10♣ J♣ Q♣ K♣",
    ];

    // active = p2 (минимальный козырь 7♦), defence = p0 (по кругу).
    private static readonly string[] ActiveLastHands =
    [
        "8♠ 9♠ 10♠ J♠ Q♠ K♠",
        "A♦ 8♥ 9♥ 10♥ J♥ Q♥",
        "7♦ 8♣ 9♣ 10♣ J♣ Q♣",
    ];

    // active = p1 (минимальный козырь 7♦), defence = p2 (последний индекс).
    private static readonly string[] DefenceLastHands =
    [
        "8♠ 9♠ 10♠ J♠ Q♠ K♠",
        "7♦ 8♥ 9♥ 10♥ J♥ Q♥",
        "A♦ 8♣ 9♣ 10♣ J♣ Q♣",
    ];

    private static Game StartGame(string[] hands)
    {
        var game = new Game { Deck = new(new SortedDeckCardGenerator(hands, "6♦")) };
        game.AddPlayer("p0");
        game.AddPlayer("p1");
        game.AddPlayer("p2");
        game.StartGame();
        return game;
    }

    [Test]
    public void LeavePlayer_NoCardsMidGame_GameContinues()
    {
        var game = StartGame(ActiveFirstHands);
        var activeBefore = game.ActivePlayer;
        var defenceBefore = game.DefencePlayer;

        game.Players[2].Hand.Clear();
        game.LeavePlayer(2);

        Assert.Multiple(() =>
        {
            Assert.That(game.Status, Is.EqualTo(GameStatus.InProcess));
            Assert.That(game.Players, Has.Count.EqualTo(2));
            Assert.That(game.ActivePlayer, Is.SameAs(activeBefore));
            Assert.That(game.DefencePlayer, Is.SameAs(defenceBefore));
        });
    }

    [Test]
    public void LeavePlayer_WithCardsMidGame_InterruptsGame()
    {
        var game = StartGame(ActiveFirstHands);

        // ушёл игрок с картами — это прерывает партию (с ≥2 оставшимися движок переводит в ReadyToStart).
        game.LeavePlayer(1);

        Assert.Multiple(() =>
        {
            Assert.That(game.Status, Is.Not.EqualTo(GameStatus.InProcess));
            Assert.That(game.Status, Is.EqualTo(GameStatus.ReadyToStart));
        });
    }

    [Test]
    public void LeavePlayer_NoCardsBeforeTurnPointers_KeepsTurnOnSamePlayers()
    {
        var game = StartGame(ActiveLastHands);
        var activeBefore = game.ActivePlayer;   // p2
        var defenceBefore = game.DefencePlayer; // p0

        game.Players[1].Hand.Clear();           // p1 — левее ходящего/защищающегося
        game.LeavePlayer(1);

        Assert.Multiple(() =>
        {
            Assert.That(game.Status, Is.EqualTo(GameStatus.InProcess));
            Assert.That(game.ActivePlayer, Is.SameAs(activeBefore));
            Assert.That(game.DefencePlayer, Is.SameAs(defenceBefore));
        });
    }

    [Test]
    public void LeavePlayer_InterruptsGame_ClearsTurnPointers()
    {
        // active = p1, defence = p2 (последний индекс). Уходит p0 с картами —
        // партия прерывается, список игроков сжимается до 2 (валидные индексы 0..1).
        var game = StartGame(DefenceLastHands);
        Assert.That(game.DefencePlayer, Is.SameAs(game.Players[2]));

        game.LeavePlayer(0);

        Assert.Multiple(() =>
        {
            Assert.That(game.Status, Is.EqualTo(GameStatus.ReadyToStart));
            Assert.That(game.ActivePlayer, Is.Null);
            // без сброса _defencePlayerIndex обращение к DefencePlayer кидало
            // ArgumentOutOfRangeException (протухший индекс 2 за границей списка из 2 игроков).
            Assert.That(game.DefencePlayer, Is.Null);
        });
    }

    [Test]
    public void TableHolder_Leave_BuildTable_DoesNotThrow()
    {
        // Воспроизводит баг из боя: Leave → Version++ → TableViewBuilder.BuildTable
        // читает DefencePlayer с протухшим индексом.
        var table = ServerTestHelper.BuildStartedTable(DefenceLastHands, "6♦", out var players);
        var holder = new TableHolder();

        holder.Leave(table, players[0]);

        Assert.DoesNotThrow(() => TableViewBuilder.BuildTable(table, players[1]));
    }

    [Test]
    public void BuildTable_ForDepartedPlayer_DoesNotThrow()
    {
        // Воспроизводит баг из боя: Blazor push (Version++) перестраивает вид глазами
        // игрока, который только что вышел и уже удалён из Players (BuildTable падал на First).
        var table = ServerTestHelper.BuildStartedTable(DefenceLastHands, "6♦", out var players);
        var departed = players[0];
        new TableHolder().Leave(table, departed);

        Assert.DoesNotThrow(() => TableViewBuilder.BuildTable(table, departed));

        var view = TableViewBuilder.BuildTable(table, departed);
        Assert.Multiple(() =>
        {
            Assert.That(view.MyPlayerIndex, Is.EqualTo(-1));
            Assert.That(view.MyCards, Is.Empty);
        });
    }

    [Test]
    public void TableHolder_Leave_NoCardsPlayer_KeepsGameInProcess()
    {
        var holder = new TableHolder();
        var table = holder.CreateTable();
        holder.Join(table.Id, "s0", "p0");
        holder.Join(table.Id, "s1", "p1");
        holder.Join(table.Id, "s2", "p2");
        table.StartGame();

        // игрок, который сейчас не ходит и не защищается — имитируем, что он отыгрался (нет карт)
        var neutral = table.Players.First(x =>
            x.Player != table.Game.ActivePlayer && x.Player != table.Game.DefencePlayer);
        neutral.Player.Hand.Clear();

        holder.Leave(neutral.AuthSecret);

        Assert.Multiple(() =>
        {
            Assert.That(table.Game.Status, Is.EqualTo(GameStatus.InProcess));
            Assert.That(table.Players, Has.Count.EqualTo(2));
            Assert.That(holder.GetBySecret(neutral.AuthSecret, out _), Is.Null);
        });
    }
}
