using PlayingCards.Durak.Server;

namespace PlayingCards.Durak.Server.Tests;

[TestFixture]
public class TableViewBuilderTests
{
    private Table _table = null!;
    private TablePlayer _me = null!;

    [SetUp]
    public void SetUp()
    {
        string[] playerCards =
        [
            "A♠ Q♦ Q♣ Q♥ 10♥ 7♦",
            "Q♠ A♦ A♣ A♥ 10♣ 10♠",
        ];

        var game = new Game
        {
            Deck = new(new SortedDeckCardGenerator(playerCards, "6♦")),
        };

        var p0 = game.AddPlayer("me");
        var p1 = game.AddPlayer("opponent");

        _table = new Table
        {
            Id = Guid.NewGuid(),
            Number = 1,
            Game = game,
            Players =
            [
                new TablePlayer { Player = p0, AuthSecret = "s0" },
                new TablePlayer { Player = p1, AuthSecret = "s1" },
            ],
        };
        _table.Owner = p0;

        game.StartGame();
        _me = _table.Players[0];
    }

    [Test]
    public void BuildTable_FillsMyIdentityIndexes()
    {
        var vm = TableViewBuilder.BuildTable(_table, _me);

        Assert.That(vm.MyPlayerIndex, Is.EqualTo(0));
        Assert.That(vm.OwnerIndex, Is.EqualTo(0));
        Assert.That(vm.Status, Is.EqualTo((int)GameStatus.InProcess));
    }

    [Test]
    public void BuildTable_MyCardsMatchHandCountAndOrder()
    {
        var vm = TableViewBuilder.BuildTable(_table, _me);

        Assert.That(vm.MyCards, Has.Length.EqualTo(6));
        Assert.That(vm.MyCards![0].Rank, Is.EqualTo(_me.Player.Hand.Cards[0].Rank.Value));
        Assert.That(vm.MyCards![0].Suit, Is.EqualTo(_me.Player.Hand.Cards[0].Suit.Value));
    }

    [Test]
    public void BuildTable_OtherPlayersExcludeMe()
    {
        var vm = TableViewBuilder.BuildTable(_table, _me);

        Assert.That(vm.Players, Has.Length.EqualTo(1));
        Assert.That(vm.Players[0].Name, Is.EqualTo("opponent"));
        Assert.That(vm.Players[0].CardsCount, Is.EqualTo(6));
    }

    [Test]
    public void BuildTable_FreshReplyShown_StaleHidden()
    {
        var opponent = _table.Players[1];

        _me.Reply = "Закрываю";
        _me.ReplyDate = DateTime.UtcNow;
        opponent.Reply = "Бито!";
        opponent.ReplyDate = DateTime.UtcNow;

        var fresh = TableViewBuilder.BuildTable(_table, _me);

        var stale = DateTime.UtcNow.AddSeconds(-(TableHolder.REPLY_SECONDS + 1));
        _me.ReplyDate = stale;
        opponent.ReplyDate = stale;

        var staleVm = TableViewBuilder.BuildTable(_table, _me);

        Assert.Multiple(() =>
        {
            Assert.That(fresh.MyReply, Is.EqualTo("Закрываю"), "своя свежая реплика");
            Assert.That(fresh.Players[0].Reply, Is.EqualTo("Бито!"), "свежая реплика соперника");
            Assert.That(staleVm.MyReply, Is.Null, "своя протухшая реплика скрыта");
            Assert.That(staleVm.Players[0].Reply, Is.Null, "протухшая реплика соперника скрыта");
        });
    }

    [Test]
    public void BuildLobby_ReturnsTableWithPlayerNames()
    {
        var result = TableViewBuilder.BuildLobby([_table]);

        Assert.That(result, Has.Length.EqualTo(1));
        Assert.That(result[0].Id, Is.EqualTo(_table.Id));
        Assert.That(result[0].Players.Select(x => x.Name),
            Is.EquivalentTo(new[] { "me", "opponent" }));
    }
}
