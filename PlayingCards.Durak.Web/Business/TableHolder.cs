using Microsoft.AspNetCore.SignalR;
using NLog;
using PlayingCards.Durak.Web.Hubs;
using PlayingCards.Durak.Web.Models;
using static PlayingCards.Durak.Web.Models.GetStatusModel;

namespace PlayingCards.Durak.Web.Business;

public class TableHolder(IHubContext<GameHub>? hubContext = null)
{
    /// <summary>
    /// Время на окончание раунда.
    /// </summary>
    public const int STOP_ROUND_SECONDS = 10;

    /// <summary>
    /// Время на принятие решения.
    /// </summary>
    public const int AFK_SECONDS = 60;

    private const int STOP_ROUND_MIN_SECONDS = 3;

    private static readonly Dictionary<Guid, Table> _tables = new();
    public int TablesVersion;
    public int TableNumber = 1;

    public static DateTime GetSecond(DateTime tableStopRoundBeginDate, StopRoundStatus? tableStopRoundStatus)
    {
        return tableStopRoundBeginDate.AddSeconds(tableStopRoundStatus == StopRoundStatus.TakeFull ? STOP_ROUND_MIN_SECONDS : STOP_ROUND_SECONDS);
    }

    public async Task<Table> CreateTable()
    {
        var table = new Table { Id = Guid.NewGuid(), Number = TableNumber, Game = new(), Players = new() };
        table.Version = 0;
        table.SetHubContext(hubContext);
        _tables.Add(table.Id, table);
        TablesVersion++;

        WriteLog(table, null, "create table");
        TableNumber++;

        await BroadcastTablesUpdated();

        return table;
    }

    public async Task Join(Guid tableId, string playerSecret, string playerName)
    {
        if (string.IsNullOrEmpty(playerSecret))
        {
            throw new BusinessException("Авторизуйтесь");
        }

        foreach (var table2 in _tables.Values)
        {
            if (table2.Players.Any(x => x.AuthSecret == playerSecret))
            {
                throw new BusinessException("Вы уже сидите за столиком");
            }
        }

        if (_tables.TryGetValue(tableId, out var table))
        {
            var debug = false;

            if (playerName != "maksim")
            {
                debug = false;
            }

            if (debug)
            {
            }

            var player = table.Game.AddPlayer(playerName);

            table.Players.Add(new()
                { Player = player, AuthSecret = playerSecret });

            WriteLog(table, playerSecret, "join to table");

            if (table.Owner == null)
            {
                table.Owner = player;
            }

            if (debug)
            {
                var player1 = table.Game.AddPlayer("1 кореш " + playerName);

                table.Players.Add(new() { Player = player1, AuthSecret = "123" });

                var player2 = table.Game.AddPlayer("2 кореш " + playerName);

                table.Players.Add(new() { Player = player2, AuthSecret = "123" });

                var player4 = table.Game.AddPlayer("4 У меня длинное имя для проверки вёрстки");

                table.Players.Add(new() { Player = player4, AuthSecret = "123" });

                var player5 = table.Game.AddPlayer("5 Лучик света продуктовой разработки");

                table.Players.Add(new() { Player = player5, AuthSecret = "123" });
            }

            var debug2 = false;

            if (debug2)
            {
                table.Game.AddPlayer("я всегда проигрываю");
                table.Game.StartGame();
                table.Game.Deck.Cards = new();

                if (table.Game.Players.IndexOf(table.Game.ActivePlayer) == 0)
                {
                    table.Game.Players[0].Hand.RemoveRange(1, 5);
                }

                if (table.Game.Players.IndexOf(table.Game.ActivePlayer) == 1)
                {
                    table.Game.Players[1].Hand.RemoveRange(1, 5);
                    table.Game.Players[1].Hand.StartAttack(new[] { 0 });
                }
            }

            var debug3 = false;

            if (debug3)
            {
                var player1 = table.Game.AddPlayer("1 кореш " + playerName);
                table.Players.Add(new() { Player = player1, AuthSecret = "123" });

                var player2 = table.Game.AddPlayer("2 кореш " + playerName);
                table.Players.Add(new() { Player = player2, AuthSecret = "123" });

                table.Game.StartGame();
                table.Game.Deck.Cards = new();

                if (table.Game.Players.IndexOf(table.Game.ActivePlayer) == 0)
                {
                    table.Game.Players[0].Hand.RemoveRange(1, 5);
                }

                if (table.Game.Players.IndexOf(table.Game.ActivePlayer) == 1)
                {
                    table.Game.Players[1].Hand.RemoveRange(1, 5);
                    //table.Game.Players[1].Hand.StartAttack(new[] { 0 });
                    //table.Game.LeavePlayer(1);
                }
            }

            table.CleanLeaverPlayer();
            table.SetHubContext(hubContext);
            table.Version++;
            TablesVersion++;

            await BroadcastPlayerJoined(table, playerName);
            await BroadcastTablesUpdated();
        }
        else
        {
            throw new BusinessException("table not found");
        }
    }

    public async Task Leave(string playerSecret)
    {
        var table = GetBySecret(playerSecret, out var tablePlayer);
        await Leave(table, tablePlayer);
    }

    public async Task Leave(Table table, TablePlayer tablePlayer)
    {
        WriteLog(table, tablePlayer.AuthSecret, "leave from table");

        var playerName = tablePlayer.Player.Name;
        var playerIndex = table.Game.Players.IndexOf(tablePlayer.Player);
        var correctionValue = 0;

        if (table.Game.LeavePlayer(playerIndex))
        {
            if (table.Game.Status == GameStatus.InProcess)
            {
                table.LeavePlayer = tablePlayer.Player;
                table.LeavePlayerIndex = playerIndex;
                WriteLog(table, "", "leaver: " + tablePlayer.Player.Name);
            }

            table.Players.Remove(tablePlayer);
            correctionValue = 1;
        }
        else
        {
            // TODO: Костыль. Игрок не сможет создать новый стол
            tablePlayer.AuthSecret = string.Empty;
            WriteLog(table, "", "winner leaver: " + tablePlayer.Player.Name);
        }

        if (table.Players.Count - correctionValue == 0)
        {
            _tables.Remove(table.Id);
        }
        else
        {
            if (table.Players.All(x => x.Player != table.Owner))
            {
                table.Owner = table.Players.First().Player;
            }
        }

        TablesVersion++;
        table.Version++;

        await BroadcastPlayerLeft(table, playerName);
        await BroadcastTablesUpdated();
    }

    public Table Get(Guid tableId)
    {
        var table = _tables[tableId];
        return table;
    }

    public Table? GetBySecret(string playerSecret, out TablePlayer? player)
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

    public Table[] GetTables()
    {
        return _tables.Values.ToArray();
    }

    public async Task BackgroundProcess()
    {
        await CheckStopRound();
        await CheckAfkPlayers();
    }

    private async Task BroadcastTablesUpdated()
    {
        if (hubContext == null)
        {
            return;
        }

        try
        {
            var tablesState = GetTablesState();
            await hubContext.Clients.All.SendAsync("TablesUpdated", tablesState);
        }
        catch (Exception ex)
        {
            var logger = LogManager.GetCurrentClassLogger();
            logger.Error(ex, "Failed to broadcast tables updated");
        }
    }

    private async Task BroadcastPlayerJoined(Table table, string playerName)
    {
        if (hubContext == null)
        {
            return;
        }

        try
        {
            var groupName = $"Table_{table.Id}";

            var notification = new PlayerJoinedNotification
            {
                PlayerName = playerName,
                TableId = table.Id,
            };

            await hubContext.Clients.Group(groupName).SendAsync("PlayerJoined", notification);
        }
        catch (Exception ex)
        {
            var logger = LogManager.GetCurrentClassLogger();
            logger.Error(ex, "Failed to broadcast player joined for table {TableId}", table.Id);
        }
    }

    private async Task BroadcastPlayerLeft(Table table, string playerName)
    {
        if (hubContext == null)
        {
            return;
        }

        try
        {
            var groupName = $"Table_{table.Id}";

            var notification = new PlayerLeftNotification
            {
                PlayerName = playerName,
                TableId = table.Id,
            };

            await hubContext.Clients.Group(groupName).SendAsync("PlayerLeft", notification);
        }
        catch (Exception ex)
        {
            var logger = LogManager.GetCurrentClassLogger();
            logger.Error(ex, "Failed to broadcast player left for table {TableId}", table.Id);
        }
    }

    private GetStatusModel GetTablesState()
    {
        return new()
        {
            Version = TablesVersion,
            Tables = GetTables()
                .Select(x => new TableModel
                {
                    Id = x.Id,
                    Players = x.Players
                        .Select(p => new PlayerModel { Name = p.Player.Name })
                        .ToArray(),
                })
                .ToArray(),
        };
    }

    private async Task CheckStopRound()
    {
        foreach (var table in _tables.Values)
        {
            if (table.StopRoundBeginDate != null)
            {
                try
                {
                    var finishTime = GetSecond(table.StopRoundBeginDate.Value, table.StopRoundStatus);

                    if (DateTime.UtcNow >= finishTime)
                    {
                        if (table.StopRoundStatus == null)
                        {
                            throw new("stop round status is null");
                        }

                        table.StopRoundStatus = null;
                        table.StopRoundBeginDate = null;
                        table.Game.StopRound();
                        table.SetActivePlayerAfkStartTime();
                        table.Version++;

                        await table.BroadcastGameStateAsync();
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
    }

    private async Task CheckAfkPlayers()
    {
        foreach (var table in _tables.Values)
        {
            for (var i = 0; i < table.Players.Count; i++)
            {
                var tablePlayer = table.Players[i];

                if (tablePlayer.AfkStartTime != null)
                {
                    var finishTime = tablePlayer.AfkStartTime.Value.AddSeconds(AFK_SECONDS);

                    if (DateTime.UtcNow >= finishTime)
                    {
                        await Leave(table, tablePlayer);
                        i--;
                    }
                }
            }
        }
    }

    private void WriteLog(Table table, string? playerSecret, string message)
    {
        var tablePlayer = table.Players.Find(x => x.AuthSecret == playerSecret);
        var playerIndex = tablePlayer == null ? null : (int?)table.Game.Players.IndexOf(tablePlayer.Player);

        var logger = LogManager.GetCurrentClassLogger()
            .WithProperty("TableId", table.Number + " " + table.Id)
            .WithProperty("PlayerId", playerSecret)
            .WithProperty("PlayerIndex", playerIndex);

        logger.Info(message);
    }
}
