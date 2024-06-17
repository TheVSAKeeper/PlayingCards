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
        }

        /// <summary>
        /// Игроки.
        /// </summary>
        public List<Player> Players { get; set; }

        /// <summary>
        /// Индекс игрока, который сейчас ходит.
        /// </summary>
        private int? _activePlayerIndex;

        /// <summary>
        /// Игровой раунд в процессе.
        /// </summary>
        private bool _roundInProcess;

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
                if (_activePlayerIndex == null)
                {
                    return null;
                }
                else
                {
                    var defencePlayerIndex = _activePlayerIndex.Value + 1;
                    if (defencePlayerIndex >= Players.Count)
                    {
                        defencePlayerIndex = 0;
                    }
                    return (Player?)Players[defencePlayerIndex];
                }
            }
        }

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
            if (_roundInProcess)
            {
                throw new Exception("round started");
            }
            _roundInProcess = true;
            if (ActivePlayer != player)
            {
                throw new Exception("player is not active");
            }
            if (cards.GroupBy(x => x.Rank.Value).Count() > 1)
            {
                throw new Exception("only one rank");
            }
            foreach (var card in cards)
            {
                var tableCard = new TableCard(this, card);
                Cards.Add(tableCard);
            }
        }

        /// <summary>
        /// Подкинуть карты.
        /// </summary>
        /// <param name="player">Игрок.</param>
        /// <param name="cards">Карты.</param>
        internal void Attack(Player player, List<Card> cards)
        {
            // todo добавить lock(object) в рамках одной игры, тут потоконебезопасно.
            if(_roundInProcess == false)
            {
                throw new Exception("round not started");
            }
            if (DefencePlayer == player)
            {
                throw new Exception("is defence player");
            }

            var cardRankExistsInTable = false;
            foreach (var card in cards)
            {
                foreach (var tableCard in Cards)
                {
                    if (card.Rank.Value == tableCard.AttackCard.Rank.Value)
                    {
                        cardRankExistsInTable = true;
                        break;
                    }
                }
                if (!cardRankExistsInTable)
                {
                    throw new Exception("this rank not found in table");
                }
            }
            foreach (var card in cards)
            {
                var addingTableCard = new TableCard(this, card);
                Cards.Add(addingTableCard);
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
            if (_roundInProcess == false)
            {
                throw new Exception("round not started");
            }
            if (DefencePlayer != player)
            {
                throw new Exception("player is not defence player");
            }
            var card = Cards.FirstOrDefault(x => x.AttackCard == attackCard);
            if (card == null)
            {
                throw new Exception("attack card not found");
            }
            card.Defence(defenceCard);
        }

        /// <summary>
        /// Добавить игрока в игру.
        /// </summary>
        /// <param name="playerName">Имя игрока.</param>
        public Player AddPlayer(string playerName)
        {
            if (Players.Count >= 6)
            {
                throw new Exception("max player count = 6");
            }
            var player = new Player(this) { Name = playerName };
            Players.Add(player);
            return player;
        }

        public void InitCardDeck()
        {
            _activePlayerIndex = null;
            for (var i = 0; i < 10; i++)
            {
                var isSuccess = ShuffleDeckAndTakeCards();
                // козырей на руках нет, перетусуем колоду.
                if (isSuccess)
                {
                    return;
                }
            }

            // никому не досталось козырей за 10 перемешиваний колоды, активным становится первый игрок.
            _activePlayerIndex = 0;
        }

        private bool ShuffleDeckAndTakeCards()
        {
            foreach (var player in Players)
            {
                player.Hand.Clear();
            }

            Deck.Shuffle();
            if (Players.Count < 2)
            {
                throw new Exception("need two or more players");
            }

            if (Players.Count > 6)
            {
                throw new Exception("need six players or less");
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

            var minTrumpSuitPlayer = minHandTrumpSuits.OrderBy(x => x.Key).FirstOrDefault().Value;
            if (minTrumpSuitPlayer != null)
            {
                _activePlayerIndex = Players.IndexOf(minTrumpSuitPlayer);
                return true;
            }
            return false;
        }

        public void StopRound()
        {
            if (Cards.All(x => x.DefenceCard != null))
            {
                // защитились от всех карт, выкидываем их в отбой
                Cards = new List<TableCard>();
            }
            else
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

            TakeCardsAfterRound();
            _roundInProcess = false;
        }

        /// <summary>
        /// Добрать до 6 карт после того, как раунд окончился, начиная с того, кто ходил по кругу.
        /// </summary>
        private void TakeCardsAfterRound()
        {
            for (var i = 0; i < Players.Count; i++)
            {
                if (Deck.CardsCount == 0)
                {
                    return;
                }
                var takeCardPlayer = Players[_activePlayerIndex.Value];
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
                _activePlayerIndex++;
                if (_activePlayerIndex >= Players.Count)
                {
                    _activePlayerIndex = 0;
                }
            }
        }
    }
}
