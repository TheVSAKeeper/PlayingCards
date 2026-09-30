using NLog;

namespace PlayingCards.Durak.Server;

public class TableHolder : TableHolderBase<Table, TablePlayer>
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
    /// Сколько секунд показываем реплику игрока («Бито!») над его бейджем (issue F5).
    /// </summary>
    public const int REPLY_SECONDS = 3;

    /// <summary>
    /// Время на принятие решения.
    /// </summary>
    public const int AFK_SECONDS = 60;

    /// <summary>
    /// Предельная длина имени игрока: длиннее не помещается в карточку стола и бейдж.
    /// </summary>
    public const int MAX_PLAYER_NAME_LENGTH = 24;

    /// <summary>
    /// Счётчик для имён болванчиков («Бот N»).
    /// </summary>
    private int _botNumber = 1;

    /// <summary>
    /// Время на окончание раунда в зависимости от причины остановки.
    /// </summary>
    public static int GetStopRoundSeconds(StopRoundStatus status)
    {
        return status switch
        {
            StopRoundStatus.Take => STOP_ROUND_TAKE_SECONDS,
            _ => STOP_ROUND_SECONDS,
        };
    }

    public Table CreateTable()
    {
        lock (Sync)
        {
            var table = new Table { Id = Guid.NewGuid(), Number = TableNumber, Game = new(), Players = new() };
            table.Version = 0;
            Tables.Add(table.Id, table);
            TableNumber++;

            WriteLog(table, null, "create table");
            TablesVersion++;

            return table;
        }
    }

    /// <summary>
    /// Создать стол и сразу посадить за него создателя. Если сесть не удалось, стол не остаётся пустым в лобби.
    /// </summary>
    /// <param name="playerSecret">Секрет создателя.</param>
    /// <param name="playerName">Имя создателя.</param>
    /// <exception cref="BusinessException">Не авторизован / уже сидит за столом / недопустимое имя.</exception>
    public Table CreateTable(string playerSecret, string playerName)
    {
        lock (Sync)
        {
            var table = CreateTable();

            try
            {
                Join(table.Id, playerSecret, playerName);
            }
            catch
            {
                Tables.Remove(table.Id);
                TableNumber--;
                TablesVersion++;
                throw;
            }

            return table;
        }
    }

    public void Join(Guid tableId, string playerSecret, string playerName)
    {
        if (string.IsNullOrEmpty(playerSecret))
        {
            throw new BusinessException("Авторизуйтесь");
        }

        if (string.IsNullOrWhiteSpace(playerName))
        {
            throw new BusinessException("Введите имя");
        }

        if (playerName.Length > MAX_PLAYER_NAME_LENGTH)
        {
            throw new BusinessException($"Имя длиннее {MAX_PLAYER_NAME_LENGTH} символов. Выйдите и войдите под именем покороче");
        }

        lock (Sync)
        {
            foreach (var table2 in Tables.Values)
            {
                if (table2.Players.Any(x => x.AuthSecret == playerSecret))
                {
                    throw new BusinessException("Вы уже сидите за столиком");
                }
            }

            if (Tables.TryGetValue(tableId, out var table))
            {
                lock (table.SyncRoot)
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
                }

                TablesVersion++;
            }
            else
            {
                throw new BusinessException("Стол не найден");
            }
        }
    }

    /// <summary>
    /// Посадить за стол ИИ-болванчика. Разрешено только владельцу стола.
    /// Уважает лимит 6 мест и статус (нельзя в идущую игру).
    /// </summary>
    /// <param name="tableId">Идентификатор стола.</param>
    /// <param name="playerSecret">Секрет вызывающего – обязан быть владельцем.</param>
    /// <exception cref="BusinessException">Стол не найден / не владелец / идёт игра / нет мест.</exception>
    public void AddBot(Guid tableId, string playerSecret)
    {
        lock (Sync)
        {
            if (Tables.TryGetValue(tableId, out var table) == false)
            {
                throw new BusinessException("Стол не найден");
            }

            var caller = table.Players.FirstOrDefault(x => x.AuthSecret == playerSecret)?.Player;
            if (caller == null || table.Owner != caller)
            {
                throw new BusinessException("Добавлять ботов может только владелец стола");
            }

            var botSecret = Guid.NewGuid().ToString();

            lock (table.SyncRoot)
            {
                var takenNames = table.Players.Select(x => x.Player.Name).ToHashSet(StringComparer.Ordinal);
                string botName;

                if (BotNames.PickName(takenNames) is { } themedName)
                {
                    botName = themedName;
                }
                else
                {
                    botName = "Бот " + _botNumber;
                    _botNumber++;
                }

                var player = table.Game.AddPlayer(botName);

                table.Players.Add(new()
                    { Player = player, AuthSecret = botSecret, IsBot = true });

                WriteLog(table, botSecret, "add bot: " + botName);

                if (table.Owner == null)
                {
                    table.Owner = player;
                }

                table.CleanLeaverPlayer();
                table.Version++;
            }

            TablesVersion++;
        }
    }

    /// <summary>
    /// Выгнать игрока (бота или человека) из-за стола. Разрешено только владельцу; работает и в лобби,
    /// и во время партии (issue F2). Реиспользует штатный <see cref="Leave(Table, TablePlayer)" />,
    /// поэтому корректно завершает/продолжает партию, ставит «крысу» и переназначает владельца.
    /// </summary>
    /// <param name="callerSecret">Секрет вызывающего –обязан быть владельцем стола.</param>
    /// <param name="tableId">Идентификатор стола.</param>
    /// <param name="targetGameIndex">Игровой индекс цели (как клиент видит соперника в кольце).</param>
    /// <exception cref="BusinessException">Стол не найден / не владелец / неверная цель / попытка выгнать себя.</exception>
    public void Kick(string callerSecret, Guid tableId, int targetGameIndex)
    {
        lock (Sync)
        {
            if (Tables.TryGetValue(tableId, out var table) == false)
            {
                throw new BusinessException("Стол не найден");
            }

            var caller = table.Players.FirstOrDefault(x => x.AuthSecret == callerSecret)?.Player;

            if (caller == null || table.Owner != caller)
            {
                throw new BusinessException("Выгонять может только владелец стола");
            }

            if (targetGameIndex < 0 || targetGameIndex >= table.Game.Players.Count)
            {
                throw new BusinessException("Игрок не найден");
            }

            var targetPlayer = table.Game.Players[targetGameIndex];

            if (targetPlayer == caller)
            {
                throw new BusinessException("Нельзя выгнать самого себя");
            }

            var target = table.Players.FirstOrDefault(x => x.Player == targetPlayer);

            if (target == null)
            {
                throw new BusinessException("Игрок не найден");
            }

            WriteLog(table, callerSecret, "kick: " + targetPlayer.Name);
            table.CleanLeaverPlayer();
            Leave(table, target);
        }
    }

    public void Leave(string playerSecret)
    {
        lock (Sync)
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
        lock (Sync)
        {
            lock (table.SyncRoot)
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
                    Tables.Remove(table.Id);
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

            // Ушедший мог быть единственным несказавшим атакующим –пересчитать вне table.SyncRoot,
            // т.к. TryCloseAfterRosterChange сам его берёт (и это уже реентрантно относительно Sync).
            table.TryCloseAfterRosterChange();
        }
    }

    /// <summary>
    /// Гасит «протухшие» реплики («Бито!») старше <see cref="REPLY_SECONDS" />: обнуляет их и делает
    /// <see cref="Table.Version" />++, чтобы push-фронт (Blazor) гарантированно перерисовал бейдж и убрал
    /// баббл. Без этого реплика «залипала» бы в DOM до следующего события стола, ведь
    /// <see cref="TableViewBuilder" /> отсекает её лишь при перестроении вида (issue F5). Под общим
    /// <see cref="Sync" />.
    /// </summary>
    private void ClearStaleReplies()
    {
        var now = DateTime.UtcNow;
        var ttl = TimeSpan.FromSeconds(REPLY_SECONDS);

        foreach (var table in Tables.Values.ToArray())
        {
            var changed = false;

            foreach (var tablePlayer in table.Players)
            {
                if (tablePlayer.Reply == null || tablePlayer.ReplyDate is not { } at || now - at < ttl)
                {
                    continue;
                }

                tablePlayer.Reply = null;
                tablePlayer.ReplyDate = null;
                changed = true;
            }

            if (changed)
            {
                table.Version++;
            }
        }
    }

    /// <summary>
    /// Боты-атакующие, которым больше нечего подкинуть, говорят «Бито» в окне удачной защиты или «беру» –
    /// тогда раунд закрывается, как только отказались все атакующие, а не висит до общего таймера (issue F5, #10).
    /// Бот, у которого ещё есть подходящий подкид, молчит: его ход исполнит <see cref="CheckBots" /> на этом же
    /// тике. НЕ БОЛЕЕ ОДНОГО голоса за тик на стол (как <see cref="CheckBots" />) –иначе все боты без карт
    /// озвучивают «Бито» в один и тот же тик открытия окна, что мгновенно выдаёт человеку отсутствие подкида.
    /// Под общим <see cref="Sync" />. Голос «Бито» сбрасывается на каждом новом окне остановки раунда.
    /// </summary>
    private void CheckBotBeats()
    {
        foreach (var table in Tables.Values.ToArray())
        {
            if (table.Game.Status != GameStatus.InProcess
                || table.StopRoundBeginDate == null
                || table.StopRoundStatus is not (StopRoundStatus.SuccessDefence or StopRoundStatus.Take))
            {
                continue;
            }

            foreach (var tablePlayer in table.Players.ToArray())
            {
                if (table.StopRoundBeginDate == null)
                {
                    break;
                }

                if (tablePlayer.IsBot == false
                    || tablePlayer.SaidBeat
                    || tablePlayer.Player == table.Game.DefencePlayer
                    || tablePlayer.Player.Hand.Cards.Count == 0)
                {
                    continue;
                }

                if (BotBrain.DecideMove(table.Game, tablePlayer.Player).Kind != BotMoveKind.None)
                {
                    continue;
                }

                try
                {
                    table.Beat(tablePlayer.AuthSecret);
                }
                catch (BusinessException ex)
                {
                    var logger = LogManager.GetCurrentClassLogger()
                        .WithProperty("TableId", table.Number + " " + table.Id);

                    logger.Warn("bot beat rejected (" + tablePlayer.Player.Name + "): " + ex.Message);
                }

                break;
            }
        }
    }

    /// <summary>
    /// Драйвер болванчиков: за тик исполняет НЕ БОЛЕЕ ОДНОГО хода бота на каждом столе в InProcess
    /// (естественная пауза ~1 с, чтобы ходы были видны). Под общим <see cref="Sync" />.
    /// </summary>
    private void CheckBots()
    {
        foreach (var table in Tables.Values.ToArray())
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

                if (table.StopRoundBeginDate != null && tablePlayer.SaidBeat)
                {
                    // Уже сказал «Бито» в этом окне остановки раунда –CheckBotBeats это уже решил на этом
                    // же тике, повторный DecideMove для него бессмыслен (issue: двойной DecideMove за тик).
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
        foreach (var table in Tables.Values.ToArray())
        {
            if (table.StopRoundBeginDate == null)
            {
                continue;
            }

            try
            {
                lock (table.SyncRoot)
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
            }
            catch (Exception ex)
            {
                var logger = LogManager.GetCurrentClassLogger()
                    .WithProperty("TableId", table.Number + " " + table.Id);

                logger.Error("background stop round error: " + ex.Message, ex);
            }
        }
    }

    protected override int AfkSeconds => AFK_SECONDS;

    protected override void ProcessTables()
    {
        CheckStopRound();
        KickAfkPlayers();
        CheckBotBeats();
        CheckBots();
        ClearStaleReplies();
    }

    protected override void KickAfk(Table table, TablePlayer player)
    {
        Leave(table, player);
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
