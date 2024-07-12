using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using PlayingCards.Durak.Web.Business;
using PlayingCards.Durak.Web.Models;
using static PlayingCards.Durak.Web.Models.GetStatusModel;

namespace PlayingCards.Durak.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly TableHolder _tableHolder;

        public HomeController(ILogger<HomeController> logger, TableHolder tableHolder)
        {
            _logger = logger;
            _tableHolder = tableHolder;
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
            var table = _tableHolder.CreateTable(); // todo сразу посадить за стол создателя
            return table.Id;
        }

        [HttpPost]
        public async Task Join([FromBody] JoinModel model)
        {
            _tableHolder.Join(model.TableId, model.PlayerSecret, model.PlayerName);
        }

        [HttpPost]
        public async Task Leave([FromBody] AuthModel model)
        {
            _tableHolder.Leave(model.PlayerSecret);
        }

        [HttpGet]
        public JsonResult GetStatus(string playerSecret, int? version = null)
        {
            var table = _tableHolder.GetBySecret(playerSecret, out TablePlayer tablePlayer);
            var result = new GetStatusModel();
            if (table != null)
            {
                result.Version = table.Version;
            }
            else
            {
                result.Version = _tableHolder.TablesVersion;
            }
            if (version != null && version == result.Version)
            {
                return Json(result);
            }

            if (table != null)
            {
                var game = table.Game;
                var tableDto = new TableModel();
                tableDto.Id = table.Id;
                tableDto.ActivePlayerIndex = game.ActivePlayer == null ? null : game.Players.IndexOf(game.ActivePlayer);
                tableDto.DefencePlayerIndex = game.DefencePlayer == null ? null : game.Players.IndexOf(game.DefencePlayer);
                tableDto.MyPlayerIndex = game.Players.IndexOf(tablePlayer.Player);
                tableDto.OwnerIndex = game.Players.IndexOf(table.Owner);
                tableDto.AfkEndTime = tablePlayer.AfkStartTime == null ? null : tablePlayer.AfkStartTime.Value.AddSeconds(TableHolder.AFK_SECONDS);
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
                                    AfkEndTime = x.AfkStartTime == null ? null : x.AfkStartTime.Value.AddSeconds(TableHolder.AFK_SECONDS),
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
            else
            {
                result.Tables = _tableHolder.GetTables().Select(x => new TableModel
                {
                    Id = x.Id,
                    Players = x.Players
                        .Select(x => new PlayerModel { Name = x.Player.Name }).ToArray(),
                }).ToArray();
            }
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
            table.StartGame();
        }

        [HttpPost]
        public async Task StartAttack([FromBody] AttackModel model)
        {
            var table = _tableHolder.Get(model.TableId);

            table.StartAttack(model.PlayerSecret, model.CardIndexes);
        }

        [HttpPost]
        public async Task Attack([FromBody] AttackModel model)
        {
            var table = _tableHolder.Get(model.TableId);
            table.Attack(model.PlayerSecret, model.CardIndexes);
        }

        [HttpPost]
        public async Task Defence([FromBody] DefenceModel model)
        {
            var table = _tableHolder.Get(model.TableId);

            table.Defence(model.PlayerSecret, model.DefenceCardIndex, model.AttackCardIndex);
        }

        [HttpPost]
        public async Task Take([FromBody] BaseTableModel model)
        {
            var table = _tableHolder.Get(model.TableId);

            table.Take(model.PlayerSecret);
        }

        // todo зачем это нажимать, если в методе Defence можно добавить проверку, что все карты отбиты и вызвать эту логику автоматически
        [HttpPost]
        public async Task SuccessDefence([FromBody] BaseTableModel model)
        {
            var table = _tableHolder.Get(model.TableId);
            table.SuccessDefence(model.PlayerSecret);
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
