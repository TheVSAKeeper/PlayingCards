using System.Collections.Concurrent;
using PlayingCards.Durak.Server;

namespace PlayingCards.Durak.Server.Tests;

[TestFixture]
public class TableHolderTests
{
    private TableHolder _holder = null!;

    [SetUp]
    public void SetUp()
    {
        _holder = new TableHolder();
    }

    [Test]
    public void CreateTable_RegistersTableWithIncrementingNumber()
    {
        var first = _holder.CreateTable();
        var second = _holder.CreateTable();

        Assert.Multiple(() =>
        {
            Assert.That(first.Number, Is.EqualTo(1));
            Assert.That(second.Number, Is.EqualTo(2));
            Assert.That(_holder.GetTables(), Has.Length.EqualTo(2));
            Assert.That(_holder.Get(first.Id), Is.SameAs(first));
        });
    }

    [Test]
    public void Join_AddsPlayerAndSetsFirstAsOwner()
    {
        var table = _holder.CreateTable();

        _holder.Join(table.Id, "s1", "Alice");
        _holder.Join(table.Id, "s2", "Bob");

        Assert.Multiple(() =>
        {
            Assert.That(table.Players, Has.Count.EqualTo(2));
            Assert.That(table.Owner.Name, Is.EqualTo("Alice"));
            Assert.That(_holder.GetBySecret("s2", out var me), Is.SameAs(table));
            Assert.That(me!.Player.Name, Is.EqualTo("Bob"));
        });
    }

    [TestCase("", "пусто", "Авторизуйтесь")]
    public void Join_EmptySecret_Throws(string secret, string name, string expected)
    {
        var table = _holder.CreateTable();

        var ex = Assert.Throws<BusinessException>(() => _holder.Join(table.Id, secret, name));
        Assert.That(ex!.Message, Is.EqualTo(expected));
    }

    [Test]
    public void Join_UnknownTable_Throws()
    {
        var ex = Assert.Throws<BusinessException>(() => _holder.Join(Guid.NewGuid(), "s1", "Alice"));
        Assert.That(ex!.Message, Is.EqualTo("table not found"));
    }

    [Test]
    public void Join_SameSecretTwice_Throws()
    {
        var table1 = _holder.CreateTable();
        var table2 = _holder.CreateTable();
        _holder.Join(table1.Id, "s1", "Alice");

        var ex = Assert.Throws<BusinessException>(() => _holder.Join(table2.Id, "s1", "Alice"));
        Assert.That(ex!.Message, Is.EqualTo("Вы уже сидите за столиком"));
    }

    [Test]
    public void Leave_LastPlayer_RemovesTable()
    {
        var table = _holder.CreateTable();
        _holder.Join(table.Id, "s1", "Alice");

        _holder.Leave("s1");

        Assert.Multiple(() =>
        {
            Assert.That(_holder.GetTables(), Is.Empty);
            Assert.That(_holder.GetBySecret("s1", out _), Is.Null);
        });
    }

    [Test]
    public void Leave_OwnerLeaves_ReassignsOwnerToRemaining()
    {
        var table = _holder.CreateTable();
        _holder.Join(table.Id, "s1", "Alice");
        _holder.Join(table.Id, "s2", "Bob");

        _holder.Leave("s1");

        Assert.Multiple(() =>
        {
            Assert.That(table.Players, Has.Count.EqualTo(1));
            Assert.That(table.Owner.Name, Is.EqualTo("Bob"));
        });
    }

    [Test]
    public void Leave_LastHumanLeaves_OnlyBotsRemain_RemovesTable()
    {
        var table = _holder.CreateTable();
        _holder.Join(table.Id, "s1", "Alice");
        _holder.AddBot(table.Id);
        _holder.AddBot(table.Id);

        _holder.Leave("s1");

        Assert.Multiple(() =>
        {
            Assert.That(_holder.GetTables(), Is.Empty);
            Assert.That(_holder.GetBySecret("s1", out _), Is.Null);
        });
    }

    [Test]
    public void Leave_OneHumanRemainsAmongBots_KeepsTableAndPrefersHumanOwner()
    {
        var table = _holder.CreateTable();
        _holder.Join(table.Id, "s1", "Alice");
        _holder.AddBot(table.Id);
        _holder.Join(table.Id, "s2", "Bob");

        _holder.Leave("s1");

        Assert.Multiple(() =>
        {
            Assert.That(_holder.GetTables(), Has.Length.EqualTo(1));
            Assert.That(table.Players, Has.Count.EqualTo(2));
            Assert.That(table.Owner.Name, Is.EqualTo("Bob"));
        });
    }

    [Test]
    public void Leave_UnknownSecret_DoesNothing()
    {
        Assert.DoesNotThrow(() => _holder.Leave("ghost"));
    }

    [Test]
    public void BackgroundProcess_KicksPlayer_AfterAfkTimeout()
    {
        var table = _holder.CreateTable();
        _holder.Join(table.Id, "s1", "Alice");
        _holder.Join(table.Id, "s2", "Bob");
        table.StartGame();

        var active = table.Players.First(x => x.Player == table.Game.ActivePlayer);
        active.AfkStartTime = DateTime.UtcNow.AddSeconds(-(TableHolder.AFK_SECONDS + 1));

        _holder.BackgroundProcess();

        Assert.That(table.Players, Has.Count.EqualTo(1));
        Assert.That(table.Players, Has.None.Matches<TablePlayer>(p => p.AuthSecret == active.AuthSecret));
    }

    [Test]
    public void BackgroundProcess_DoesNotKick_BeforeAfkTimeout()
    {
        var table = _holder.CreateTable();
        _holder.Join(table.Id, "s1", "Alice");
        _holder.Join(table.Id, "s2", "Bob");
        table.StartGame();

        var active = table.Players.First(x => x.Player == table.Game.ActivePlayer);
        active.AfkStartTime = DateTime.UtcNow.AddSeconds(-(TableHolder.AFK_SECONDS - 5));

        _holder.BackgroundProcess();

        Assert.That(table.Players, Has.Count.EqualTo(2));
    }

    /// <summary>
    /// Регрессия на гонку из задачи 3: фоновый тик итерирует столы, пока игроки их мутируют.
    /// Без блокировки это падало с «Collection was modified».
    /// </summary>
    [Test]
    public void BackgroundProcess_ConcurrentWithMutations_DoesNotThrow()
    {
        var errors = new ConcurrentQueue<Exception>();
        var cts = new CancellationTokenSource();

        var background = Task.Run(() =>
        {
            try
            {
                while (!cts.IsCancellationRequested)
                {
                    _holder.BackgroundProcess();
                    _holder.GetTables();
                }
            }
            catch (Exception ex)
            {
                errors.Enqueue(ex);
            }
        });

        var mutators = Enumerable.Range(0, 4)
            .Select(worker => Task.Run(() =>
            {
                try
                {
                    for (var i = 0; i < 200; i++)
                    {
                        var table = _holder.CreateTable();
                        var secret = $"s{worker}-{i}";
                        _holder.Join(table.Id, secret, "p");
                        _holder.GetBySecret(secret, out _);
                        _holder.Leave(secret);
                    }
                }
                catch (Exception ex)
                {
                    errors.Enqueue(ex);
                }
            }))
            .ToArray();

        Task.WaitAll(mutators);
        cts.Cancel();
        background.Wait();

        Assert.That(errors, Is.Empty, () => string.Join(Environment.NewLine, errors.Select(e => e.Message)));
    }
}
