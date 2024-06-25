namespace PlayingCards.Durak.Web.Business
{
    /// <summary>
    /// Игровой стол.
    /// </summary>
    public class Table
    {
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
        public Dictionary<string, Player> PlayerSecrets { get; set; }

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
    }
}
