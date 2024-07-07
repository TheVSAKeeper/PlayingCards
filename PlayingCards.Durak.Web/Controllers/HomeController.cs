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

        public HomeController(ILogger<HomeController> logger, TableHolder tableHolder,
            IHubContext<GameHub> hubContext)
        {
            _logger = logger;
            _tableHolder = tableHolder;
            _hubContext = hubContext;
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

        [HttpPost]
        public async Task Leave([FromBody] AuthModel model)
        {
            _tableHolder.Leave(model.PlayerSecret);
            await _hubContext.Clients.All.SendAsync("ChangeStatus");
        }

        [HttpGet]
        public JsonResult GetStatus(string playerSecret)
        {
            var playerTable = _tableHolder.GetBySecret(playerSecret, out Player player);
            var result = new GetStatusModel();
            if (playerTable != null)
            {
                var game = playerTable.Game;
                var table = new TableModel();
                table.Id = playerTable.Id;
                table.ActivePlayerIndex = game.Players.IndexOf(game.ActivePlayer);
                table.DefencePlayerIndex = game.Players.IndexOf(game.DefencePlayer);
                table.MyPlayerIndex = game.Players.IndexOf(player);
                table.OwnerIndex = game.Players.IndexOf(playerTable.Owner);
                table.LooserPlayerIndex = game.LooserPlayer == null ? null : game.Players.IndexOf(game.LooserPlayer);
                table.NeedShowCardMinTrumpValue = game.NeedShowCardMinTrumpValue;
                table.MyCards = game.Players.First(x => x == player).Hand.Cards
                            .Select(x => new CardModel(x)).ToArray();
                table.DeckCardsCount = game.Deck.CardsCount;
                table.Trump = game.Deck.TrumpCard == null ? null :
                        new CardModel(game.Deck.TrumpCard);
                table.Cards = game.Cards.Select(x => new TableCardModel
                {
                    AttackCard = new CardModel(x.AttackCard),
                    DefenceCard = x.DefenceCard == null ? null : new CardModel(x.DefenceCard),
                }).ToArray();
                table.Players = game.Players.Where(x => x != player)
                                .Select((x, i) => new PlayerModel
                                {
                                    Index = i,
                                    Name = x.Name,
                                    CardsCount = x.Hand.Cards.Count
                                }).ToArray();
                table.Status = (int)game.Status;
                table.StopRoundStatus = playerTable.StopRoundStatus == null ? null : (int)playerTable.StopRoundStatus;
                table.StopRoundEndDate = playerTable.StopRoundBeginDate == null ? null : playerTable.StopRoundBeginDate.Value.AddSeconds(TableHolder.STOP_ROUND_SECONDS);

                if (playerTable.LeavePlayer != null)
                {
                    table.LeavePlayer = new PlayerModel
                    {
                        Index = playerTable.LeavePlayerIndex.Value,
                        Name = playerTable.LeavePlayer.Name,
                        CardsCount = playerTable.LeavePlayer.Hand.Cards.Count
                    };
                }
                result.Table = table;
            }
            result.Tables = playerTable != null ? null : _tableHolder.GetTables().Select(x => new TableModel
            {
                Id = x.Id,
                Players = x.PlayerSecrets.Select(x => x.Value)
                    .Select(x => new PlayerModel { Name = x.Name }).ToArray(),
            }).ToArray();
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
            table.Game.StartGame();
            table.LeavePlayer = null;
            table.LeavePlayerIndex = null;
            await _hubContext.Clients.All.SendAsync("ChangeStatus");
        }

        [HttpPost]
        public async Task StartAttack([FromBody] AttackModel model)
        {
            var table = _tableHolder.Get(model.TableId);
            CheckGameInProcess(table);

            var player = table.PlayerSecrets[model.PlayerSecret];
            player.Hand.StartAttack(model.CardIndexes);
            await _hubContext.Clients.All.SendAsync("ChangeStatus");
        }

        [HttpPost]
        public async Task Attack([FromBody] AttackModel model)
        {
            var table = _tableHolder.Get(model.TableId);
            CheckGameInProcess(table);

            var player = table.PlayerSecrets[model.PlayerSecret];
            player.Hand.Attack(model.CardIndexes);

            if (table.StopRoundStatus != null)
            {
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
            await _hubContext.Clients.All.SendAsync("ChangeStatus");
        }

        [HttpPost]
        public async Task Defence([FromBody] DefenceModel model)
        {
            var table = _tableHolder.Get(model.TableId);
            CheckGameInProcess(table);

            var player = table.PlayerSecrets[model.PlayerSecret];
            player.Hand.Defence(model.DefenceCardIndex, model.AttackCardIndex);
            await _hubContext.Clients.All.SendAsync("ChangeStatus");
        }

        [HttpPost]
        public async Task Take([FromBody] BaseTableModel model)
        {
            var table = _tableHolder.Get(model.TableId);
            CheckGameInProcess(table);

            var player = table.PlayerSecrets[model.PlayerSecret];
            if (table.Game.DefencePlayer != player)
            {
                throw new Exception("you are not defence player");
            }

            CheckStopRoundBeginDate(table);
            table.StopRoundStatus = StopRoundStatus.Take;
            await _hubContext.Clients.All.SendAsync("ChangeStatus");
        }

        [HttpPost]
        public async Task SuccessDefence([FromBody] BaseTableModel model)
        {
            var table = _tableHolder.Get(model.TableId);
            CheckGameInProcess(table);

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
            await _hubContext.Clients.All.SendAsync("ChangeStatus");
        }

        private static void CheckGameInProcess(Table table)
        {
            if (table.Game.Status != GameStatus.InProcess)
            {
                throw new Exception("game not in process");
            }
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
