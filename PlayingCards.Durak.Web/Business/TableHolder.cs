using Microsoft.Extensions.Logging;
using NLog;
using NLog.Fluent;

namespace PlayingCards.Durak.Web.Business
{
    public class TableHolder
    {
        /// <summary>
        /// Время на окончание раунда.
        /// </summary>
        public const int STOP_ROUND_SECONDS = 10;

        /// <summary>
        /// Время на принятие решения.
        /// </summary>
        public const int AFK_SECONDS = 60;

        private static Dictionary<Guid, Table> _tables = new Dictionary<Guid, Table>();
        public int TablesVersion = 0;
        public int TableNumber = 1;

        public Table CreateTable()
        {
            var table = new Table { Id = Guid.NewGuid(), Number = TableNumber, Game = new Game(), Players = new List<TablePlayer>() };
            table.Version = 0;
            _tables.Add(table.Id, table);
            TablesVersion++;

            WriteLog(table, null, "create table");
            TableNumber++;

            return table;
        }

        public void Join(Guid tableId, string playerSecret, string playerName)
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
                table.Players.Add(new TablePlayer { Player = player, AuthSecret = playerSecret });
                WriteLog(table, playerSecret, "join to table");

                if (table.Owner == null)
                {
                    table.Owner = player;
                }

                if (debug)
                {
                    var player1 = table.Game.AddPlayer("1 кореш " + playerName);
                    table.Players.Add(new TablePlayer { Player = player1, AuthSecret = "123" });
                    var player2 = table.Game.AddPlayer("2 кореш " + playerName);
                    table.Players.Add(new TablePlayer { Player = player2, AuthSecret = "123" });
                    var player4 = table.Game.AddPlayer("4 У меня длинное имя для проверки вёрстки");
                    table.Players.Add(new TablePlayer { Player = player4, AuthSecret = "123" });
                    var player5 = table.Game.AddPlayer("5 Лучик света продуктовой разработки");
                    table.Players.Add(new TablePlayer { Player = player5, AuthSecret = "123" });
                }

                var debug2 = false;
                if (debug2)
                {
                    table.Game.AddPlayer("я всегда проигрываю");
                    table.Game.StartGame();
                    table.Game.Deck.Cards = new List<Card>();
                    if (table.Game.Players.IndexOf(table.Game.ActivePlayer) == 0)
                    {
                        table.Game.Players[0].Hand.Cards = table.Game.Players[0].Hand.Cards.Take(1).ToList();
                    }
                    if (table.Game.Players.IndexOf(table.Game.ActivePlayer) == 1)
                    {
                        table.Game.Players[1].Hand.Cards = table.Game.Players[1].Hand.Cards.Take(1).ToList();
                        table.Game.Players[1].Hand.StartAttack(new int[] { 0 });
                    }
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

        public void Leave(string playerSecret)
        {
            var table = GetBySecret(playerSecret, out TablePlayer? tablePlayer);
            table.CleanLeaverPlayer();
            Leave(table, tablePlayer);
        }

        public void Leave(Table table, TablePlayer tablePlayer)
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
            if (table.Players.Count == 0)
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

        public void BackgroundProcess()
        {
            CheckStopRound();
            CheckAfkPlayers();
        }

        private void CheckStopRound()
        {
            // todo потокобезопасность натянуть
            foreach (var table in _tables.Values)
            {
                if (table.StopRoundBeginDate != null)
                {
                    var finishTime = table.StopRoundBeginDate.Value.AddSeconds(STOP_ROUND_SECONDS);
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
        }

        private void CheckAfkPlayers()
        {
            foreach (var table in _tables.Values)
            {
                for (int i = 0; i < table.Players.Count; i++)
                {
                    TablePlayer? tablePlayer = table.Players[i];
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
}
