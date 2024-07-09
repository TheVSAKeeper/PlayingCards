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
            var table = _tableHolder.GetBySecret(playerSecret, out TablePlayer tablePlayer);
            var result = new GetStatusModel();
            if (table != null)
            {
                var game = table.Game;
                var tableDto = new TableModel();
                tableDto.Id = table.Id;
                tableDto.ActivePlayerIndex = game.Players.IndexOf(game.ActivePlayer);
                tableDto.DefencePlayerIndex = game.Players.IndexOf(game.DefencePlayer);
                tableDto.MyPlayerIndex = game.Players.IndexOf(tablePlayer.Player);
                tableDto.OwnerIndex = game.Players.IndexOf(table.Owner);
                tableDto.LooserPlayerIndex = game.LooserPlayer == null ? null : game.Players.IndexOf(game.LooserPlayer);
                tableDto.NeedShowCardMinTrumpValue = game.NeedShowCardMinTrumpValue;
                tableDto.MyCards = game.Players.First(x => x == tablePlayer.Player).Hand.Cards
                            .Select(x => new CardModel(x)).ToArray();
                tableDto.DeckCardsCount = game.Deck.CardsCount;
                tableDto.Trump = game.Deck.TrumpCard == null ? null :
                        new CardModel(game.Deck.TrumpCard);
                tableDto.Cards = game.Cards.Select(x => new TableCardModel
                {
                    AttackCard = new CardModel(x.AttackCard),
                    DefenceCard = x.DefenceCard == null ? null : new CardModel(x.DefenceCard),
                }).ToArray();
                tableDto.Players = table.Players.Where(x => x.Player != tablePlayer.Player)
                                .Select((x, i) => new PlayerModel
                                {
                                    Index = i,
                                    Name = x.Player.Name,
                                    CardsCount = x.Player.Hand.Cards.Count,
                                    AfkStartTime = x.AfkStartTime,
                                }).ToArray();
                tableDto.Status = (int)game.Status;
                tableDto.StopRoundStatus = table.StopRoundStatus == null ? null : (int)table.StopRoundStatus;
                tableDto.StopRoundEndDate = table.StopRoundBeginDate == null ? null : table.StopRoundBeginDate.Value.AddSeconds(TableHolder.STOP_ROUND_SECONDS);

                if (table.LeavePlayer != null)
                {
                    tableDto.LeavePlayer = new PlayerModel
                    {
                        Index = table.LeavePlayerIndex.Value,
                        Name = table.LeavePlayer.Name,
                        CardsCount = table.LeavePlayer.Hand.Cards.Count
                    };
                }
                result.Table = tableDto;
            }
            result.Tables = table != null ? null : _tableHolder.GetTables().Select(x => new TableModel
            {
                Id = x.Id,
                Players = x.Players
                    .Select(x => new PlayerModel { Name = x.Player.Name }).ToArray(),
            }).ToArray();
            return Json(result);
        }

        [HttpPost]
        public async Task StartGame([FromBody] BaseTableModel model)
        {
            var table = _tableHolder.Get(model.TableId);
            var player = table.Players.Single(x => x.AuthSecret == model.PlayerSecret).Player;
            if (table.Owner != player)
            {
                throw new Exception("you are not owner");
            }
            table.Game.StartGame();
            table.CleanLeaverPlayer();
            table.SetActivePlayerAfkStartTime();
            await _hubContext.Clients.All.SendAsync("ChangeStatus");
        }

        [HttpPost]
        public async Task StartAttack([FromBody] AttackModel model)
        {
            var table = _tableHolder.Get(model.TableId);
            CheckGameInProcess(table);

            var tablePlayer = table.Players.Single(x => x.AuthSecret == model.PlayerSecret);
            tablePlayer.Player.Hand.StartAttack(model.CardIndexes);
            table.SetDefencePlayerAfkStartTime();
            await _hubContext.Clients.All.SendAsync("ChangeStatus");
        }

        [HttpPost]
        public async Task Attack([FromBody] AttackModel model)
        {
            var table = _tableHolder.Get(model.TableId);
            CheckGameInProcess(table);

            var tablePlayer = table.Players.Single(x => x.AuthSecret == model.PlayerSecret);
            tablePlayer.Player.Hand.Attack(model.CardIndexes);
            table.SetDefencePlayerAfkStartTime();

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

            var tablePlayer = table.Players.Single(x => x.AuthSecret == model.PlayerSecret);
            tablePlayer.Player.Hand.Defence(model.DefenceCardIndex, model.AttackCardIndex);
            table.SetDefencePlayerAfkStartTime();
            await _hubContext.Clients.All.SendAsync("ChangeStatus");
        }

        [HttpPost]
        public async Task Take([FromBody] BaseTableModel model)
        {
            var table = _tableHolder.Get(model.TableId);
            CheckGameInProcess(table);

            var tablePlayer = table.Players.Single(x => x.AuthSecret == model.PlayerSecret);
            if (table.Game.DefencePlayer != tablePlayer.Player)
            {
                throw new Exception("you are not defence player");
            }

            CheckStopRoundBeginDate(table);
            table.StopRoundStatus = StopRoundStatus.Take;
            table.CleanDefencePlayerAfkStartTime();
            await _hubContext.Clients.All.SendAsync("ChangeStatus");
        }

        // todo зачем это нажимать, если в методе Defence можно добавить проверку, что все карты отбиты и вызвать эту логику автоматически
        [HttpPost]
        public async Task SuccessDefence([FromBody] BaseTableModel model)
        {
            var table = _tableHolder.Get(model.TableId);
            CheckGameInProcess(table);

            var player = table.Players.Single(x => x.AuthSecret == model.PlayerSecret).Player;
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
            table.CleanDefencePlayerAfkStartTime();
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
