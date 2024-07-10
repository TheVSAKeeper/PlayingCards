namespace PlayingCards.Durak.Web.Business
{
    public class TableHolder
    {
        /// <summary>
        /// Время на окончание раунда.
        /// </summary>
        public const int STOP_ROUND_SECONDS = 7;

        /// <summary>
        /// Время на принятие решения.
        /// </summary>
        public const int AFK_SECONDS = 60;

        private static Dictionary<Guid, Table> _tables = new Dictionary<Guid, Table>();

        public Table CreateTable()
        {
            var table = new Table { Id = Guid.NewGuid(), Game = new Game(), Players = new List<TablePlayer>() };
            _tables.Add(table.Id, table);
            return table;
        }

        public void Join(Guid tableId, string playerSecret, string playerName)
        {
            if (string.IsNullOrEmpty(playerSecret))
            {
                throw new Exception("Авторизуйтесь");
            }
            foreach (var table2 in _tables.Values)
            {
                if (table2.Players.Any(x => x.AuthSecret == playerSecret))
                {
                    throw new Exception("Вы уже сидите за столиком");
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
                    var player1 = table.Game.AddPlayer("1 кореш " + playerName);
                    table.Players.Add(new TablePlayer { Player = player1, AuthSecret = "123" });
                    var player2 = table.Game.AddPlayer("2 кореш " + playerName);
                    table.Players.Add(new TablePlayer { Player = player2, AuthSecret = "123" });
                }

                var player = table.Game.AddPlayer(playerName);
                table.Players.Add(new TablePlayer { Player = player, AuthSecret = playerSecret });
                if (table.Owner == null)
                {
                    table.Owner = player;
                }

                if (debug)
                {
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
                        table.Game.Players[1].Hand.StartAttack([0]);
                    }
                }

                table.CleanLeaverPlayer();
            }
            else
            {
                throw new Exception("table not found");
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
            var playerIndex = table.Game.Players.IndexOf(tablePlayer.Player);
            if (table.Game.Status == GameStatus.InProcess)
            {
                table.LeavePlayer = tablePlayer.Player;
                table.LeavePlayerIndex = playerIndex;
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

        public bool BackgroundProcess()
        {
            var hasChanges = CheckStopRound();
            hasChanges = hasChanges || CheckAfkPlayers();
            return hasChanges;
        }

        private bool CheckStopRound()
        {
            var hasChanges = false;
            // todo потокобезопасность натянуть
            foreach (var table in _tables)
            {
                if (table.Value.StopRoundBeginDate != null)
                {
                    var finishTime = table.Value.StopRoundBeginDate.Value.AddSeconds(STOP_ROUND_SECONDS);
                    if (DateTime.UtcNow >= finishTime)
                    {
                        table.Value.StopRoundStatus = null;
                        table.Value.StopRoundBeginDate = null;
                        table.Value.Game.StopRound();
                        table.Value.SetActivePlayerAfkStartTime();
                        hasChanges = true;
                    }
                }
            }

            return hasChanges;
        }

        private bool CheckAfkPlayers()
        {
            var hasChanges = false;
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
                            hasChanges = true;
                        }
                    }
                }
            }
            return hasChanges;
        }
    }
}
