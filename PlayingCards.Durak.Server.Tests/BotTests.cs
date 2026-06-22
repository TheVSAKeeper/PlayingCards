using PlayingCards.Durak;
using PlayingCards.Durak.Server;

namespace PlayingCards.Durak.Server.Tests;

[TestFixture]
public class BotTests
{

    [Test]
    public void AddBot_OwnerSeatsBot_HumanRemainsOwner()
    {
        var holder = new TableHolder();
        var table = holder.CreateTable();
        holder.Join(table.Id, "s0", "Alice");

        holder.AddBot(table.Id, "s0");

        Assert.Multiple(() =>
        {
            Assert.That(table.Players, Has.Count.EqualTo(2));
            Assert.That(table.Players[1].IsBot, Is.True);
            Assert.That(table.Players[1].Player.Name, Is.Not.Empty);
            Assert.That(BotNames.Pool, Does.Contain(table.Players[1].Player.Name));
            Assert.That(table.Owner!.Name, Is.EqualTo("Alice"));
        });
    }

    [Test]
    public void AddBot_ManyBots_NamesAreUnique()
    {
        var holder = new TableHolder();
        var table = holder.CreateTable();
        holder.Join(table.Id, "s0", "Alice");

        for (var i = 0; i < 5; i++)
        {
            holder.AddBot(table.Id, "s0");
        }

        var names = table.Players.Select(x => x.Player.Name).ToArray();
        Assert.That(names, Is.Unique, "двум игрокам за столом не выдаётся одинаковое имя");
    }

    [Test]
    public void AddBot_NonOwner_Throws()
    {
        var holder = new TableHolder();
        var table = holder.CreateTable();
        holder.Join(table.Id, "s0", "Alice");
        holder.Join(table.Id, "s1", "Bob");

        var ex = Assert.Throws<BusinessException>(() => holder.AddBot(table.Id, "s1"));
        Assert.That(ex!.Message, Is.EqualTo("Добавлять ботов может только владелец стола"));
    }

    [Test]
    public void AddBot_UnknownTable_Throws()
    {
        var holder = new TableHolder();

        var ex = Assert.Throws<BusinessException>(() => holder.AddBot(Guid.NewGuid(), "s0"));
        Assert.That(ex!.Message, Is.EqualTo("Стол не найден"));
    }

    [Test]
    public void AddBot_BeyondSixPlayers_Throws()
    {
        var holder = new TableHolder();
        var table = holder.CreateTable();
        holder.Join(table.Id, "s0", "Alice");

        for (var i = 0; i < 5; i++)
        {
            holder.AddBot(table.Id, "s0");
        }

        var ex = Assert.Throws<BusinessException>(() => holder.AddBot(table.Id, "s0"));
        Assert.That(ex!.Message, Is.EqualTo("max player count = 6"));
    }

    [Test]
    public void AddBot_DuringGame_Throws()
    {
        var holder = new TableHolder();
        var table = holder.CreateTable();
        holder.Join(table.Id, "s0", "Alice");
        holder.AddBot(table.Id, "s0");
        table.StartGame();

        var ex = Assert.Throws<BusinessException>(() => holder.AddBot(table.Id, "s0"));
        Assert.That(ex!.Message, Does.Contain("bad status for join"));
    }

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

        Assert.That(BotBrain.DecideMove(table.Game, defender).Kind, Is.EqualTo(BotMoveKind.None));
    }

    [Test]
    public void BotsOnlyTable_BackgroundProcess_PlaysGameToFinish()
    {
        var holder = new TableHolder();
        var table = holder.CreateTable();
        holder.Join(table.Id, "s0", "Owner");
        holder.AddBot(table.Id, "s0");
        holder.AddBot(table.Id, "s0");
        table.Players.Single(x => x.AuthSecret == "s0").IsBot = true;
        table.StartGame();

        var guard = 0;

        while (table.Game.Status == GameStatus.InProcess && guard++ < 5000)
        {
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

    private const string BeatTrump = "6♦";
    private const string BeatAttacker = "7♦ 8♠ 10♠ Q♠ A♠ K♥";
    private const string BeatDefender = "9♠ 9♥ J♠ J♥ Q♥ A♥";
    private const string BeatSecondAttacker = "7♣ 8♣ 9♣ 10♣ J♣ Q♣";

    [Test]
    public void CheckBotBeats_LoneAttackerBotWithNoThrowIn_SaysBeatAndClosesRound()
    {
        var holder = new TableHolder();
        var table = holder.CreateTable();
        table.Game = new Game { Deck = new(new SortedDeckCardGenerator([BeatAttacker, BeatDefender], BeatTrump)) };
        holder.Join(table.Id, "s0", "Owner");
        holder.Join(table.Id, "s1", "Bob");
        table.StartGame();

        var p0 = table.Players.Single(x => x.AuthSecret == "s0");
        var p1 = table.Players.Single(x => x.AuthSecret == "s1");
        p0.IsBot = true;

        table.PlayCards("s0", [ServerTestHelper.HandIndex(p0.Player, "8♠")]);
        table.PlayCards("s1", [ServerTestHelper.HandIndex(p1.Player, "9♠")], 0);
        Assert.That(table.StopRoundStatus, Is.EqualTo(StopRoundStatus.SuccessDefence), "после полного отбоя");

        holder.BackgroundProcess();

        Assert.Multiple(() =>
        {
            Assert.That(p0.SaidBeat, Is.True, "бот объявил «Бито»");
            Assert.That(p0.Reply, Is.Not.Null.And.Not.Empty, "над ботом всплыла реплика");
            Assert.That(table.StopRoundStatus, Is.Null, "единственный атакующий-бот закрыл раунд");
            Assert.That(table.Game.DiscardCardsCount, Is.EqualTo(2), "отбитая пара ушла в бито");
        });
    }

    [Test]
    public void CheckBotBeats_HumanAttackerStillPending_KeepsRoundOpen()
    {
        var holder = new TableHolder();
        var table = holder.CreateTable();
        table.Game = new Game { Deck = new(new SortedDeckCardGenerator([BeatAttacker, BeatDefender, BeatSecondAttacker], BeatTrump)) };
        holder.Join(table.Id, "s0", "Owner");
        holder.Join(table.Id, "s1", "Bob");
        holder.Join(table.Id, "s2", "Cat");
        table.StartGame();

        var p0 = table.Players.Single(x => x.AuthSecret == "s0");
        var p1 = table.Players.Single(x => x.AuthSecret == "s1");
        p0.IsBot = true;

        table.PlayCards("s0", [ServerTestHelper.HandIndex(p0.Player, "8♠")]);
        table.PlayCards("s1", [ServerTestHelper.HandIndex(p1.Player, "9♠")], 0);
        Assert.That(table.StopRoundStatus, Is.EqualTo(StopRoundStatus.SuccessDefence));

        holder.BackgroundProcess();

        Assert.Multiple(() =>
        {
            Assert.That(p0.SaidBeat, Is.True, "бот объявил «Бито»");
            Assert.That(table.StopRoundStatus, Is.EqualTo(StopRoundStatus.SuccessDefence), "ждём человека-атакующего");
            Assert.That(table.StopRoundBeginDate, Is.Not.Null, "общий таймер остаётся");
        });

        table.Beat("s2");
        Assert.That(table.StopRoundStatus, Is.Null, "все атакующие сказали «Бито» — раунд закрыт");
    }
}
