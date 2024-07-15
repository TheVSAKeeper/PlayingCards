using System.Linq;
using System.Reflection;
using NLog;

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

        /// <summary>
        /// Номер версии данных, на любой чих мы его повышаем.
        /// </summary>
        public int Version { get; set; }

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

        public void StartGame()
        {
            Game.StartGame();
            CleanLeaverPlayer();
            SetActivePlayerAfkStartTime();
            Version++;
        }

        public void StartAttack(string playerSecret, int[] cardIndexes)
        {
            WriteLog(Id, playerSecret, "start attack " + string.Join(',', cardIndexes.Select(x => x.ToString())));

            CheckGameInProcess();
            var tablePlayer = Players.Single(x => x.AuthSecret == playerSecret);
            tablePlayer.Player.Hand.StartAttack(cardIndexes);
            SetDefencePlayerAfkStartTime();
            Version++;
        }

        public void Attack(string playerSecret, int[] cardIndexes)
        {
            WriteLog(Id, playerSecret, "attack " + string.Join(',', cardIndexes.Select(x => x.ToString())));

            CheckGameInProcess();

            var tablePlayer = Players.Single(x => x.AuthSecret == playerSecret);
            tablePlayer.Player.Hand.Attack(cardIndexes);

            if (StopRoundStatus != null)
            {
                if (StopRoundStatus == Business.StopRoundStatus.SuccessDefence)
                {
                    SetDefencePlayerAfkStartTime();
                    StopRoundBeginDate = null;
                    StopRoundStatus = null;
                }
                else if (StopRoundStatus == Business.StopRoundStatus.Take)
                {
                    StopRoundBeginDate = DateTime.UtcNow;
                }
                else
                {
                    throw new Exception("undefined " + StopRoundStatus);
                }
            }
            Version++;
        }

        public void Defence(string playerSecret, int defenceCardIndex, int attackCardIndex)
        {
            WriteLog(Id, playerSecret, "defence " + defenceCardIndex + " " + attackCardIndex);
            CheckGameInProcess();

            var tablePlayer = Players.Single(x => x.AuthSecret == playerSecret);
            tablePlayer.Player.Hand.Defence(defenceCardIndex, attackCardIndex);
            SetDefencePlayerAfkStartTime();

            if (Game.Cards.All(x => x.DefenceCard != null))
            {
                CheckStopRoundBeginDate();
                StopRoundStatus = Business.StopRoundStatus.SuccessDefence;
                CleanDefencePlayerAfkStartTime();
            }
            Version++;
        }

        public void Take(string playerSecret)
        {
            WriteLog(Id, playerSecret, "take");

            CheckGameInProcess();
            CheckStopRoundBeginDate();

            var tablePlayer = Players.Single(x => x.AuthSecret == playerSecret);
            if (Game.DefencePlayer != tablePlayer.Player)
            {
                throw new Exception("you are not defence player");
            }
            StopRoundStatus = Business.StopRoundStatus.Take;
            CleanDefencePlayerAfkStartTime();
            Version++;
        }

        private void CheckGameInProcess()
        {
            if (Game.Status != GameStatus.InProcess)
            {
                throw new Exception("game not in process");
            }
        }

        private void CheckStopRoundBeginDate()
        {
            if (StopRoundBeginDate != null)
            {
                throw new Exception("stop round in process");
            }
            else
            {
                StopRoundBeginDate = DateTime.UtcNow;
            }
        }

        private void WriteLog(Guid tableId, string? playerSecret, string message)
        {
            var logger = LogManager.GetCurrentClassLogger()
                .WithProperty("TableId", tableId)
                .WithProperty("PlayerId", playerSecret);
            logger.Info(message);
        }
    }
}
