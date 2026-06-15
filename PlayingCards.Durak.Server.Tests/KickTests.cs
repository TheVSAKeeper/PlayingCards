using PlayingCards.Durak;
using PlayingCards.Durak.Server;

namespace PlayingCards.Durak.Server.Tests;

/// <summary>
/// Выгнать игрока из-за стола (issue F2). Владелец кикает любого (бота/человека), всегда;
/// реиспользуется штатный путь Leave, поэтому работают и завершение партии, и «крыса».
/// </summary>
[TestFixture]
public class KickTests
{
    private static int GameIndex(Table table, string secret)
    {
        var player = table.Players.Single(x => x.AuthSecret == secret).Player;
        return table.Game.Players.IndexOf(player);
    }

    [Test]
    public void Kick_OwnerKicksBot_SeatRemoved()
    {
        var holder = new TableHolder();
        var table = holder.CreateTable();
        holder.Join(table.Id, "s0", "Owner");
        holder.AddBot(table.Id, "s0");
        var botIndex = GameIndex(table, table.Players[1].AuthSecret);

        holder.Kick("s0", table.Id, botIndex);

        Assert.Multiple(() =>
        {
            Assert.That(table.Players, Has.Count.EqualTo(1));
            Assert.That(table.Players[0].AuthSecret, Is.EqualTo("s0"));
        });
    }

    [Test]
    public void Kick_NonOwner_Throws()
    {
        var holder = new TableHolder();
        var table = holder.CreateTable();
        holder.Join(table.Id, "s0", "Owner");
        holder.Join(table.Id, "s1", "Bob");

        var ex = Assert.Throws<BusinessException>(() => holder.Kick("s1", table.Id, GameIndex(table, "s0")));
        Assert.That(ex!.Message, Does.Contain("владелец"));
    }

    [Test]
    public void Kick_Self_Throws()
    {
        var holder = new TableHolder();
        var table = holder.CreateTable();
        holder.Join(table.Id, "s0", "Owner");
        holder.Join(table.Id, "s1", "Bob");

        var ex = Assert.Throws<BusinessException>(() => holder.Kick("s0", table.Id, GameIndex(table, "s0")));
        Assert.That(ex!.Message, Does.Contain("самого себя"));
    }

    [Test]
    public void Kick_BadIndex_Throws()
    {
        var holder = new TableHolder();
        var table = holder.CreateTable();
        holder.Join(table.Id, "s0", "Owner");

        var ex = Assert.Throws<BusinessException>(() => holder.Kick("s0", table.Id, 7));
        Assert.That(ex!.Message, Is.EqualTo("Игрок не найден"));
    }

    [Test]
    public void Kick_HumanDuringGame_FinishesAndMarksLeaver()
    {
        var holder = new TableHolder();
        var table = holder.CreateTable();
        holder.Join(table.Id, "s0", "Owner");
        holder.Join(table.Id, "s1", "Bob");
        table.StartGame();
        var bobIndex = GameIndex(table, "s1");

        holder.Kick("s0", table.Id, bobIndex);

        Assert.Multiple(() =>
        {
            Assert.That(table.Players.Any(x => x.AuthSecret == "s1"), Is.False, "кикнутый удалён со стола");
            Assert.That(table.Game.Status, Is.EqualTo(GameStatus.Finish), "выход игрока с картами завершает партию");
            Assert.That(table.LeavePlayer, Is.Not.Null, "кикнутый мид-гейм помечен «крысой»");
        });
    }
}
