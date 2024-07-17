using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
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

        /// <summary>
        /// Порядковый номер стола.
        /// </summary>
        public int Number { get; set; }

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
            var log = new StringBuilder();
            log.AppendLine("deck: " + string.Join(' ', Game.Deck.Cards.Select(x => x.ToString())));
            for (int i = 0; i < Game.Players.Count; i++)
            {
                log.AppendLine("pl-" + i + ": " + string.Join(' ', Game.Players[i].Hand.Cards.Select(x => x.ToString())));
            }

            CleanLeaverPlayer();
            SetActivePlayerAfkStartTime();
            Version++;

            WriteLog("", "start game: \r\n" + log.ToString());
        }

        public void StartAttack(string playerSecret, int[] cardIndexes)
        {
            CheckGameInProcess();
            var tablePlayer = Players.Single(x => x.AuthSecret == playerSecret);
            StringBuilder logCards = GetCardsLog(cardIndexes, tablePlayer);
            tablePlayer.Player.Hand.StartAttack(cardIndexes);
            SetDefencePlayerAfkStartTime();
            Version++;
            WriteLog(playerSecret, "start attack " + logCards);
        }

        public void Attack(string playerSecret, int[] cardIndexes)
        {
            CheckGameInProcess();

            var tablePlayer = Players.Single(x => x.AuthSecret == playerSecret);
            StringBuilder logCards = GetCardsLog(cardIndexes, tablePlayer);
            tablePlayer.Player.Hand.Attack(cardIndexes);

            if (StopRoundStatus != null)
            {
                switch (StopRoundStatus)
                {
                    case Business.StopRoundStatus.SuccessDefence:
                        SetDefencePlayerAfkStartTime();
                        StopRoundBeginDate = null;
                        StopRoundStatus = null;
                        break;
                    case Business.StopRoundStatus.Take:
                        break;
                    default:
                        throw new BusinessException("undefined " + StopRoundStatus);
                }
            }
            Version++;
            WriteLog(playerSecret, "attack " + logCards);
        }
        public void Defence(string playerSecret, int defenceCardIndex, int attackCardIndex)
        {
            CheckGameInProcess();

            var tablePlayer = Players.Single(x => x.AuthSecret == playerSecret);
            var logCards = new StringBuilder();
            try
            {
                var defenceCard = tablePlayer.Player.Hand.Cards[defenceCardIndex];
                var tableCard = Game.Cards[attackCardIndex].AttackCard;
                logCards.Append(" " + defenceCard +" -> " + tableCard);
            }
            catch (Exception ex)
            {

            }
            tablePlayer.Player.Hand.Defence(defenceCardIndex, attackCardIndex);
            SetDefencePlayerAfkStartTime();

            if (Game.Cards.All(x => x.DefenceCard != null))
            {
                CheckStopRoundBeginDate();
                StopRoundStatus = Business.StopRoundStatus.SuccessDefence;
                CleanDefencePlayerAfkStartTime();
            }
            Version++;

            WriteLog(playerSecret, "defence " + logCards);
        }

        public void Take(string playerSecret)
        {
            WriteLog(playerSecret, "take");

            CheckGameInProcess();

            var tablePlayer = Players.Single(x => x.AuthSecret == playerSecret);
            if (Game.DefencePlayer != tablePlayer.Player)
            {
                throw new BusinessException("you are not defence player");
            }
            if (Game.Cards.Count == 0)
            {
                throw new BusinessException("На столе нет карт");
            }
            CheckStopRoundBeginDate();
            StopRoundStatus = Business.StopRoundStatus.Take;
            CleanDefencePlayerAfkStartTime();
            Version++;
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
            else
            {
                StopRoundBeginDate = DateTime.UtcNow;
            }
        }

        private void WriteLog(string? playerSecret, string message)
        {
            var tablePlayer = Players.SingleOrDefault(x => x.AuthSecret == playerSecret);
            var playerIndex = tablePlayer == null ? null : (int?)Game.Players.IndexOf(tablePlayer.Player);

            var logger = LogManager.GetCurrentClassLogger()
                .WithProperty("TableId", Number + " " + Id)
                .WithProperty("PlayerId", playerSecret)
                .WithProperty("PlayerIndex", playerIndex);
            logger.Info(message);
        }

        private static StringBuilder GetCardsLog(int[] cardIndexes, TablePlayer tablePlayer)
        {
            var logCards = new StringBuilder();
            try
            {
                foreach (var cardIndex in cardIndexes)
                {
                    var card = tablePlayer.Player.Hand.Cards[cardIndex];
                    logCards.Append(" " + card.ToString());
                }
            }
            catch (Exception ex)
            {

            }

            return logCards;
        }

    }
}
