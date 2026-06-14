using PlayingCards.Durak;
using PlayingCards.Durak.Server;

namespace PlayingCards.Durak.Tests;

[TestFixture]
public class BotTests
{
    // --- TableHolder.AddBot ---

    [Test]
    public void AddBot_SeatsBotAndBecomesOwner()
    {
        var holder = new TableHolder();
        var table = holder.CreateTable();

        holder.AddBot(table.Id);

        Assert.Multiple(() =>
        {
            Assert.That(table.Players, Has.Count.EqualTo(1));
            Assert.That(table.Players[0].IsBot, Is.True);
            Assert.That(table.Players[0].Player.Name, Is.EqualTo("Бот 1"));
            Assert.That(table.Owner, Is.SameAs(table.Players[0].Player));
        });
    }

    [Test]
    public void AddBot_UnknownTable_Throws()
    {
        var holder = new TableHolder();

        var ex = Assert.Throws<BusinessException>(() => holder.AddBot(Guid.NewGuid()));
        Assert.That(ex!.Message, Is.EqualTo("table not found"));
    }

    [Test]
    public void AddBot_BeyondSixPlayers_Throws()
    {
        var holder = new TableHolder();
        var table = holder.CreateTable();

        for (var i = 0; i < 6; i++)
        {
            holder.AddBot(table.Id);
        }

        var ex = Assert.Throws<BusinessException>(() => holder.AddBot(table.Id));
        Assert.That(ex!.Message, Is.EqualTo("max player count = 6"));
    }

    [Test]
    public void AddBot_DuringGame_Throws()
    {
        var holder = new TableHolder();
        var table = holder.CreateTable();
        holder.AddBot(table.Id);
        holder.AddBot(table.Id);
        table.StartGame();

        var ex = Assert.Throws<BusinessException>(() => holder.AddBot(table.Id));
        Assert.That(ex!.Message, Does.Contain("bad status for join"));
    }

    // --- BotBrain.DecideMove ---

    [Test]
    public void DecideMove_ActiveOnEmptyTable_StartsWithLowestNonTrump()
    {
        string[] hands =
        [
            "7♣ 8♣ 9♠ 10♠ J♥ 7♦",
            "8♥ 9♥ 10♥ J♠ Q♠ A♦",
        ];
        var table = ServerTestHelper.BuildStartedTable(hands, "6♦", out _);
        var active = table.Game.ActivePlayer!;
        var trumpSuit = table.Game.Deck.TrumpCard!.Suit;

        var move = BotBrain.DecideMove(table.Game, active);

        var chosen = active.Hand.Cards[move.CardIndexes[0]];
        var minNonTrumpRank = active.Hand.Cards.Where(c => c.Suit != trumpSuit).Min(c => c.Rank.Value);

        Assert.Multiple(() =>
        {
            Assert.That(move.Kind, Is.EqualTo(BotMoveKind.StartAttack));
            Assert.That(move.CardIndexes, Has.Length.EqualTo(1));
            Assert.That(chosen.Suit, Is.Not.EqualTo(trumpSuit), "болванчик предпочитает не-козырь");
            Assert.That(chosen.Rank.Value, Is.EqualTo(minNonTrumpRank), "и наименьший ранг среди не-козырей");
        });
    }

    [Test]
    public void DecideMove_NotMyTurn_Passes()
    {
        string[] hands =
        [
            "7♣ 8♣ 9♠ 10♠ J♥ 7♦",
            "8♥ 9♥ 10♥ J♠ Q♠ A♦",
        ];
        var table = ServerTestHelper.BuildStartedTable(hands, "6♦", out _);
        var defender = table.Game.DefencePlayer!;

        // на пустом столе защитнику нечего отбивать — пас
        Assert.That(BotBrain.DecideMove(table.Game, defender).Kind, Is.EqualTo(BotMoveKind.None));
    }

    // --- Интеграция: драйвер ботов доигрывает партию до конца ---

    [Test]
    public void BotsOnlyTable_BackgroundProcess_PlaysGameToFinish()
    {
        var holder = new TableHolder();
        var table = holder.CreateTable();
        holder.AddBot(table.Id);
        holder.AddBot(table.Id);
        table.StartGame();

        var guard = 0;

        while (table.Game.Status == GameStatus.InProcess && guard++ < 5000)
        {
            // Отсчёт остановки раунда завязан на реальное время; в тесте перематываем его
            // в прошлое, чтобы CheckStopRound завершал раунд и партия двигалась без ожидания.
            if (table.StopRoundBeginDate != null)
            {
                table.StopRoundBeginDate = DateTime.UtcNow.AddSeconds(-30);
            }

            holder.BackgroundProcess();
        }

        Assert.Multiple(() =>
        {
            Assert.That(table.Game.Status, Is.Not.EqualTo(GameStatus.InProcess), "боты обязаны доиграть партию");
            Assert.That(table.Game.LooserPlayer, Is.Not.Null, "в партии на двоих кто-то остаётся дураком");
        });
    }
}
