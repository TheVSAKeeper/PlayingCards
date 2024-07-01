using Microsoft.AspNetCore.SignalR;

namespace PlayingCards.Durak.Web.SignalR.Hubs
{

    public class GameHub : Hub
    {
        public async Task ChangeStatus(string user, string message)
        {
            await Clients.All.SendAsync("ChangeStatus", user, message);
        }
    }
}
