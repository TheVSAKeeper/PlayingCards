using Microsoft.AspNetCore.Mvc;
using PlayingCards.Durak.Server;
using PlayingCards.Durak.Web.Models;
using System.Diagnostics;
using static PlayingCards.Durak.Server.GetStatusModel;

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
        var result = new GetStatusModel
        {
            Version = table?.Version ?? tableHolder.TablesVersion,
        };

        if (version != null && version == result.Version)
        {
            return Json(result);
        }

        if (table != null)
        {
            result.Table = TableViewBuilder.BuildTable(table, tablePlayer!);
        }
        else
        {
            result.Tables = TableViewBuilder.BuildLobby(tableHolder.GetTables());
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
