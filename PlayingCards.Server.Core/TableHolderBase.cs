namespace PlayingCards.Server.Core;

public abstract class TableHolderBase<TTable, TPlayer> : IBackgroundProcessor
    where TTable : TableBase<TPlayer>
    where TPlayer : TablePlayerBase
{
    protected readonly Dictionary<Guid, TTable> Tables = new();

    /// <summary>
    /// Защищает <see cref="Tables" /> и счётчики от гонок между фоновым таймером и запросами игроков.
    /// </summary>
    protected readonly object Sync = new();

    private int _tablesVersion;

    public int TablesVersion
    {
        get => _tablesVersion;
        set
        {
            _tablesVersion = value;
            Changed?.Invoke();
        }
    }

    /// <summary>
    /// Событие изменения списка столов / лобби (для push в UI).
    /// </summary>
    public event Action? Changed;
    public int TableNumber = 1;

    /// <summary>
    /// Время на принятие решения до AFK-кика, в секундах.
    /// </summary>
    protected abstract int AfkSeconds { get; }

    public TTable Get(Guid tableId)
    {
        lock (Sync)
        {
            return Tables[tableId];
        }
    }

    public TTable? GetBySecret(string playerSecret, out TPlayer? player)
    {
        lock (Sync)
        {
            player = null;

            foreach (var table in Tables.Values)
            {
                player = table.Players.FirstOrDefault(x => x.AuthSecret == playerSecret);

                if (player != null)
                {
                    return table;
                }
            }

            return null;
        }
    }

    public TTable[] GetTables()
    {
        lock (Sync)
        {
            return Tables.Values.ToArray();
        }
    }

    public void BackgroundProcess()
    {
        lock (Sync)
        {
            ProcessTables();
        }
    }

    /// <summary>
    /// Игро-специфичная часть фонового тика. Вызывается под <see cref="Sync" />.
    /// </summary>
    protected abstract void ProcessTables();

    protected void KickAfkPlayers()
    {
        foreach (var table in Tables.Values.ToArray())
        {
            for (var i = 0; i < table.Players.Count; i++)
            {
                var tablePlayer = table.Players[i];

                if (tablePlayer.AfkStartTime != null)
                {
                    var finishTime = tablePlayer.AfkStartTime.Value.AddSeconds(AfkSeconds);

                    if (DateTime.UtcNow >= finishTime)
                    {
                        KickAfk(table, tablePlayer);
                        i--;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Убрать AFK-игрока со стола по правилам конкретной игры.
    /// </summary>
    protected abstract void KickAfk(TTable table, TPlayer player);
}
