
namespace PlayingCards.Durak
{
    /// <summary>
    /// Игрок.
    /// </summary>
    public class Player
    {
        /// <summary>
        /// Игрок.
        /// </summary>
        /// <param name="game">Игра.</param>
        public Player(Game game)
        {
            Hand = new PlayerHand(game, this);
        }

        /// <summary>
        /// Имя.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Рука.
        /// </summary>
        public PlayerHand Hand { get; set; }
    }
}
