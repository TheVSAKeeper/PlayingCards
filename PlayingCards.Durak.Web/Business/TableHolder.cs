
using Microsoft.AspNetCore.SignalR;
using PlayingCards.Durak.Web.SignalR.Hubs;

namespace PlayingCards.Durak.Web.Business
{
    public class TableHolder
    {
        /// <summary>
        /// Время на окончание раунда.
        /// </summary>
        public const int STOP_ROUND_SECONDS = 7;

        private static Dictionary<Guid, Table> _tables = new Dictionary<Guid, Table>();

        public Table CreateTable()
        {
            var table = new Table { Id = Guid.NewGuid(), Game = new Game(), PlayerSecrets = new Dictionary<string, Player>() };
            _tables.Add(table.Id, table);
            return table;
        }

        public void Join(Guid tableId, string playerSecret, string playerName)
        {
            foreach (var table2 in _tables.Values)
            {
                if (table2.PlayerSecrets.ContainsKey(playerSecret))
                {
                    throw new Exception("Вы уже сидите за столиком");
                }
            }

            if (_tables.TryGetValue(tableId, out var table))
            {
                var debug = false;
                if (debug)
                {
                    table.Game.AddPlayer("1 Вася");
                    table.Game.AddPlayer("2 Петя");
                }


                var player = table.Game.AddPlayer(playerName);
                table.PlayerSecrets.Add(playerSecret, player);
                if (table.PlayerSecrets.Values.Count == 1)
                {
                    // кто первый сел за стол, тот и главный
                    // когда будет функция выйти из за стола, будем думать, кому отдать главенство
                    // если вышел последний игрок из за стола, то и уничтожим стол
                    table.Owner = player;
                }

                if (debug)
                {
                    table.Game.AddPlayer("4 У меня длинное имя для проверки вёрстки");
                    table.Game.AddPlayer("5 Лучик света продуктовой разработки");
                    table.Game.StartGame();
                }

                var debug2 = true;
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
            }
            else
            {
                throw new Exception("table not found");
            }
        }

        public Table Get(Guid tableId)
        {
            var table = _tables[tableId];
            return table;
        }

        public Table? GetBySecret(string playerSecret, out Player? player)
        {
            player = null;
            foreach (var table in _tables.Values)
            {
                if (table.PlayerSecrets.ContainsKey(playerSecret))
                {
                    player = table.PlayerSecrets[playerSecret];
                    return table;
                }
            }
            return null;
        }

        public Table[] GetTables()
        {
            return _tables.Values.ToArray();
        }

        public bool CheckStopRound()
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
                        hasChanges = true;
                    }
                }
            }

            return hasChanges;
        }
    }
}
