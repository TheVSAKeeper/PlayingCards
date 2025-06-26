using Microsoft.AspNetCore.Mvc;
using PlayingCards.Durak.Web.Business;
using PlayingCards.Durak.Web.Models;
using System.Diagnostics;

namespace PlayingCards.Durak.Web.Controllers;

public class HomeController(SignalRConfiguration signalRConfig) : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public JsonResult SignalRSupport()
    {
        var clientConfig = signalRConfig.GetClientConfiguration();
        return Json(clientConfig);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
