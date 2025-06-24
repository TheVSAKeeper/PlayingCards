using Microsoft.AspNetCore.Mvc;
using PlayingCards.Durak.Web.Business;
using PlayingCards.Durak.Web.Models;
using System.Diagnostics;
using static PlayingCards.Durak.Web.Models.GetStatusModel;

namespace PlayingCards.Durak.Web.Controllers;

public class HomeController(TableHolder tableHolder) : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public Guid CreateTable([FromBody] CreateTableModel model)
    {
        var table = tableHolder.CreateTable();
        tableHolder.Join(table.Id, model.PlayerSecret, model.PlayerName);

        return table.Id;
    }

    [HttpPost]
    public void Join([FromBody] JoinModel model)
    {
        tableHolder.Join(model.TableId, model.PlayerSecret, model.PlayerName);
    }

    [HttpPost]
    public void Leave([FromBody] AuthModel model)
    {
        tableHolder.Leave(model.PlayerSecret);
    }

    [HttpGet]
    public JsonResult GetStatus(string playerSecret, int? version = null)
    {
        var table = tableHolder.GetBySecret(playerSecret, out var tablePlayer);
        var result = new GetStatusModel();

        result.Version = table?.Version ?? tableHolder.TablesVersion;

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
            tableDto.AfkEndTime = tablePlayer.AfkStartTime?.AddSeconds(TableHolder.AFK_SECONDS);
            tableDto.LooserPlayerIndex = game.LooserPlayer == null ? null : game.Players.IndexOf(game.LooserPlayer);
            tableDto.NeedShowCardMinTrumpValue = game.NeedShowCardMinTrumpValue;

            tableDto.MyCards = game.Players.First(x => x == tablePlayer.Player)
                .Hand.Cards
                .Select(x => new CardModel(x))
                .ToArray();

            tableDto.DeckCardsCount = game.Deck.CardsCount;
            tableDto.Trump = game.Deck.TrumpCard == null ? null : new CardModel(game.Deck.TrumpCard);

            tableDto.Cards = game.Cards.Select(x => new TableCardModel
                {
                    AttackCard = new(x.AttackCard),
                    DefenceCard = x.DefenceCard == null ? null : new CardModel(x.DefenceCard),
                })
                .ToArray();

            tableDto.Players = table.Players.Where(x => x.Player != tablePlayer.Player)
                .Select((x, i) => new PlayerModel
                {
                    Index = i,
                    Name = x.Player.Name,
                    CardsCount = x.Player.Hand.Cards.Count,
                    AfkEndTime = x.AfkStartTime?.AddSeconds(TableHolder.AFK_SECONDS),
                })
                .ToArray();

            tableDto.Status = (int)game.Status;
            tableDto.StopRoundStatus = table.StopRoundStatus == null ? null : (int)table.StopRoundStatus;
            tableDto.StopRoundEndDate = table.StopRoundBeginDate?.AddSeconds(TableHolder.STOP_ROUND_SECONDS);

            if (table.LeavePlayer != null)
            {
                tableDto.LeavePlayer = new()
                {
                    Index = table.LeavePlayerIndex.Value,
                    Name = table.LeavePlayer.Name,
                    CardsCount = table.LeavePlayer.Hand.Cards.Count,
                };
            }

            result.Table = tableDto;
        }
        else
        {
            result.Tables = tableHolder.GetTables()
                .Select(x => new TableModel
                {
                    Id = x.Id,
                    Players = x.Players
                        .Select(x => new PlayerModel { Name = x.Player.Name })
                        .ToArray(),
                })
                .ToArray();
        }

        return Json(result);
    }

    [HttpPost]
    public void StartGame([FromBody] BaseTableModel model)
    {
        var table = tableHolder.Get(model.TableId);
        var player = table.Players.Single(x => x.AuthSecret == model.PlayerSecret).Player;

        if (table.Owner != player)
        {
            throw new BusinessException("you are not owner");
        }

        table.StartGame();
    }

    [HttpPost]
    public void PlayCards([FromBody] PlayCardsModel model)
    {
        var table = tableHolder.Get(model.TableId);

        table.PlayCards(model.PlayerSecret, model.CardIndexes, model.AttackCardIndex);
    }

    [HttpPost]
    public void Take([FromBody] BaseTableModel model)
    {
        var table = tableHolder.Get(model.TableId);

        table.Take(model.PlayerSecret);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
