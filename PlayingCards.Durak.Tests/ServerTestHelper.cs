using PlayingCards.Durak.Server;

namespace PlayingCards.Durak.Tests;

/// <summary>
/// Помощник для тестов Server-слоя: собирает стол с детерминированной раздачей.
/// </summary>
internal static class ServerTestHelper
{
    /// <summary>
    /// Стол с уже начатой игрой из фиксированных рук. Секреты игроков — "s0", "s1", ...,
    /// владелец и (при нужном козыре) ходящий — первый игрок.
    /// </summary>
    public static Table BuildStartedTable(string[] hands, string trump, out TablePlayer[] players)
    {
        var game = new Game
        {
            Deck = new(new SortedDeckCardGenerator(hands, trump)),
        };

        var tablePlayers = new List<TablePlayer>();

        for (var i = 0; i < hands.Length; i++)
        {
            var player = game.AddPlayer("p" + i);
            tablePlayers.Add(new TablePlayer { Player = player, AuthSecret = "s" + i });
        }

        var table = new Table
        {
            Id = Guid.NewGuid(),
            Number = 1,
            Game = game,
            Players = tablePlayers,
            Owner = tablePlayers[0].Player,
        };

        game.StartGame();
        players = tablePlayers.ToArray();
        return table;
    }

    /// <summary>
    /// Индекс карты в руке игрока по строковому представлению ("10♥").
    /// </summary>
    public static int HandIndex(Player player, string card)
    {
        var cards = player.Hand.Cards;

        for (var i = 0; i < cards.Count; i++)
        {
            if (cards[i].ToString() == card)
            {
                return i;
            }
        }

        throw new InvalidOperationException($"Карта {card} не найдена в руке {player.Name}: {player.Hand}");
    }
}
