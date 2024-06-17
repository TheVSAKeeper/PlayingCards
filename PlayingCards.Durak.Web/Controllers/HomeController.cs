using System.Diagnostics;
using System.Numerics;
using Microsoft.AspNetCore.Mvc;
using PlayingCards.Durak.Web.Business;
using PlayingCards.Durak.Web.Models;

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
            var table = new Table { Game = game, PlayerSecrets = new Dictionary<string, Player>() };
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
            var result = new
            {
                Table = playerTable == null ? null :
                new
                {
                    Id = playerTable.Id,
                    MyCards = playerTable.Game.Players.First(x => x == player).Hand.Cards
                        .Select(x => new { Rank = x.Rank.Value, Suit = x.Suit.Value }),
                    DeckCardsCount = playerTable.Game.Deck.CardsCount,
                    Trump = playerTable.Game.Deck.TrumpCard == null ? null :
                    new
                    {
                        Rank = playerTable.Game.Deck.TrumpCard.Rank.Value,
                        Suit = playerTable.Game.Deck.TrumpCard.Suit.Value,
                    }
                },
                Tables = playerTable != null ? null : _tables.Select(x => new
                {
                    Id = x.Value.Id
                }),
            };
            return Json(result);
        }

        [HttpPost]
        public void Join(JoinModel model)
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
                var player = table.Game.AddPlayer(model.PlayerName);
                table.PlayerSecrets.Add(model.PlayerSecret, player);
                var debug = true;
                if (debug)
                {
                    table.Game.AddPlayer("противник");
                    table.Game.InitCardDeck();
                }
            }
            else
            {
                throw new Exception("table not found");
            }
        }

        [HttpPost]
        public void StartAttack(AttackModel model)
        {
            Table? playerTable = null;
            Player? player = null;
            foreach (var table in _tables.Values)
            {
                if (table.PlayerSecrets.ContainsKey(model.PlayerSecret))
                {
                    playerTable = table;
                    player = table.PlayerSecrets[model.PlayerSecret];
                }
            }
            player.Hand.StartAttack(model.CardIndexes);
        }

        [HttpPost]
        public void Attack(AttackModel model)
        {
            Table? playerTable = null;
            Player? player = null;
            foreach (var table in _tables.Values)
            {
                if (table.PlayerSecrets.ContainsKey(model.PlayerSecret))
                {
                    playerTable = table;
                    player = table.PlayerSecrets[model.PlayerSecret];
                }
            }
            player.Hand.Attack(model.CardIndexes);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
