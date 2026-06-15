using PlayingCards.Durak.Server;
using static PlayingCards.Durak.Server.GetStatusModel;

namespace PlayingCards.Durak.Server.Tests;

/// <summary>
/// Регресс на «висит» из боя: выход игрока во время партии ронял Blazor-circuit
/// необработанным <see cref="IndexOutOfRangeException" /> при рендере кольца соперников.
/// Корень — вид глазами игрока, которого уже нет в <c>game.Players</c> (MyPlayerIndex == -1).
/// </summary>
[TestFixture]
public class LeaveRenderCrashTests
{
    private static readonly string[] Hands =
    [
        "8♠ 9♠ 10♠ J♠ Q♠ K♠",
        "7♦ 8♥ 9♥ 10♥ J♥ Q♥",
        "A♦ 8♣ 9♣ 10♣ J♣ Q♣",
    ];

    /// <summary>
    /// Окно гонки выхода: <see cref="TableHolder.Leave(Table, TablePlayer)" /> чистит
    /// <c>game.Players</c> и <c>table.Players</c> не атомарно для незалоченного рендера.
    /// Если рендер успел между ними — игрок ещё «за столом», но уже не в партии. По этому
    /// признаку (MyPlayerIndex &lt; 0) <c>GameTable.Rebuild</c> обязан НЕ строить доску.
    /// </summary>
    [Test]
    public void BuildTable_PlayerRemovedFromGameButStillSeated_HasNegativeMyIndex()
    {
        var table = ServerTestHelper.BuildStartedTable(Hands, "6♦", out var players);
        var me = players[0];

        table.Game.LeavePlayer(table.Game.Players.IndexOf(me.Player));

        Assert.That(table.Players, Does.Contain(me), "имитируем гонку: ещё в table.Players");
        Assert.That(TableViewBuilder.BuildTable(table, me).MyPlayerIndex, Is.LessThan(0));
    }

    /// <summary>
    /// AFK-засечки больше не падают на <c>First</c>, если ходящего/защищающегося уже нет за
    /// столом (раньше — «Sequence contains no matching element» → краш circuit).
    /// </summary>
    [Test]
    public void SetAfkStartTime_ActiveOrDefenceNotSeated_DoesNotThrow()
    {
        var table = ServerTestHelper.BuildStartedTable(Hands, "6♦", out _);

        table.Players.RemoveAll(x => x.Player == table.Game.ActivePlayer || x.Player == table.Game.DefencePlayer);

        Assert.Multiple(() =>
        {
            Assert.DoesNotThrow(() => table.SetActivePlayerAfkStartTime());
            Assert.DoesNotThrow(() => table.SetDefencePlayerAfkStartTime());
            Assert.DoesNotThrow(() => table.CleanDefencePlayerAfkStartTime());
        });
    }

    /// <summary>
    /// Зеркало алгоритма <c>PlayersRing.RingPlayers</c> из Blazor (отдельного тест-проекта на
    /// фронт нет). Держим его на реальных видах из <see cref="TableViewBuilder.BuildTable" />
    /// после выхода в партии — включая вид самого вышедшего (MyPlayerIndex == -1), на котором
    /// прежний код уходил в <c>others[-1]</c>. ВАЖНО: держать в синхроне с PlayersRing.razor.
    /// </summary>
    [Test]
    public void RingPlayers_ForEveryViewAfterLeave_NeverThrows()
    {
        var problems = new List<string>();

        for (var leaver = 0; leaver < 3; leaver++)
        {
            var table = ServerTestHelper.BuildStartedTable(Hands, "6♦", out var players);
            var seatedBefore = players.ToArray();
            new TableHolder().Leave(table, players[leaver]);

            foreach (var p in seatedBefore)
            {
                var view = TableViewBuilder.BuildTable(table, p);

                try
                {
                    _ = BuildRing(view);
                }
                catch (Exception e)
                {
                    problems.Add($"leaver={leaver} viewer={p.AuthSecret} my={view.MyPlayerIndex} others={view.Players.Length}: {e.GetType().Name}");
                }
            }
        }

        Assert.That(problems, Is.Empty, string.Join("\n", problems));
    }

    private record RingPlayer(PlayerModel Player, int GameIndex, bool IsLeaver);

    private static List<RingPlayer> BuildRing(TableModel view)
    {
        var others = view.Players;
        var my = Math.Clamp(view.MyPlayerIndex, 0, others.Length);
        var result = new List<RingPlayer>();

        for (var i = my - 1; i >= 0; i--)
        {
            result.Add(new RingPlayer(others[i], i, false));
        }

        for (var i = others.Length - 1; i >= my; i--)
        {
            result.Add(new RingPlayer(others[i], i + 1, false));
        }

        if (view.LeavePlayer is not { } leaver)
        {
            return result;
        }

        InsertLeaver(result, new RingPlayer(leaver, -1, true), leaver.Index, my, others.Length);
        return result;
    }

    private static void InsertLeaver(List<RingPlayer> ring, RingPlayer leaver, int leaverIndex, int my, int othersLength)
    {
        if (leaverIndex == my) { ring.Insert(0, leaver); return; }
        if (leaverIndex > othersLength) { leaverIndex = 0; }
        if (othersLength == 0) { ring.Add(leaver); return; }
        var afterPos = ring.FindIndex(x => !x.IsLeaver && x.GameIndex == leaverIndex);
        ring.Insert(afterPos < 0 ? ring.Count : afterPos + 1, leaver);
    }
}
