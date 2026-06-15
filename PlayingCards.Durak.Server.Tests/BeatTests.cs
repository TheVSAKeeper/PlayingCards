using PlayingCards.Durak;
using PlayingCards.Durak.Server;
using PlayingCards.Durak.Tests;

namespace PlayingCards.Durak.Server.Tests;

/// <summary>
/// «Бито» — атакующие вручную закрывают раунд при удачной защите (issue F5). Пришло на смену
/// авто-таймеру «никто не может ходить»: раунд закрывается досрочно, лишь когда «Бито» сказали ВСЕ
/// атакующие, иначе остаётся общий таймер. Над сказавшим всплывает реплика, и сервер больше не
/// выдаёт отсутствие карт у других досрочным завершением.
/// </summary>
[TestFixture]
public class BeatTests
{
    private const string Trump = "6♦";

    private const string Attacker = "7♦ 8♠ 10♠ Q♠ A♠ K♥";
    private const string Defender = "9♠ 9♥ J♠ J♥ Q♥ A♥";
    private const string SecondAttacker = "7♣ 8♣ 9♣ 10♣ J♣ Q♣";

    private static (Table table, Player p0, Player p1) BuildHeldTable()
    {
        var holder = new TableHolder();
        var table = holder.CreateTable();
        table.Game = new Game { Deck = new(new SortedDeckCardGenerator([Attacker, Defender], Trump)) };
        holder.Join(table.Id, "s0", "Owner");
        holder.Join(table.Id, "s1", "Bob");
        table.StartGame();

        var p0 = table.Players.Single(x => x.AuthSecret == "s0").Player;
        var p1 = table.Players.Single(x => x.AuthSecret == "s1").Player;
        return (table, p0, p1);
    }

    /// <summary>Доводит стол на двоих до удачной защиты (полный отбой 8♠ картой 9♠).</summary>
    private static (Table table, Player p0, Player p1) BuildSuccessDefence()
    {
        var (table, p0, p1) = BuildHeldTable();
        table.PlayCards("s0", [ServerTestHelper.HandIndex(p0, "8♠")]);
        table.PlayCards("s1", [ServerTestHelper.HandIndex(p1, "9♠")], 0);

        Assert.That(table.StopRoundStatus, Is.EqualTo(StopRoundStatus.SuccessDefence), "после полного отбоя");
        return (table, p0, p1);
    }

    [Test]
    public void Beat_SingleAttacker_ClosesRoundAndSetsReply()
    {
        var (table, _, _) = BuildSuccessDefence();

        table.Beat("s0");

        var attacker = table.Players.Single(x => x.AuthSecret == "s0");

        Assert.Multiple(() =>
        {
            Assert.That(table.StopRoundStatus, Is.Null, "раунд закрыт");
            Assert.That(table.StopRoundBeginDate, Is.Null);
            Assert.That(table.Game.DiscardCardsCount, Is.EqualTo(2), "отбитая пара ушла в бито");
            Assert.That(attacker.Reply, Is.Not.Null.And.Not.Empty, "над атакующим всплыла реплика");
            Assert.That(attacker.ReplyDate, Is.Not.Null);
        });
    }

    [Test]
    public void Beat_NotAllAttackers_KeepsRoundOpenUntilEveryoneSaysIt()
    {
        var holder = new TableHolder();
        var table = holder.CreateTable();
        table.Game = new Game { Deck = new(new SortedDeckCardGenerator([Attacker, Defender, SecondAttacker], Trump)) };
        holder.Join(table.Id, "s0", "Owner");
        holder.Join(table.Id, "s1", "Bob");
        holder.Join(table.Id, "s2", "Cat");
        table.StartGame();

        var p0 = table.Players.Single(x => x.AuthSecret == "s0").Player;
        var p1 = table.Players.Single(x => x.AuthSecret == "s1").Player;

        table.PlayCards("s0", [ServerTestHelper.HandIndex(p0, "8♠")]);
        table.PlayCards("s1", [ServerTestHelper.HandIndex(p1, "9♠")], 0);
        Assert.That(table.StopRoundStatus, Is.EqualTo(StopRoundStatus.SuccessDefence));

        table.Beat("s0");

        Assert.Multiple(() =>
        {
            Assert.That(table.StopRoundStatus, Is.EqualTo(StopRoundStatus.SuccessDefence), "не все сказали «Бито»");
            Assert.That(table.StopRoundBeginDate, Is.Not.Null, "общий таймер остаётся");
            Assert.That(table.Players.Single(x => x.AuthSecret == "s0").Reply, Is.Not.Null.And.Not.Empty);
        });

        table.Beat("s2");

        Assert.Multiple(() =>
        {
            Assert.That(table.StopRoundStatus, Is.Null, "все атакующие сказали «Бито» — раунд закрыт");
            Assert.That(table.Game.DiscardCardsCount, Is.EqualTo(2), "отбитая пара ушла в бито");
        });
    }

    [Test]
    public void Beat_NotInStopRound_Throws()
    {
        var (table, p0, _) = BuildHeldTable();

        table.PlayCards("s0", [ServerTestHelper.HandIndex(p0, "8♠")]);

        var ex = Assert.Throws<BusinessException>(() => table.Beat("s0"));
        Assert.That(ex!.Message, Is.EqualTo("Сейчас нельзя закрыть раунд"));
    }

    [Test]
    public void Beat_DuringTake_Throws()
    {
        var (table, p0, _) = BuildHeldTable();

        table.PlayCards("s0", [ServerTestHelper.HandIndex(p0, "8♠")]);
        table.Take("s1");

        Assert.That(table.StopRoundStatus, Is.EqualTo(StopRoundStatus.Take));

        var ex = Assert.Throws<BusinessException>(() => table.Beat("s0"));
        Assert.That(ex!.Message, Is.EqualTo("Сейчас нельзя закрыть раунд"));
    }

    [Test]
    public void Beat_ByDefender_Throws()
    {
        var (table, _, _) = BuildSuccessDefence();

        var ex = Assert.Throws<BusinessException>(() => table.Beat("s1"));
        Assert.That(ex!.Message, Is.EqualTo("Защищающийся не закрывает раунд"));
    }
}
