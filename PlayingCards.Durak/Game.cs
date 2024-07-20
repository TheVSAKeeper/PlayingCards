namespace PlayingCards.Durak
{
    /// <summary>
    /// Игра.
    /// </summary>
    public class Game
    {
        /// <summary>
        /// Игра.
        /// </summary>
        public Game()
        {
            Deck = new Deck(new RandomDeckCardGenerator());
            Cards = new List<TableCard>();
            Players = new List<Player>();
            Status = GameStatus.WaitPlayers;
        }

        /// <summary>
        /// Игроки.
        /// </summary>
        public List<Player> Players { get; set; }

        /// <summary>
        /// Статус.
        /// </summary>
        public GameStatus Status { get; private set; }

        /// <summary>
        /// Индекс игрока, который начинает раунд.
        /// </summary>
        private int? _activePlayerIndex;

        /// <summary>
        /// Индекс игрока, который сейчас защищается.
        /// </summary>
        private int? _defencePlayerIndex;

        /// <summary>
        /// Номинал карты, минимального козыря.
        /// </summary>
        /// <remarks>
        /// Игрок, который ходит первый (с минимальным козырем), должен показать его другим игрокам.
        /// </remarks>
        public int? NeedShowCardMinTrumpValue { get; set; }

        /// <summary>
        /// Есть первый отбой.
        /// </summary>
        private bool _isSuccessDefenceExists;

        /// <summary>
        /// Игрок, который сейчас ходит.
        /// </summary>
        public Player? ActivePlayer
        {
            get
            {
                if (_activePlayerIndex == null)
                {
                    return null;
                }
                else
                {
                    return (Player?)Players[_activePlayerIndex.Value];
                }
            }
        }

        /// <summary>
        /// Игрок, на которого, ходят.
        /// </summary>
        public Player? DefencePlayer
        {
            get
            {
                if (_defencePlayerIndex == null)
                {
                    return null;
                }
                else
                {
                    return (Player?)Players[_defencePlayerIndex.Value];
                }
            }
        }

        /// <summary>
        /// Проигравший.
        /// </summary>
        public Player? LooserPlayer { get; set; }

        /// <summary>
        /// Карты на столе.
        /// </summary>
        public List<TableCard> Cards { get; set; }

        /// <summary>
        /// Колода.
        /// </summary>
        public Deck Deck { get; set; }

        /// <summary>
        /// Начать раунд, сыграв карту.
        /// </summary>
        /// <param name="player">Игрок.</param>
        /// <param name="cards">Карты.</param>
        internal void StartAttack(Player player, List<Card> cards)
        {
            CheckGameInProcess();

            if (IsRoundStarted())
            {
                throw new BusinessException("round started");
            }

            if (ActivePlayer != player)
            {
                throw new BusinessException("player is not active");
            }

            if (cards.GroupBy(x => x.Rank.Value).Count() > 1)
            {
                throw new BusinessException("only one rank");
            }

            CheckDeskCardCount(cards.Count);
            foreach (var card in cards)
            {
                var tableCard = new TableCard(this, card);
                Cards.Add(tableCard);
                player.Hand.Cards.Remove(card);
            }
            NeedShowCardMinTrumpValue = null;
            CheckWin();
        }

        /// <summary>
        /// Подкинуть карты.
        /// </summary>
        /// <param name="player">Игрок.</param>
        /// <param name="cards">Карты.</param>
        internal void Attack(Player player, List<Card> cards)
        {
            CheckGameInProcess();

            // todo добавить lock(object) в рамках одной игры, тут потоконебезопасно.
            if (IsRoundStarted() == false)
            {
                throw new BusinessException("round not started");
            }

            if (DefencePlayer == player)
            {
                throw new BusinessException("is defence player");
            }

            CheckDeskCardCount(cards.Count);

            foreach (var card in cards)
            {
                if (!IsRankOnTable(card))
                {
                    throw new BusinessException("this rank not found in table");
                }
            }
            
            foreach (var card in cards)
            {
                var addingTableCard = new TableCard(this, card);
                Cards.Add(addingTableCard);
                player.Hand.Cards.Remove(card);
            }
            CheckWin();
        }

        private bool IsRankOnTable(Card card)
        {
            var cardRankExistsInTable = false;
            foreach (var tableCard in Cards)
            {
                if (card.Rank.Value == tableCard.AttackCard.Rank.Value)
                {
                    cardRankExistsInTable = true;
                    break;
                }
                if (tableCard.DefenceCard != null)
                {
                    if (card.Rank.Value == tableCard.DefenceCard.Rank.Value)
                    {
                        cardRankExistsInTable = true;
                        break;
                    }
                }
            }

            return cardRankExistsInTable;
        }

        private void CheckWin()
        {
            var playersWithCards = Players.Where(x => x.Hand.Cards.Count > 0);
            if (playersWithCards.Count() == 1 && Deck.CardsCount == 0)
            {
                LooserPlayer = playersWithCards.First();
                Status = GameStatus.Finish;
            }
        }

        private void CheckDeskCardCount(int cardsCount)
        {
            if (cardsCount + Cards.Count(x => x.DefenceCard == null) > DefencePlayer.Hand.Cards.Count)
            {
                throw new BusinessException("defence player cards count less that attack cards count");
            }

            var maxCardsCount = _isSuccessDefenceExists ? 6 : 5;
            if (cardsCount + Cards.Count > maxCardsCount)
            {
                throw new BusinessException("max cards equals " + maxCardsCount);
            }
        }

        /// <summary>
        /// Защититься от карты.
        /// </summary>
        /// <param name="player">Игрок.</param>
        /// <param name="defenceCard">Карта, которой мы защищаемся.</param>
        /// <param name="attackCard">Карта, от которой защищаемся.</param>
        internal void Defence(Player player, Card defenceCard, Card attackCard)
        {
            CheckGameInProcess();

            if (IsRoundStarted() == false)
            {
                throw new BusinessException("round not started");
            }
            if (DefencePlayer != player)
            {
                throw new BusinessException("player is not defence player");
            }
            var card = Cards.FirstOrDefault(x => x.AttackCard == attackCard);
            if (card == null)
            {
                throw new BusinessException("attack card not found");
            }
            card.Defence(defenceCard);
            player.Hand.Cards.Remove(defenceCard);
            CheckWin();
        }

        /// <summary>
        /// Добавить игрока в игру.
        /// </summary>
        /// <param name="playerName">Имя игрока.</param>
        public Player AddPlayer(string playerName)
        {
            if (Players.Count >= 6)
            {
                throw new BusinessException("max player count = 6");
            }

            if (Status == GameStatus.InProcess)
            {
                throw new BusinessException("bad status for join: " + Status);
            }

            var player = new Player(this) { Name = playerName };
            Players.Add(player);
            if (Players.Count >= 2)
            {
                Status = GameStatus.ReadyToStart;
            }
            return player;
        }

        /// <summary>
        /// Игрок вышел из игры.
        /// </summary>
        /// <param name="playerIndex">Имя игрока.</param>
        public Player LeavePlayer(int playerIndex)
        {
            var player = Players[playerIndex];
            Players.Remove(player);
            if (Status == GameStatus.InProcess)
            {
                Status = GameStatus.Finish;
                _activePlayerIndex = null;
            }
            else
            {
                if (Players.Count <= 1)
                {
                    Status = GameStatus.WaitPlayers;
                }
            }

            if (Players.Count >= 2)
            {
                Status = GameStatus.ReadyToStart;
            }
            return player;
        }

        public void StartGame()
        {
            if (Status != GameStatus.ReadyToStart && Status != GameStatus.Finish)
            {
                throw new BusinessException("bad status for start: " + Status);
            }
            SetActivePlayerIndex(null);
            _isSuccessDefenceExists = false;
            Cards = new List<TableCard>();
            Status = GameStatus.InProcess;
            int? looserPlayerIndex = null;
            if (LooserPlayer != null)
            {
                looserPlayerIndex = Players.IndexOf(LooserPlayer);
                LooserPlayer = null;
            }
            for (var i = 0; i < 10; i++)
            {
                var isSuccess = ShuffleDeckAndTakeCards(looserPlayerIndex);
                // козырей на руках нет, перетусуем колоду.
                if (isSuccess)
                {
                    return;
                }
            }

            // никому не досталось козырей за 10 перемешиваний колоды, активным становится первый игрок.
            SetActivePlayerIndex(0);
        }

        private bool ShuffleDeckAndTakeCards(int? looserPlayerIndex)
        {
            foreach (var player in Players)
            {
                player.Hand.Clear();
            }

            Deck.Shuffle();
            if (Players.Count < 2)
            {
                throw new BusinessException("need two or more players");
            }

            if (Players.Count > 6)
            {
                throw new BusinessException("need six players or less");
            }

            for (var i = 0; i < 6; i++)
            {
                foreach (var player in Players)
                {
                    var card = Deck.PullCard();
                    player.Hand.TakeCard(card);
                }
            }

            var trumpSuitValue = Deck.TrumpCard.Suit.Value;
            var minHandTrumpSuits = new Dictionary<int, Player>();
            foreach (var player in Players)
            {
                var minTrumpRank = player.Hand.Cards
                    .Where(x => x.Suit.Value == trumpSuitValue)
                    .OrderBy(x => x.Rank.Value)
                    .FirstOrDefault()?.Rank.Value;
                if (minTrumpRank != null)
                {
                    minHandTrumpSuits.Add(minTrumpRank.Value, player);
                }
            }

            if (looserPlayerIndex != null)
            {
                var activePlayerIndex = looserPlayerIndex - 1;
                if (activePlayerIndex < 0)
                {
                    activePlayerIndex = Players.Count - 1;
                }
                SetActivePlayerIndex(activePlayerIndex);
                return true;
            }

            var minTrumpSuitPlayerSuit = minHandTrumpSuits.OrderBy(x => x.Key).FirstOrDefault();
            var minTrumpSuitPlayer = minTrumpSuitPlayerSuit.Value;
            if (minTrumpSuitPlayer != null)
            {
                var activePlayerIndex = Players.IndexOf(minTrumpSuitPlayer);
                SetActivePlayerIndex(activePlayerIndex);
                NeedShowCardMinTrumpValue = minTrumpSuitPlayerSuit.Key;
                return true;
            }
            return false;
        }

        public void StopRound()
        {
            CheckGameInProcess();

            bool isDefenceSuccess = Cards.All(x => x.DefenceCard != null);
            // если защитился не от всех карт, то забирает себе, иначе всё в отбой и следующий раунд.
            if (!isDefenceSuccess)
            {
                foreach (var card in Cards)
                {
                    DefencePlayer.Hand.TakeCard(card.AttackCard);
                    if (card.DefenceCard != null)
                    {
                        DefencePlayer.Hand.TakeCard(card.DefenceCard);
                    }
                }
            }
            else
            {
                _isSuccessDefenceExists = true;
            }

            Cards = new List<TableCard>();

            TakeCardsAfterRound();
            SetNextActivePlayer(isDefenceSuccess);
        }

        private void SetNextActivePlayer(bool isDefenceSuccess)
        {
            if (isDefenceSuccess)
            {
                SetNextActivePlayerIndex();
            }
            else
            {
                // если игрок не отбился, то ход сначала переходит к нему, а потом к следующему за ним.
                SetNextActivePlayerIndex();
                SetNextActivePlayerIndex();
            }
        }

        private void SetNextActivePlayerIndex()
        {
            var activePlayerIndex = _activePlayerIndex;
            activePlayerIndex++;
            if (activePlayerIndex >= Players.Count)
            {
                activePlayerIndex = activePlayerIndex - Players.Count;
            }

            activePlayerIndex = MoveNextIfCardsCountEqualZero(activePlayerIndex.Value);
            SetActivePlayerIndex(activePlayerIndex);
        }

        private int MoveNextIfCardsCountEqualZero(int playerIndex)
        {
            for (var i = 0; i < Players.Count; i++)
            {
                if (Players[playerIndex].Hand.Cards.Count == 0)
                {
                    playerIndex++;
                    if (playerIndex >= Players.Count)
                    {
                        playerIndex = 0;
                    }
                }
                else
                {
                    break;
                }
            }
            return playerIndex;
        }

        /// <summary>
        /// Добрать до 6 карт после того, как раунд окончился, начиная с того, кто ходил по кругу.
        /// </summary>
        private void TakeCardsAfterRound()
        {
            var startPlayerIndex = _activePlayerIndex.Value;
            for (var i = 0; i < Players.Count; i++)
            {
                if (Deck.CardsCount == 0)
                {
                    return;
                }
                var takeCardPlayer = Players[startPlayerIndex];
                var handCount = takeCardPlayer.Hand.Cards.Count();
                if (handCount < 6)
                {
                    var needTakeCount = 6 - handCount;
                    for (var j = 0; j < needTakeCount; j++)
                    {
                        var takeCard = Deck.PullCard();
                        takeCardPlayer.Hand.TakeCard(takeCard);

                        if (Deck.CardsCount == 0)
                        {
                            return;
                        }
                    }
                }
                startPlayerIndex++;
                if (startPlayerIndex >= Players.Count)
                {
                    startPlayerIndex = 0;
                }
            }
        }

        private bool IsRoundStarted()
        {
            return Cards.Count > 0;
        }

        private void CheckGameInProcess()
        {
            if (Status != GameStatus.InProcess)
            {
                throw new BusinessException("game not in process: " + Status);
            }
            //if (LooserPlayer != null)
            //{
            //    throw new BusinessException("looser is ready");
            //}
        }

        private void SetActivePlayerIndex(int? value)
        {
            _activePlayerIndex = value;
            if (_activePlayerIndex == null)
            {
                _defencePlayerIndex = null;
                return;
            }

            var defencePlayerIndex = _activePlayerIndex.Value + 1;
            if (defencePlayerIndex >= Players.Count)
            {
                defencePlayerIndex = 0;
            }

            _defencePlayerIndex = MoveNextIfCardsCountEqualZero(defencePlayerIndex);
        }
    }
}
