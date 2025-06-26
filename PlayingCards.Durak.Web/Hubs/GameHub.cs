using Microsoft.AspNetCore.SignalR;
using PlayingCards.Durak.Web.Business;
using PlayingCards.Durak.Web.Models;
using static PlayingCards.Durak.Web.Models.GetStatusModel;

namespace PlayingCards.Durak.Web.Hubs;

public class GameHub(TableHolder tableHolder, ILogger<GameHub> logger) : Hub
{
    public override async Task OnConnectedAsync()
    {
        var connectionId = Context.ConnectionId;
        logger.LogDebug("Client connected: {ConnectionId}", connectionId);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var connectionId = Context.ConnectionId;

        if (exception != null)
        {
            logger.LogWarning(exception, "Client disconnected with error: {ConnectionId}", connectionId);
        }
        else
        {
            logger.LogDebug("Client disconnected: {ConnectionId}", connectionId);
        }

        await CleanupConnectionGroups(connectionId);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task<Guid> CreateTable(string playerSecret, string playerName)
    {
        try
        {
            var table = await tableHolder.CreateTable();
            await tableHolder.Join(table.Id, playerSecret, playerName);

            var groupName = $"Table_{table.Id}";
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

            logger.LogDebug("Player {PlayerSecret} created and joined table {TableId}", playerSecret, table.Id);

            var player = table.Players.First(p => p.AuthSecret == playerSecret);
            await Clients.Caller.SendAsync("GameStateChanged", GetTableState(table, player));

            return table.Id;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating table for player {PlayerSecret}", playerSecret);
            await Clients.Caller.SendAsync("Error", "Failed to create table");
            return Guid.Empty;
        }
    }

  public async Task JoinExistingTable(Guid tableId, string playerSecret, string playerName)
    {
        try
        {
            await tableHolder.Join(tableId, playerSecret, playerName);

            var table = tableHolder.Get(tableId);
            var groupName = $"Table_{table.Id}";
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

            logger.LogDebug("Player {PlayerSecret} joined table {TableId}", playerSecret, tableId);

            var player = table.Players.First(p => p.AuthSecret == playerSecret);
            await Clients.Caller.SendAsync("GameStateChanged", GetTableState(table, player));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error joining table {TableId} for player {PlayerSecret}", tableId, playerSecret);
            await Clients.Caller.SendAsync("Error", "Failed to join table");
        }
    }

  public async Task JoinTable(string playerSecret)
    {
        try
        {
            var table = tableHolder.GetBySecret(playerSecret, out var player);

            if (table == null || player == null)
            {
                await Clients.Caller.SendAsync("Error", "Player not found at any table");
                return;
            }

            var groupName = $"Table_{table.Id}";
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

            logger.LogDebug("Player {PlayerSecret} joined table group {GroupName}", playerSecret, groupName);

            await Clients.Caller.SendAsync("GameStateChanged", GetTableState(table, player));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error joining table for player {PlayerSecret}", playerSecret);
            await Clients.Caller.SendAsync("Error", "Failed to join table");
        }
    }

    public async Task LeaveTable(string playerSecret)
    {
        try
        {
            var table = tableHolder.GetBySecret(playerSecret, out _);

            if (table != null)
            {
                var groupName = $"Table_{table.Id}";
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

                logger.LogDebug("Player {PlayerSecret} left table group {GroupName}", playerSecret, groupName);
            }

            await tableHolder.Leave(playerSecret);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error leaving table for player {PlayerSecret}", playerSecret);
            await Clients.Caller.SendAsync("Error", "Failed to leave table");
        }
    }

    public async Task PlayCards(string playerSecret, int[] cardIndexes, int? attackCardIndex = null)
    {
        try
        {
            var table = tableHolder.GetBySecret(playerSecret, out var player);

            if (table == null || player == null)
            {
                await Clients.Caller.SendAsync("Error", "Player not found at any table");
                return;
            }

            await table.PlayCards(playerSecret, cardIndexes, attackCardIndex);

            logger.LogDebug("Player {PlayerSecret} played cards", playerSecret);
        }
        catch (BusinessException ex)
        {
            await Clients.Caller.SendAsync("GameError", ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error playing cards for player {PlayerSecret}", playerSecret);
            await Clients.Caller.SendAsync("Error", "Failed to play cards");
        }
    }

    public async Task TakeCards(string playerSecret)
    {
        try
        {
            var table = tableHolder.GetBySecret(playerSecret, out var player);

            if (table == null || player == null)
            {
                await Clients.Caller.SendAsync("Error", "Player not found at any table");
                return;
            }

            await table.Take(playerSecret);

            logger.LogDebug("Player {PlayerSecret} took cards", playerSecret);
        }
        catch (BusinessException ex)
        {
            await Clients.Caller.SendAsync("GameError", ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error taking cards for player {PlayerSecret}", playerSecret);
            await Clients.Caller.SendAsync("Error", "Failed to take cards");
        }
    }

    public async Task StartGame(string playerSecret)
    {
        try
        {
            var table = tableHolder.GetBySecret(playerSecret, out var player);

            if (table == null || player == null)
            {
                await Clients.Caller.SendAsync("Error", "Player not found at any table");
                return;
            }

            if (table.Owner != player.Player)
            {
                await Clients.Caller.SendAsync("GameError", "You are not the table owner");
                return;
            }

            await table.StartGame();

            logger.LogDebug("Player {PlayerSecret} started game", playerSecret);
        }
        catch (BusinessException ex)
        {
            await Clients.Caller.SendAsync("GameError", ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error starting game for player {PlayerSecret}", playerSecret);
            await Clients.Caller.SendAsync("Error", "Failed to start game");
        }
    }

    private GetStatusModel GetTableState(Table table, TablePlayer player)
    {
        var game = table.Game;

        var result = new GetStatusModel
        {
            Version = table.Version,
            Table = new()
            {
                Id = table.Id,
                ActivePlayerIndex = game.ActivePlayer == null ? null : game.Players.IndexOf(game.ActivePlayer),
                DefencePlayerIndex = game.DefencePlayer == null ? null : game.Players.IndexOf(game.DefencePlayer),
                MyPlayerIndex = game.Players.IndexOf(player.Player),
                OwnerIndex = game.Players.IndexOf(table.Owner),
                AfkEndTime = player.AfkStartTime?.AddSeconds(TableHolder.AFK_SECONDS),
                LooserPlayerIndex = game.LooserPlayer == null ? null : game.Players.IndexOf(game.LooserPlayer),
                NeedShowCardMinTrumpValue = game.NeedShowCardMinTrumpValue,

                MyCards = game.Players.First(x => x == player.Player)
                    .Hand.Cards
                    .Select(x => new CardModel(x))
                    .ToArray(),

                DeckCardsCount = game.Deck.CardsCount,
                Trump = game.Deck.TrumpCard == null ? null : new CardModel(game.Deck.TrumpCard),

                Cards = game.Cards.Select(x => new TableCardModel
                    {
                        AttackCard = new(x.AttackCard),
                        DefenceCard = x.DefenceCard == null ? null : new CardModel(x.DefenceCard),
                    })
                    .ToArray(),

                Players = table.Players.Where(x => x.Player != player.Player)
                    .Select((x, i) => new PlayerModel
                    {
                        Index = i,
                        Name = x.Player.Name,
                        CardsCount = x.Player.Hand.Cards.Count,
                        AfkEndTime = x.AfkStartTime?.AddSeconds(TableHolder.AFK_SECONDS),
                    })
                    .ToArray(),

                Status = (int)game.Status,

                StopRoundStatus = table.StopRoundStatus switch
                {
                    null => null,
                    StopRoundStatus.SuccessDefence => 1,
                    StopRoundStatus.Take => 0,
                    StopRoundStatus.TakeFull => 0,
                    _ => (int)table.StopRoundStatus,
                },

                StopRoundEndDate = table.StopRoundBeginDate == null
                    ? null
                    : TableHolder.GetSecond(table.StopRoundBeginDate.Value, table.StopRoundStatus),
            },
        };

        if (table.LeavePlayer != null)
        {
            result.Table.LeavePlayer = new()
            {
                Index = table.LeavePlayerIndex.Value,
                Name = table.LeavePlayer.Name,
                CardsCount = table.LeavePlayer.Hand.Cards.Count,
            };
        }

        return result;
    }

    private async Task CleanupConnectionGroups(string connectionId)
    {
        await Task.CompletedTask;
    }
}
