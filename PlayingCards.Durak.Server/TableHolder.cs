using NLog;

namespace PlayingCards.Durak.Server;

public class TableHolder
{
    /// <summary>
    /// Время на окончание раунда при удачной защите (даём время на подкид).
    /// </summary>
    public const int STOP_ROUND_SECONDS = 10;

    /// <summary>
    /// Время на окончание раунда при «беру»: подкидывать особо некому, поэтому ждём меньше (issue #5).
    /// </summary>
    public const int STOP_ROUND_TAKE_SECONDS = 5;

    /// <summary>
    /// Время на принятие решения.
    /// </summary>
    public const int AFK_SECONDS = 60;

    private readonly Dictionary<Guid, Table> _tables = new();

    /// <summary>
    /// Защищает <see cref="_tables" /> и счётчики от гонок между фоновым таймером и запросами игроков.
    /// </summary>
    private readonly object _sync = new();

    private int _tablesVersion;

    /// <summary>
    /// Счётчик для имён болванчиков («Бот N»).
    /// </summary>
    private int _botNumber = 1;

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
    /// Время на окончание раунда в зависимости от причины остановки.
    /// </summary>
    public static int GetStopRoundSeconds(StopRoundStatus status)
    {
        return status == StopRoundStatus.Take ? STOP_ROUND_TAKE_SECONDS : STOP_ROUND_SECONDS;
    }

    public Table CreateTable()
    {
        lock (_sync)
        {
            var table = new Table { Id = Guid.NewGuid(), Number = TableNumber, Game = new(), Players = new() };
            table.Version = 0;
            _tables.Add(table.Id, table);
            TableNumber++;

            WriteLog(table, null, "create table");
            TablesVersion++;

            return table;
        }
    }

    public void Join(Guid tableId, string playerSecret, string playerName)
    {
        if (string.IsNullOrEmpty(playerSecret))
        {
            throw new BusinessException("Авторизуйтесь");
        }

        lock (_sync)
        {
            foreach (var table2 in _tables.Values)
            {
                if (table2.Players.Any(x => x.AuthSecret == playerSecret))
                {
                    throw new BusinessException("Вы уже сидите за столиком");
                }
            }

            if (_tables.TryGetValue(tableId, out var table))
            {
                var player = table.Game.AddPlayer(playerName);

                table.Players.Add(new()
                    { Player = player, AuthSecret = playerSecret });

                WriteLog(table, playerSecret, "join to table");

                if (table.Owner == null)
                {
                    table.Owner = player;
                }

                table.CleanLeaverPlayer();
                table.Version++;
                TablesVersion++;
            }
            else
            {
                throw new BusinessException("table not found");
            }
        }
    }

    /// <summary>
    /// Посадить за стол ИИ-болванчика. Разрешено только владельцу стола.
    /// Уважает лимит 6 мест и статус (нельзя в идущую игру).
    /// </summary>
    /// <param name="tableId">Идентификатор стола.</param>
    /// <param name="playerSecret">Секрет вызывающего — обязан быть владельцем.</param>
    /// <exception cref="BusinessException">Стол не найден / не владелец / идёт игра / нет мест.</exception>
    public void AddBot(Guid tableId, string playerSecret)
    {
        lock (_sync)
        {
            if (_tables.TryGetValue(tableId, out var table) == false)
            {
                throw new BusinessException("table not found");
            }

            var caller = table.Players.FirstOrDefault(x => x.AuthSecret == playerSecret)?.Player;
            if (caller == null || table.Owner != caller)
            {
                throw new BusinessException("only owner can add bot");
            }

            var botSecret = Guid.NewGuid().ToString();
            var botName = "Бот " + _botNumber;

            var player = table.Game.AddPlayer(botName);
            _botNumber++;

            table.Players.Add(new()
                { Player = player, AuthSecret = botSecret, IsBot = true });

            WriteLog(table, botSecret, "add bot: " + botName);

            if (table.Owner == null)
            {
                table.Owner = player;
            }

            table.CleanLeaverPlayer();
            table.Version++;
            TablesVersion++;
        }
    }

    public void Leave(string playerSecret)
    {
        lock (_sync)
        {
            var table = GetBySecret(playerSecret, out var tablePlayer);

            if (table == null || tablePlayer == null)
            {
                return;
            }

            table.CleanLeaverPlayer();
            Leave(table, tablePlayer);
        }
    }

    public void Leave(Table table, TablePlayer tablePlayer)
    {
        lock (_sync)
        {
            WriteLog(table, tablePlayer.AuthSecret, "leave from table");

            var playerIndex = table.Game.Players.IndexOf(tablePlayer.Player);

            if (table.Game.Status == GameStatus.InProcess)
            {
                table.LeavePlayer = tablePlayer.Player;
                table.LeavePlayerIndex = playerIndex;
                WriteLog(table, "", "leaver: " + tablePlayer.Player.Name);
            }

            table.Game.LeavePlayer(playerIndex);
            table.Players.Remove(tablePlayer);

            if (table.Game.Status != GameStatus.InProcess)
            {
                table.CleanStopRound();
                table.CleanAllAfkTime();
            }

            if (table.Players.All(x => x.IsBot))
            {
                _tables.Remove(table.Id);
            }
            else
            {
                if (table.Players.All(x => x.Player != table.Owner))
                {
                    table.Owner = table.Players.First(x => x.IsBot == false).Player;
                }
            }

            TablesVersion++;
            table.Version++;
        }
    }

    public Table Get(Guid tableId)
    {
        lock (_sync)
        {
            return _tables[tableId];
        }
    }

    public Table? GetBySecret(string playerSecret, out TablePlayer? player)
    {
        lock (_sync)
        {
            player = null;

            foreach (var table in _tables.Values)
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

    public Table[] GetTables()
    {
        lock (_sync)
        {
            return _tables.Values.ToArray();
        }
    }

    public void BackgroundProcess()
    {
        lock (_sync)
        {
            CheckStopRound();
            CheckAfkPlayers();
            CheckBots();
        }
    }

    /// <summary>
    /// Драйвер болванчиков: за тик исполняет НЕ БОЛЕЕ ОДНОГО хода бота на каждом столе в InProcess
    /// (естественная пауза ~1 с, чтобы ходы были видны). Под общим <see cref="_sync" />.
    /// </summary>
    private void CheckBots()
    {
        foreach (var table in _tables.Values.ToArray())
        {
            if (table.Game.Status != GameStatus.InProcess)
            {
                continue;
            }

            TablePlayer? defenderBot = null;
            BotMove defenderMove = default;
            TablePlayer? otherBot = null;
            BotMove otherMove = default;

            foreach (var tablePlayer in table.Players)
            {
                if (tablePlayer.IsBot == false)
                {
                    continue;
                }

                var candidate = BotBrain.DecideMove(table.Game, tablePlayer.Player);

                if (candidate.Kind == BotMoveKind.None)
                {
                    continue;
                }

                if (table.StopRoundBeginDate != null
                    && candidate.Kind is BotMoveKind.Defence or BotMoveKind.Take)
                {
                    continue;
                }

                if (tablePlayer.Player == table.Game.DefencePlayer)
                {
                    defenderBot = tablePlayer;
                    defenderMove = candidate;
                    break;
                }

                if (otherBot == null)
                {
                    otherBot = tablePlayer;
                    otherMove = candidate;
                }
            }

            var botToMove = defenderBot ?? otherBot;

            if (botToMove == null)
            {
                continue;
            }

            ExecuteBotMove(table, botToMove, defenderBot != null ? defenderMove : otherMove);
        }
    }

    /// <summary>
    /// Исполнить один ход бота через методы <see cref="Table" /> (они валидируют правила и делают Version++).
    /// Любая <see cref="BusinessException" /> гасится и логируется, чтобы единичная нелегальная попытка
    /// не валила фоновый тик.
    /// </summary>
    private void ExecuteBotMove(Table table, TablePlayer bot, BotMove move)
    {
        try
        {
            switch (move.Kind)
            {
                case BotMoveKind.StartAttack:
                case BotMoveKind.Attack:
                    table.PlayCards(bot.AuthSecret, move.CardIndexes);
                    break;

                case BotMoveKind.Defence:
                    table.PlayCards(bot.AuthSecret, move.CardIndexes, move.AttackCardIndex);
                    break;

                case BotMoveKind.Take:
                    table.Take(bot.AuthSecret);
                    break;
            }
        }
        catch (BusinessException ex)
        {
            var logger = LogManager.GetCurrentClassLogger()
                .WithProperty("TableId", table.Number + " " + table.Id);

            logger.Warn("bot move rejected (" + bot.Player.Name + ", " + move.Kind + "): " + ex.Message);
        }
    }

    private void CheckStopRound()
    {
        foreach (var table in _tables.Values.ToArray())
        {
            if (table.StopRoundBeginDate == null)
            {
                continue;
            }

            try
            {
                if (table.StopRoundStatus == null)
                {
                    throw new("stop round status is null");
                }

                var seconds = GetStopRoundSeconds(table.StopRoundStatus.Value);
                var finishTime = table.StopRoundBeginDate.Value.AddSeconds(seconds);

                if (DateTime.UtcNow >= finishTime)
                {
                    table.StopRoundStatus = null;
                    table.StopRoundBeginDate = null;
                    table.Game.StopRound();
                    table.SetActivePlayerAfkStartTime();
                    table.Version++;
                }
            }
            catch (Exception ex)
            {
                var logger = LogManager.GetCurrentClassLogger()
                    .WithProperty("TableId", table.Number + " " + table.Id);

                logger.Error("background stop round error: " + ex.Message, ex);
            }
        }
    }

    private void CheckAfkPlayers()
    {
        foreach (var table in _tables.Values.ToArray())
        {
            for (var i = 0; i < table.Players.Count; i++)
            {
                var tablePlayer = table.Players[i];

                if (tablePlayer.AfkStartTime != null)
                {
                    var finishTime = tablePlayer.AfkStartTime.Value.AddSeconds(AFK_SECONDS);

                    if (DateTime.UtcNow >= finishTime)
                    {
                        Leave(table, tablePlayer);
                        i--;
                    }
                }
            }
        }
    }

    private void WriteLog(Table table, string? playerSecret, string message)
    {
        var tablePlayer = table.Players.SingleOrDefault(x => x.AuthSecret == playerSecret);
        var playerIndex = tablePlayer == null ? null : (int?)table.Game.Players.IndexOf(tablePlayer.Player);

        var logger = LogManager.GetCurrentClassLogger()
            .WithProperty("TableId", table.Number + " " + table.Id)
            .WithProperty("PlayerId", playerSecret)
            .WithProperty("PlayerIndex", playerIndex);

        logger.Info(message);
    }
}
