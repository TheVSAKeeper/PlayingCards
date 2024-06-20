using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PlayingCards.Durak.Web.Business;
using PlayingCards.Durak.Web.Models;
using static PlayingCards.Durak.Web.Models.GetStatusModel;

namespace PlayingCards.Durak.Web.Controllers
{
    public class HomeController : Controller
    {
        private static Dictionary<Guid, Table> _tables = new Dictionary<Guid, Table>();

        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [HttpPost]
        public Guid CreateTable()
        {
            var tableId = Guid.NewGuid();
            var game = new Game();
            var table = new Table { Id = tableId, Game = game, PlayerSecrets = new Dictionary<string, Player>() };
            _tables.Add(tableId, table);
            return tableId;
        }

        [HttpGet]
        public JsonResult GetStatus(string playerSecret)
        {
            Table? playerTable = null;
            Player? player = null;
            foreach (var table in _tables.Values)
            {
                if (table.PlayerSecrets.ContainsKey(playerSecret))
                {
                    playerTable = table;
                    player = table.PlayerSecrets[playerSecret];
                }
            }
            var result = new GetStatusModel
            {
                Table = playerTable == null ? null :
                new TableModel
                {
                    Id = playerTable.Id,
                    ActivePlayerIndex = playerTable.Game.Players.IndexOf(playerTable.Game.ActivePlayer),
                    MyIndex = playerTable.Game.Players.IndexOf(player),
                    OwnerIndex = playerTable.Game.Players.IndexOf(playerTable.Owner),
                    MyCards = playerTable.Game.Players.First(x => x == player).Hand.Cards
                        .Select(x => new CardModel(x)).ToArray(),
                    DeckCardsCount = playerTable.Game.Deck.CardsCount,
                    Trump = playerTable.Game.Deck.TrumpCard == null ? null :
                    new CardModel(playerTable.Game.Deck.TrumpCard),
                    Cards = playerTable.Game.Cards.Select(x => new TableCardModel
                    {
                        AttackCard = new CardModel(x.AttackCard),
                        DefenceCard = x.DefenceCard == null ? null : new CardModel(x.DefenceCard),
                    }).ToArray(),
                    Players = playerTable.Game.Players.Where(x => x != player)
                        .Select((x, i) => new PlayerModel { Index = i, Name = x.Name, CardsCount = x.Hand.Cards.Count })
                        .ToArray(),
                    Status = (int)playerTable.Game.Status,
                },
                Tables = playerTable != null ? null : _tables.Select(x => new TableModel
                {
                    Id = x.Value.Id,
                    Players = x.Value.PlayerSecrets.Select(x => x.Value)
                    .Select(x => new PlayerModel { Name = x.Name }).ToArray(),
                }).ToArray(),
            };
            return Json(result);
        }

        [HttpPost]
        public void Join([FromBody] JoinModel model)
        {
            foreach (var table2 in _tables.Values)
            {
                if (table2.PlayerSecrets.ContainsKey(model.PlayerSecret))
                {
                    throw new Exception("Вы уже сидите за столиком");
                }
            }

            if (_tables.TryGetValue(model.TableId, out var table))
            {
                var debug = false;
                if (debug)
                {
                    table.Game.AddPlayer("1 Вася");
                    table.Game.AddPlayer("2 Петя");
                }


                var player = table.Game.AddPlayer(model.PlayerName);
                table.PlayerSecrets.Add(model.PlayerSecret, player);
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
                    table.Game.InitCardDeck();
                    table.Game.ActivePlayer.Hand.StartAttack([3]);
                }
            }
            else
            {
                throw new Exception("table not found");
            }
        }

        [HttpPost]
        public void StartGame([FromBody] StartGameModel model)
        {
            var table = _tables[model.TableId];
            var player = table.PlayerSecrets[model.PlayerSecret];
            if(table.Owner != player)
            {
                throw new Exception("you are not owner");
            }
            table.Game.InitCardDeck();
        }

        [HttpPost]
        public void StartAttack([FromBody] AttackModel model)
        {
            var table = _tables[model.TableId];
            var player = table.PlayerSecrets[model.PlayerSecret];
            player.Hand.StartAttack(model.CardIndexes);
        }

        [HttpPost]
        public void Attack([FromBody] AttackModel model)
        {
            var table = _tables[model.TableId];
            var player = table.PlayerSecrets[model.PlayerSecret];
            player.Hand.Attack(model.CardIndexes);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
