using System.Text;
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

        public void CleanAllAfkTime()
        {
            foreach (TablePlayer player in Players)
            {
                player.AfkStartTime = null;
            }
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

        public void PlayCards(string playerSecret, int[] cardIndexes, int? attackCardIndex = null)
        {
            CheckGameInProcess();
            TablePlayer tablePlayer = Players.Single(player => player.AuthSecret == playerSecret);

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
        }

        private void StartAttack(TablePlayer tablePlayer, int[] cardIndexes)
        {
            StringBuilder logCards = GetCardsLog(cardIndexes, tablePlayer);

            tablePlayer.Player.Hand.StartAttack(cardIndexes);
            WriteLog(tablePlayer.AuthSecret, $"start attack{logCards}");

            SetDefencePlayerAfkStartTime();
        }

        private void Attack(TablePlayer tablePlayer, int[] cardIndexes)
        {
            StringBuilder logCards = GetCardsLog(cardIndexes, tablePlayer);

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
                Card defenceCard = tablePlayer.Player.Hand.Cards[defenceCardIndex];
                Card tableCard = Game.Cards[attackCardIndex].AttackCard;
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


        public void Take(string playerSecret)
        {
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
            WriteLog(playerSecret, "take");
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

        private void CheckEndGame()
        {
            if (Game.Status != GameStatus.InProcess)
            {
                CleanAllAfkTime();
                StopRoundStatus = null;
                StopRoundBeginDate = null;
                WriteLog("", "game finish " + Game.Status);
                if(Game.LooserPlayer != null)
                {
                    WriteLog("", "looser: " + Game.LooserPlayer.Name);
                }
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
            StringBuilder logCards = new();
            IReadOnlyList<Card> cards = tablePlayer.Player.Hand.Cards;

            foreach (int cardIndex in cardIndexes)
            {
                if (cardIndex >= 0 && cardIndex < cards.Count)
                {
                    Card card = cards[cardIndex];
                    logCards.Append(' ').Append(card);
                }
                else
                {
                    throw new BusinessException($"Ошибка при получении логов карт: индекс {cardIndex} вне диапазона.");
                }
            }

            return logCards;
        }
    }
}
