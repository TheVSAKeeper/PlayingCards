using Microsoft.AspNetCore.SignalR;
using NLog;
using PlayingCards.Durak.Web.Hubs;
using PlayingCards.Durak.Web.Models;
using System.Text;
using static PlayingCards.Durak.Web.Models.GetStatusModel;

namespace PlayingCards.Durak.Web.Business;

/// <summary>
/// Игровой стол.
/// </summary>
public class Table
{
    private IHubContext<GameHub>? _hubContext;

    /// <summary>
    /// Идентификатор.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Игра.
    /// </summary>
    public Game Game { get; set; }

    /// <summary>
    /// Секреты игроков, чтоб понять, кто есть кто.
    /// </summary>
    public List<TablePlayer> Players { get; set; }

    /// <summary>
    /// Хозяин стола.
    /// </summary>
    public Player Owner { get; set; }

    /// <summary>
    /// Время начала отсчёта об окончании раунда.
    /// </summary>
    public DateTime? StopRoundBeginDate { get; set; }

    /// <summary>
    /// Причина окончания раунда.
    /// </summary>
    public StopRoundStatus? StopRoundStatus { get; set; }

    /// <summary>
    /// Игрок, покинувший игру, до её окончания.
    /// </summary>
    public Player? LeavePlayer { get; set; }

    /// <summary>
    /// Индекс игрока, покинувшего игру.
    /// </summary>
    public int? LeavePlayerIndex { get; set; }

    /// <summary>
    /// Номер версии данных, на любой чих мы его повышаем.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Порядковый номер стола.
    /// </summary>
    public int Number { get; set; }

    public void SetHubContext(IHubContext<GameHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task BroadcastGameStateAsync()
    {
        if (_hubContext == null || Players.Count == 0)
        {
            return;
        }

        try
        {
            var groupName = $"Table_{Id}";

            var gameState = GetTableState(Players.First());
            await _hubContext.Clients.Group(groupName).SendAsync("GameStateChanged", gameState);
        }
        catch (Exception ex)
        {
            var logger = LogManager.GetCurrentClassLogger();
            logger.Error(ex, "Failed to broadcast game state for table {TableId}", Id);
        }
    }

    public void SetActivePlayerAfkStartTime()
    {
        Players.First(x => x.Player == Game.ActivePlayer).AfkStartTime = DateTime.UtcNow;
    }

    public void SetDefencePlayerAfkStartTime()
    {
        Players.First(x => x.Player == Game.ActivePlayer).AfkStartTime = null;
        Players.First(x => x.Player == Game.DefencePlayer).AfkStartTime = DateTime.UtcNow;
    }

    public void CleanDefencePlayerAfkStartTime()
    {
        Players.First(x => x.Player == Game.DefencePlayer).AfkStartTime = null;
    }

    public void CleanAllAfkTime()
    {
        foreach (var player in Players)
        {
            player.AfkStartTime = null;
        }
    }

    public void CleanLeaverPlayer()
    {
        LeavePlayer = null;
        LeavePlayerIndex = null;
    }

    public async Task StartGame()
    {
        Game.StartGame();
        var log = new StringBuilder();
        log.AppendLine("deck: " + string.Join(' ', Game.Deck.Cards.Select(x => x.ToString())));

        for (var i = 0; i < Game.Players.Count; i++)
        {
            log.AppendLine("pl-" + i + ": " + string.Join(' ', Game.Players[i].Hand.Cards.Select(x => x.ToString())));
        }

        CleanLeaverPlayer();
        SetActivePlayerAfkStartTime();
        Version++;

        WriteLog("", "start game: \r\n" + log);

        await BroadcastGameStateAsync();
    }

    public async Task PlayCards(string playerSecret, int[] cardIndexes, int? attackCardIndex = null)
    {
        CheckGameInProcess();
        var tablePlayer = GetPlayer(playerSecret);

        if (attackCardIndex != null)
        {
            Defence(tablePlayer, cardIndexes.First(), attackCardIndex.Value);
        }
        else
        {
            if (Game.IsRoundStarted())
            {
                Attack(tablePlayer, cardIndexes);
            }
            else
            {
                StartAttack(tablePlayer, cardIndexes);
            }
        }

        CheckEndGame();
        Version++;

        await BroadcastGameStateAsync();
    }

    public async Task Take(string playerSecret)
    {
        CheckGameInProcess();

        var tablePlayer = GetPlayer(playerSecret);

        if (Game.DefencePlayer != tablePlayer.Player)
        {
            throw new BusinessException("you are not defence player");
        }

        if (Game.Cards.Count == 0)
        {
            throw new BusinessException("На столе нет карт");
        }

        CheckStopRoundBeginDate();

        if (Game.Cards.Count == 6 || tablePlayer.Player.Hand.Cards.Count == Game.Cards.Count(x => x.DefenceCard == null))
        {
            StopRoundStatus = Business.StopRoundStatus.TakeFull;
        }
        else
        {
            StopRoundStatus = Business.StopRoundStatus.Take;
        }

        CleanDefencePlayerAfkStartTime();
        WriteLog(playerSecret, "take");
        Version++;

        await BroadcastGameStateAsync();
    }

    public TablePlayer GetPlayer(string playerSecret)
    {
        var tablePlayer = Players.Find(x => x.AuthSecret == playerSecret)
                          ?? throw new BusinessException("player not found");

        return tablePlayer;
    }

    private static StringBuilder GetCardsLog(int[] cardIndexes, TablePlayer tablePlayer)
    {
        StringBuilder logCards = new();
        var cards = tablePlayer.Player.Hand.Cards;

        foreach (var cardIndex in cardIndexes)
        {
            if (cardIndex >= 0 && cardIndex < cards.Count)
            {
                var card = cards[cardIndex];
                logCards.Append(' ').Append(card);
            }
            else
            {
                throw new BusinessException($"Ошибка при получении логов карт: индекс {cardIndex} вне диапазона.");
            }
        }

        return logCards;
    }

    private GetStatusModel GetTableState(TablePlayer player)
    {
        var game = Game;

        var result = new GetStatusModel
        {
            Version = Version,
            Table = new()
            {
                Id = Id,
                ActivePlayerIndex = game.ActivePlayer == null ? null : game.Players.IndexOf(game.ActivePlayer),
                DefencePlayerIndex = game.DefencePlayer == null ? null : game.Players.IndexOf(game.DefencePlayer),
                MyPlayerIndex = game.Players.IndexOf(player.Player),
                OwnerIndex = game.Players.IndexOf(Owner),
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

                Players = Players.Where(x => x.Player != player.Player)
                    .Select((x, i) => new PlayerModel
                    {
                        Index = i,
                        Name = x.Player.Name,
                        CardsCount = x.Player.Hand.Cards.Count,
                        AfkEndTime = x.AfkStartTime?.AddSeconds(TableHolder.AFK_SECONDS),
                    })
                    .ToArray(),

                Status = (int)game.Status,

                StopRoundStatus = StopRoundStatus switch
                {
                    null => null,
                    Business.StopRoundStatus.SuccessDefence => 1,
                    Business.StopRoundStatus.Take => 0,
                    Business.StopRoundStatus.TakeFull => 0,
                    _ => (int)StopRoundStatus,
                },

                StopRoundEndDate = StopRoundBeginDate == null
                    ? null
                    : TableHolder.GetSecond(StopRoundBeginDate.Value, StopRoundStatus),
            },
        };

        if (LeavePlayer != null)
        {
            result.Table.LeavePlayer = new()
            {
                Index = LeavePlayerIndex!.Value,
                Name = LeavePlayer.Name,
                CardsCount = LeavePlayer.Hand.Cards.Count,
            };
        }

        return result;
    }

    private void StartAttack(TablePlayer tablePlayer, int[] cardIndexes)
    {
        var logCards = GetCardsLog(cardIndexes, tablePlayer);

        tablePlayer.Player.Hand.StartAttack(cardIndexes);
        WriteLog(tablePlayer.AuthSecret, $"start attack{logCards}");

        SetDefencePlayerAfkStartTime();
    }

    private void Attack(TablePlayer tablePlayer, int[] cardIndexes)
    {
        var logCards = GetCardsLog(cardIndexes, tablePlayer);

        tablePlayer.Player.Hand.Attack(cardIndexes);
        WriteLog(tablePlayer.AuthSecret, $"attack{logCards}");

        if (StopRoundStatus == null)
        {
            return;
        }

        switch (StopRoundStatus)
        {
            case Business.StopRoundStatus.SuccessDefence:
                SetDefencePlayerAfkStartTime();
                StopRoundBeginDate = null;
                StopRoundStatus = null;
                break;

            case Business.StopRoundStatus.Take:
            case Business.StopRoundStatus.TakeFull:
                break;

            default:
                throw new BusinessException($"Неопределённый статус остановки раунда: {StopRoundStatus}");
        }
    }

    private void Defence(TablePlayer tablePlayer, int defenceCardIndex, int attackCardIndex)
    {
        if (StopRoundBeginDate != null)
        {
            throw new BusinessException("Раунд в процессе остановки");
        }

        StringBuilder logCards = new();

        try
        {
            var defenceCard = tablePlayer.Player.Hand.Cards[defenceCardIndex];
            var tableCard = Game.Cards[attackCardIndex].AttackCard;
            logCards.Append($" {defenceCard}->{tableCard}");
        }
        catch (Exception exception)
        {
            throw new BusinessException($"Ошибка при попытке записи логов защиты: {exception.Message}");
        }

        tablePlayer.Player.Hand.Defence(defenceCardIndex, attackCardIndex);
        WriteLog(tablePlayer.AuthSecret, $"defence{logCards}");

        SetDefencePlayerAfkStartTime();

        if (Game.Cards.Any(tableCard => tableCard.DefenceCard == null))
        {
            return;
        }

        CheckStopRoundBeginDate();
        StopRoundStatus = Business.StopRoundStatus.SuccessDefence;
        CleanDefencePlayerAfkStartTime();
    }

    private void CheckGameInProcess()
    {
        if (Game.Status != GameStatus.InProcess)
        {
            throw new BusinessException("game not in process");
        }
    }

    private void CheckStopRoundBeginDate()
    {
        if (StopRoundBeginDate != null)
        {
            throw new BusinessException("stop round in process");
        }

        StopRoundBeginDate = DateTime.UtcNow;
    }

    private void CheckEndGame()
    {
        if (Game.Status != GameStatus.InProcess)
        {
            CleanAllAfkTime();
            StopRoundStatus = null;
            StopRoundBeginDate = null;
            WriteLog("", "game finish " + Game.Status);

            if (Game.LooserPlayer != null)
            {
                WriteLog("", "looser: " + Game.LooserPlayer.Name);
            }
        }
    }

    private void WriteLog(string? playerSecret, string message)
    {
        var tablePlayer = Players.Find(x => x.AuthSecret == playerSecret);
        var playerIndex = tablePlayer == null ? null : (int?)Game.Players.IndexOf(tablePlayer.Player);

        var logger = LogManager.GetCurrentClassLogger()
            .WithProperty("TableId", Number + " " + Id)
            .WithProperty("PlayerId", playerSecret)
            .WithProperty("PlayerIndex", playerIndex);

        logger.Info(message);
    }
}
