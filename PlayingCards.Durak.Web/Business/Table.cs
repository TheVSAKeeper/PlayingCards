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

        public void CleanLeaverPlayer()
        {
            LeavePlayer = null;
            LeavePlayerIndex = null;
        }
    }
}
