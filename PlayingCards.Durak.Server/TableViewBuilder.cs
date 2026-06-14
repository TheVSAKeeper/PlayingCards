using static PlayingCards.Durak.Server.GetStatusModel;

namespace PlayingCards.Durak.Server;

/// <summary>
/// Сборка view-модели стола из игрового состояния. Единый источник правды для всех фронтов.
/// </summary>
public static class TableViewBuilder
{
    /// <summary>
    /// Полное состояние стола глазами конкретного игрока (внутриигровой экран).
    /// </summary>
    public static TableModel BuildTable(Table table, TablePlayer me)
    {
        var game = table.Game;

        var tableDto = new TableModel
        {
            Id = table.Id,
            ActivePlayerIndex = game.ActivePlayer == null ? null : game.Players.IndexOf(game.ActivePlayer),
            DefencePlayerIndex = game.DefencePlayer == null ? null : game.Players.IndexOf(game.DefencePlayer),
            MyPlayerIndex = game.Players.IndexOf(me.Player),
            OwnerIndex = game.Players.IndexOf(table.Owner),
            AfkEndTime = me.AfkStartTime?.AddSeconds(TableHolder.AFK_SECONDS),
            LooserPlayerIndex = game.LooserPlayer == null ? null : game.Players.IndexOf(game.LooserPlayer),
            NeedShowCardMinTrumpValue = game.NeedShowCardMinTrumpValue,
            DeckCardsCount = game.Deck.CardsCount,
            DiscardCardsCount = game.DiscardCardsCount,
            Trump = game.Deck.TrumpCard == null ? null : new CardModel(game.Deck.TrumpCard),
            Status = (int)game.Status,
            StopRoundStatus = table.StopRoundStatus == null ? null : (int)table.StopRoundStatus,
            StopRoundEndDate = table.StopRoundBeginDate == null || table.StopRoundStatus == null
                ? null
                : table.StopRoundBeginDate.Value.AddSeconds(TableHolder.GetStopRoundSeconds(table.StopRoundStatus.Value)),
        };

        var mePlayer = game.Players.FirstOrDefault(x => x == me.Player);
        tableDto.MyCards = mePlayer == null
            ? []
            : mePlayer.Hand.Cards
                .Select(x => new CardModel(x))
                .ToArray();

        tableDto.Cards = game.Cards.Select(x => new TableCardModel
            {
                AttackCard = new(x.AttackCard),
                DefenceCard = x.DefenceCard == null ? null : new CardModel(x.DefenceCard),
            })
            .ToArray();

        tableDto.Players = table.Players.Where(x => x.Player != me.Player)
            .Select((x, i) => new PlayerModel
            {
                Index = i,
                Name = x.Player.Name,
                CardsCount = x.Player.Hand.Cards.Count,
                AfkEndTime = x.AfkStartTime?.AddSeconds(TableHolder.AFK_SECONDS),
            })
            .ToArray();

        if (table.LeavePlayer != null)
        {
            tableDto.LeavePlayer = new PlayerModel
            {
                Index = table.LeavePlayerIndex!.Value,
                Name = table.LeavePlayer.Name,
                CardsCount = table.LeavePlayer.Hand.Cards.Count,
            };
        }

        return tableDto;
    }

    /// <summary>
    /// Список столов для лобби (когда игрок ещё не сидит за столом).
    /// </summary>
    public static TableModel[] BuildLobby(IEnumerable<Table> tables)
    {
        return tables
            .Select(x => new TableModel
            {
                Id = x.Id,
                Players = x.Players
                    .Select(p => new PlayerModel { Name = p.Player.Name })
                    .ToArray(),
            })
            .ToArray();
    }
}
