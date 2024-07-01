using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using PlayingCards.Durak.Web.Business;
using PlayingCards.Durak.Web.Models;
using PlayingCards.Durak.Web.SignalR.Hubs;
using static PlayingCards.Durak.Web.Models.GetStatusModel;

namespace PlayingCards.Durak.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly TableHolder _tableHolder;
        private readonly IHubContext<GameHub> _hubContext;
        private readonly IHubContext<ChatHub>  _chatHub;

        public HomeController(ILogger<HomeController> logger, TableHolder tableHolder,
            IHubContext<GameHub> hubContext,
            IHubContext<ChatHub> chatHub)
        {
            _logger = logger;
            _tableHolder = tableHolder;
            _hubContext = hubContext;
            _chatHub = chatHub;
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
        public async Task<Guid> CreateTable()
        {
            var table = _tableHolder.CreateTable();
            await _hubContext.Clients.All.SendAsync("ChangeStatus");
            return table.Id;
        }

        [HttpPost]
        public async Task Join([FromBody] JoinModel model)
        {
            _tableHolder.Join(model.TableId, model.PlayerSecret, model.PlayerName);
            await _hubContext.Clients.All.SendAsync("ChangeStatus");
        }

        [HttpGet]
        public JsonResult GetStatus(string playerSecret)
        {
            var playerTable = _tableHolder.GetBySecret(playerSecret, out Player player);
            var result = new GetStatusModel
            {
                Table = playerTable == null ? null :
                new TableModel
                {
                    Id = playerTable.Id,
                    ActivePlayerIndex = playerTable.Game.Players.IndexOf(playerTable.Game.ActivePlayer),
                    DefencePlayerIndex = playerTable.Game.Players.IndexOf(playerTable.Game.DefencePlayer),
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
                Tables = playerTable != null ? null : _tableHolder.GetTables().Select(x => new TableModel
                {
                    Id = x.Id,
                    Players = x.PlayerSecrets.Select(x => x.Value)
                    .Select(x => new PlayerModel { Name = x.Name }).ToArray(),
                }).ToArray(),
            };
            return Json(result);
        }

        [HttpPost]
        public async Task StartGame([FromBody] BaseTableModel model)
        {
            var table = _tableHolder.Get(model.TableId);
            var player = table.PlayerSecrets[model.PlayerSecret];
            if (table.Owner != player)
            {
                throw new Exception("you are not owner");
            }
            table.Game.InitCardDeck();
            await _hubContext.Clients.All.SendAsync("ChangeStatus");
        }

        [HttpPost]
        public void StartAttack([FromBody] AttackModel model)
        {
            var table = _tableHolder.Get(model.TableId);
            var player = table.PlayerSecrets[model.PlayerSecret];
            player.Hand.StartAttack(model.CardIndexes);
        }

        [HttpPost]
        public void Attack([FromBody] AttackModel model)
        {
            var table = _tableHolder.Get(model.TableId);
            var player = table.PlayerSecrets[model.PlayerSecret];
            player.Hand.Attack(model.CardIndexes);

            if (table.StopRoundStatus == StopRoundStatus.SuccessDefence)
            {
                table.StopRoundBeginDate = null;
                table.StopRoundStatus = null;
            }
            else if (table.StopRoundStatus == StopRoundStatus.Take)
            {
                table.StopRoundBeginDate = DateTime.UtcNow;
            }
            else
            {
                throw new Exception("undefined " + table.StopRoundStatus);
            }
        }

        [HttpPost]
        public void Defence([FromBody] DefenceModel model)
        {
            var table = _tableHolder.Get(model.TableId);
            var player = table.PlayerSecrets[model.PlayerSecret];
            player.Hand.Defence(model.DefenceCardIndex, model.AttackCardIndex);
        }

        [HttpPost]
        public void Take([FromBody] BaseTableModel model)
        {
            var table = _tableHolder.Get(model.TableId);
            var player = table.PlayerSecrets[model.PlayerSecret];
            if (table.Game.DefencePlayer != player)
            {
                throw new Exception("you are not defence player");
            }

            CheckStopRoundBeginDate(table);
            table.StopRoundStatus = StopRoundStatus.Take;
        }

        [HttpPost]
        public void SuccessDefence([FromBody] BaseTableModel model)
        {
            var table = _tableHolder.Get(model.TableId);
            var player = table.PlayerSecrets[model.PlayerSecret];
            if (table.Game.DefencePlayer != player)
            {
                throw new Exception("you are not defence player");
            }
            if (table.Game.Cards.Any(x => x.DefenceCard == null))
            {
                throw new Exception("not all cards defenced");
            }

            CheckStopRoundBeginDate(table);
            table.StopRoundStatus = StopRoundStatus.SuccessDefence;
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private void CheckStopRoundBeginDate(Table table)
        {
            if (table.StopRoundBeginDate != null)
            {
                throw new Exception("stop round in process");
            }
            else
            {
                table.StopRoundBeginDate = DateTime.UtcNow;
            }
        }
    }
}
